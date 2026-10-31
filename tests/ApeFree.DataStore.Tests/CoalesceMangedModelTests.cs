using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApeFree.DataStore.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApeFree.DataStore.Tests
{
    [TestClass]
    public class CoalesceMangedModelTests
    {
        [TestMethod]
        public async Task TestHighConcurrencyWriteCoalescing()
        {
            var model = new CoalesceMangedModel<SampleSettings>();
            int physicalWriteCount = 0;
            var isRunning = 0;

            Action ensureConsumerRunning = null;
            ensureConsumerRunning = () =>
            {
                if (Interlocked.CompareExchange(ref isRunning, 1, 0) == 0)
                {
                    Task.Run(() =>
                    {
                        while (true)
                        {
                            var item = model.Dequeue();
                            if (item == null)
                            {
                                Interlocked.Exchange(ref isRunning, 0);
                                item = model.Dequeue();
                                if (item != null)
                                {
                                    if (Interlocked.CompareExchange(ref isRunning, 1, 0) != 0) break;
                                }
                                else break;
                            }

                            if (item.EventType == ReadWriteEventType.Write)
                            {
                                Interlocked.Increment(ref physicalWriteCount);
                                // 模拟物理 IO 延迟
                                Thread.Sleep(2);
                            }
                            item.Release();
                        }
                    });
                }
            };

            const int totalWrites = 500;
            var tasks = new List<Task>();

            for (int i = 0; i < totalWrites; i++)
            {
                var item = new EventItem<SampleSettings>(ReadWriteEventType.Write, new SampleSettings { Version = i });
                tasks.Add(model.EnqueueAsync(item, ensureConsumerRunning));
            }

            await Task.WhenAll(tasks);

            // 验证所有 500 个写操作都正常完成，且触发写合并（物理写次数远小于 500）
            Assert.IsTrue(physicalWriteCount < totalWrites, $"物理写入次数 ({physicalWriteCount}) 应显著低于并发请求总数 ({totalWrites})");
            Assert.IsTrue(physicalWriteCount > 0, "至少发生了一次物理写入");
        }

        [TestMethod]
        public async Task TestReadYourWritesCausalConsistency()
        {
            var model = new CoalesceMangedModel<SampleSettings>();
            var currentStoreValue = new SampleSettings { Version = 0 };
            var isRunning = 0;

            Action ensureConsumerRunning = null;
            ensureConsumerRunning = () =>
            {
                if (Interlocked.CompareExchange(ref isRunning, 1, 0) == 0)
                {
                    Task.Run(() =>
                    {
                        while (true)
                        {
                            var item = model.Dequeue();
                            if (item == null)
                            {
                                Interlocked.Exchange(ref isRunning, 0);
                                item = model.Dequeue();
                                if (item != null)
                                {
                                    if (Interlocked.CompareExchange(ref isRunning, 1, 0) != 0) break;
                                }
                                else break;
                            }

                            if (item.EventType == ReadWriteEventType.Write)
                            {
                                currentStoreValue = item.Value;
                            }
                            else
                            {
                                item.Value = currentStoreValue;
                            }
                            item.Release();
                        }
                    });
                }
            };

            // 1. 写入 Version 1
            await model.EnqueueAsync(new EventItem<SampleSettings>(ReadWriteEventType.Write, new SampleSettings { Version = 1 }), ensureConsumerRunning);

            // 2. 并发发起 Read1、Write(Version 2)、Read2
            var readItem1 = new EventItem<SampleSettings>(ReadWriteEventType.Read);
            var writeItem2 = new EventItem<SampleSettings>(ReadWriteEventType.Write, new SampleSettings { Version = 2 });
            var readItem2 = new EventItem<SampleSettings>(ReadWriteEventType.Read);

            var taskR1 = model.EnqueueAsync(readItem1, ensureConsumerRunning);
            var taskW2 = model.EnqueueAsync(writeItem2, ensureConsumerRunning);
            var taskR2 = model.EnqueueAsync(readItem2, ensureConsumerRunning);

            await Task.WhenAll(taskR1, taskW2, taskR2);

            // 严格验证时序因果：在 W2 之后入队的 R2 必须读到 Version 2
            Assert.AreEqual(2, readItem2.Value.Version, "Read2 在 Write2 之后发起，必须读取到 Write2 写入的新值！");
        }

        [TestMethod]
        public async Task TestExceptionPropagation()
        {
            var model = new CoalesceMangedModel<SampleSettings>();
            var isRunning = 0;

            Action ensureConsumerRunning = () =>
            {
                if (Interlocked.CompareExchange(ref isRunning, 1, 0) == 0)
                {
                    Task.Run(() =>
                    {
                        var item = model.Dequeue();
                        if (item != null)
                        {
                            item.Fail(new InvalidOperationException("模拟物理 IO 失败"));
                        }
                        Interlocked.Exchange(ref isRunning, 0);
                    });
                }
            };

            var item = new EventItem<SampleSettings>(ReadWriteEventType.Write);
            var task = model.EnqueueAsync(item, ensureConsumerRunning);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            {
                await task;
            });
        }
    }
}
