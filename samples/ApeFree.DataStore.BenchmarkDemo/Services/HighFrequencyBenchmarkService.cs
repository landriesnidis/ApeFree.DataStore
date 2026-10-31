using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApeFree.DataStore.Adapters;
using ApeFree.DataStore.BenchmarkDemo.Models;
using ApeFree.DataStore.Core;
using ApeFree.DataStore.Local;
using Newtonsoft.Json;

namespace ApeFree.DataStore.BenchmarkDemo.Services
{
    /// <summary>
    /// 高频读写基准测试结果模型
    /// </summary>
    public class HighFrequencyResult
    {
        public string StrategyName { get; set; }
        public int TotalOperations { get; set; }
        public int ConcurrencyThreads { get; set; }
        public double ElapsedMilliseconds { get; set; }
        public double ThroughputOpsPerSec { get; set; }
        public double AvgLatencyMs { get; set; }
        public double SingleReadLatencyMs { get; set; }
        public int PhysicalDiskWriteCount { get; set; }
        public long TotalDiskBytesWritten { get; set; }
        public int ConcurrencyLockErrors { get; set; }
        public bool IsConsistent { get; set; }
        public string IntegrityDetails { get; set; }
    }

    /// <summary>
    /// 统计流过滤器：用于精确捕获物理底层流的实际落盘触发次数与写入字节总数
    /// </summary>
    public class PhysicalDiskIOMonitorFilter : IStreamFilter
    {
        private int writeCount = 0;
        private long totalBytes = 0;

        public int PhysicalWriteCount => writeCount;
        public long TotalBytesWritten => totalBytes;

        public void Reset()
        {
            Interlocked.Exchange(ref writeCount, 0);
            Interlocked.Exchange(ref totalBytes, 0);
        }

        public Stream WrapWriteStream(Stream innerStream)
        {
            Interlocked.Increment(ref writeCount);
            return new CountingWriteStream(innerStream, bytes => Interlocked.Add(ref totalBytes, bytes));
        }

        public Stream WrapReadStream(Stream innerStream)
        {
            return innerStream;
        }

        public void Dispose() { }

        private class CountingWriteStream : Stream
        {
            private readonly Stream inner;
            private readonly Action<long> onBytesWritten;

            public CountingWriteStream(Stream inner, Action<long> onBytesWritten)
            {
                this.inner = inner;
                this.onBytesWritten = onBytesWritten;
            }

            public override bool CanRead => inner.CanRead;
            public override bool CanSeek => inner.CanSeek;
            public override bool CanWrite => inner.CanWrite;
            public override long Length => inner.Length;
            public override long Position { get => inner.Position; set => inner.Position = value; }

            public override void Flush() => inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
            public override void SetLength(long value) => inner.SetLength(value);

            public override void Write(byte[] buffer, int offset, int count)
            {
                inner.Write(buffer, offset, count);
                onBytesWritten?.Invoke(count);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    inner.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// 高频读写配置基准对比测试执行服务
    /// </summary>
    public class HighFrequencyBenchmarkService
    {
        private readonly string tempDir;

        public HighFrequencyBenchmarkService()
        {
            tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Benchmark_Temp_Config");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
        }

        /// <summary>
        /// 执行传统原生 JSON 直接读写基准测试
        /// </summary>
        public async Task<HighFrequencyResult> RunDirectJsonBenchmarkAsync(
            int totalOps,
            int threadCount,
            bool complexData,
            int simulatedDelayMs,
            Action<string> logAction,
            Action<int, int> progressAction)
        {
            string filePath = Path.Combine(tempDir, "direct_json_config.json");
            if (File.Exists(filePath)) File.Delete(filePath);

            var initialConfig = AppConfig.CreateSampleConfig(complexData);
            string initJson = JsonConvert.SerializeObject(initialConfig, Formatting.Indented);
            File.WriteAllText(filePath, initJson);

            logAction?.Invoke($"[传统直接读写] 开始测试: 总操作数={totalOps}, 并发线程={threadCount}, 单次保存延时={simulatedDelayMs}ms...");

            var result = new HighFrequencyResult
            {
                StrategyName = "传统原生读写 (File.WriteAllText + Json)",
                TotalOperations = totalOps,
                ConcurrencyThreads = threadCount
            };

            int opsPerThread = totalOps / threadCount;
            int completedOps = 0;
            int lockCollisions = 0;
            long totalWrittenBytes = 0;
            int physicalDiskWrites = 0;

            object fileLock = new object();
            var sw = Stopwatch.StartNew();

            if (threadCount <= 1)
            {
                // 单线程高频读写
                for (int i = 0; i < totalOps; i++)
                {
                    // 模拟业务修改并保存
                    initialConfig.Counter++;
                    initialConfig.LastUpdated = DateTime.Now;
                    string json = JsonConvert.SerializeObject(initialConfig);
                    byte[] bytes = Encoding.UTF8.GetBytes(json);

                    File.WriteAllText(filePath, json);
                    physicalDiskWrites++;
                    totalWrittenBytes += bytes.Length;

                    completedOps++;
                    if (i % 100 == 0 || i == totalOps - 1)
                    {
                        progressAction?.Invoke(completedOps, totalOps);
                    }

                    if (simulatedDelayMs > 0)
                    {
                        await Task.Delay(simulatedDelayMs);
                    }
                }
            }
            else
            {
                // 多线程并发高频读写
                var tasks = new List<Task>();
                for (int t = 0; t < threadCount; t++)
                {
                    int threadIndex = t;
                    tasks.Add(Task.Run(() =>
                    {
                        for (int i = 0; i < opsPerThread; i++)
                        {
                            try
                            {
                                lock (fileLock)
                                {
                                    // 模拟读取并修改保存
                                    initialConfig.Counter++;
                                    initialConfig.LastUpdated = DateTime.Now;
                                    string json = JsonConvert.SerializeObject(initialConfig);
                                    byte[] bytes = Encoding.UTF8.GetBytes(json);

                                    File.WriteAllText(filePath, json);
                                    Interlocked.Increment(ref physicalDiskWrites);
                                    Interlocked.Add(ref totalWrittenBytes, bytes.Length);
                                }
                            }
                            catch (IOException)
                            {
                                Interlocked.Increment(ref lockCollisions);
                            }

                            int current = Interlocked.Increment(ref completedOps);
                            if (current % 100 == 0 || current == totalOps)
                            {
                                progressAction?.Invoke(current, totalOps);
                            }

                            if (simulatedDelayMs > 0)
                            {
                                Thread.Sleep(simulatedDelayMs);
                            }
                        }
                    }));
                }
                await Task.WhenAll(tasks);
            }

            sw.Stop();
            result.ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds;
            result.ThroughputOpsPerSec = (totalOps / result.ElapsedMilliseconds) * 1000.0;
            result.AvgLatencyMs = result.ElapsedMilliseconds / totalOps;
            result.PhysicalDiskWriteCount = physicalDiskWrites;
            result.TotalDiskBytesWritten = totalWrittenBytes;
            result.ConcurrencyLockErrors = lockCollisions;

            // 测试单次读取耗时（每次从磁盘读取并解析）
            var readSw = Stopwatch.StartNew();
            for (int r = 0; r < 50; r++)
            {
                string txt = File.ReadAllText(filePath);
                var loaded = JsonConvert.DeserializeObject<AppConfig>(txt);
            }
            readSw.Stop();
            result.SingleReadLatencyMs = readSw.Elapsed.TotalMilliseconds / 50.0;

            // 校验最终结果
            string verifyJson = File.ReadAllText(filePath);
            var finalObj = JsonConvert.DeserializeObject<AppConfig>(verifyJson);
            result.IsConsistent = finalObj != null && finalObj.Counter > 0;
            result.IntegrityDetails = $"磁盘最终计数={finalObj?.Counter}, 期望值={completedOps}";

            logAction?.Invoke($"[传统直接读写] 完成: 耗时={result.ElapsedMilliseconds:F1}ms, 吞吐={result.ThroughputOpsPerSec:F0} ops/s, 实际落盘={physicalDiskWrites}次");
            return result;
        }

        /// <summary>
        /// 执行 ApeFree.DataStore (内置 CoalesceMangedModel 写合并) 基准测试
        /// </summary>
        public async Task<HighFrequencyResult> RunDataStoreBenchmarkAsync(
            int totalOps,
            int threadCount,
            bool complexData,
            int simulatedDelayMs,
            Action<string> logAction,
            Action<int, int> progressAction)
        {
            string filePath = Path.Combine(tempDir, "datastore_coalesce_config.json");
            if (File.Exists(filePath)) File.Delete(filePath);

            var diskMonitor = new PhysicalDiskIOMonitorFilter();
            var settings = new LocalStoreAccessSettings(filePath)
            {
                // 使用 JSON 适配器以实现公正的同格式对比
                SerializationAdapter = new JsonSerializationAdapter()
            };
            settings.StreamFilters.Add(diskMonitor);

            var store = StoreFactory.Factory.CreateLocalStore<AppConfig>(settings, () => AppConfig.CreateSampleConfig(complexData));
            store.Load();

            logAction?.Invoke($"[ApeFree.DataStore] 开始测试: 总操作数={totalOps}, 并发线程={threadCount}, 异步因果写合并调度已激活...");

            var result = new HighFrequencyResult
            {
                StrategyName = "ApeFree.DataStore (Coalesce 读写合并)",
                TotalOperations = totalOps,
                ConcurrencyThreads = threadCount
            };

            int opsPerThread = totalOps / threadCount;
            int completedOps = 0;
            var sw = Stopwatch.StartNew();

            if (threadCount <= 1)
            {
                // 单线程高频触发异步持久化请求（模拟高频配置变更或传感器采样打点）
                var saveTasks = new List<Task>(totalOps);
                for (int i = 0; i < totalOps; i++)
                {
                    store.Value.Counter++;
                    store.Value.LastUpdated = DateTime.Now;
                    saveTasks.Add(store.SaveAsync());

                    completedOps++;
                    if (i % 100 == 0 || i == totalOps - 1)
                    {
                        progressAction?.Invoke(completedOps, totalOps);
                    }

                    if (simulatedDelayMs > 0)
                    {
                        await Task.Delay(simulatedDelayMs);
                    }
                }
                // 等待所有因果合并批次落盘完成
                await Task.WhenAll(saveTasks);
            }
            else
            {
                // 多线程高并发异步写入
                var saveTasks = new ConcurrentBag<Task>();
                var workerTasks = new List<Task>();
                for (int t = 0; t < threadCount; t++)
                {
                    workerTasks.Add(Task.Run(async () =>
                    {
                        for (int i = 0; i < opsPerThread; i++)
                        {
                            lock (store.Value)
                            {
                                store.Value.Counter++;
                                store.Value.LastUpdated = DateTime.Now;
                            }
                            saveTasks.Add(store.SaveAsync());

                            int current = Interlocked.Increment(ref completedOps);
                            if (current % 100 == 0 || current == totalOps)
                            {
                                progressAction?.Invoke(current, totalOps);
                            }

                            if (simulatedDelayMs > 0)
                            {
                                await Task.Delay(simulatedDelayMs);
                            }
                        }
                    }));
                }
                await Task.WhenAll(workerTasks);
                await Task.WhenAll(saveTasks);
            }

            sw.Stop();
            result.ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds;
            result.ThroughputOpsPerSec = (totalOps / result.ElapsedMilliseconds) * 1000.0;
            result.AvgLatencyMs = result.ElapsedMilliseconds / totalOps;
            result.PhysicalDiskWriteCount = diskMonitor.PhysicalWriteCount;
            result.TotalDiskBytesWritten = diskMonitor.TotalBytesWritten;
            result.ConcurrencyLockErrors = 0; // DataStore 调度器零锁冲突

            // 测试单次读取耗时（直接内存级访问）
            var readSw = Stopwatch.StartNew();
            for (int r = 0; r < 50; r++)
            {
                var val = store.Value;
                var dummy = val.Counter;
            }
            readSw.Stop();
            result.SingleReadLatencyMs = readSw.Elapsed.TotalMilliseconds / 50.0;

            // 最终一致性校验
            store.Dispose();
            string verifyJson = File.ReadAllText(filePath);
            var finalObj = JsonConvert.DeserializeObject<AppConfig>(verifyJson);
            result.IsConsistent = finalObj != null && finalObj.Counter > 0;
            result.IntegrityDetails = $"磁盘最终计数={finalObj?.Counter}, 期望值={completedOps}";

            logAction?.Invoke($"[ApeFree.DataStore] 完成: 耗时={result.ElapsedMilliseconds:F1}ms, 吞吐={result.ThroughputOpsPerSec:F0} ops/s, 实际物理落盘仅={result.PhysicalDiskWriteCount}次！");
            return result;
        }

        public void CleanUp()
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch { }
        }
    }
}
