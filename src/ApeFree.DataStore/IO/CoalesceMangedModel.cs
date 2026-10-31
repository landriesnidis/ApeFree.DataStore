using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApeFree.DataStore.IO
{
    /// <summary>
    /// 双缓冲读写因果合并管理模型（Coalesce Managed Model）
    /// 在高频并发场景下将连续写入合并为单次落盘，同时严格保证读写因果时序一致性。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CoalesceMangedModel<T> : IReadWriteMangedModel<T>
    {
        internal TaskGroup currentTaskGroup = new TaskGroup();
        internal TaskGroup standbyTaskGroup = new TaskGroup();
        private readonly object queueLocker = new object();

        /// <inheritdoc/>
        public EventItem<T> Dequeue()
        {
            lock (queueLocker)
            {
                // 若当前批次已清空，翻转备用批次为当前批次（双缓冲翻转）
                if (currentTaskGroup.IsEmpty)
                {
                    var temp = currentTaskGroup;
                    currentTaskGroup = standbyTaskGroup;
                    standbyTaskGroup = temp;
                }

                if (!currentTaskGroup.IsEmpty)
                {
                    // 组内写操作优先于读操作
                    if (!currentTaskGroup.IsWriteTaskEmpty)
                    {
                        return (EventItem<T>)currentTaskGroup.GetWriteTask();
                    }
                    if (!currentTaskGroup.IsReadTaskEmpty)
                    {
                        return (EventItem<T>)currentTaskGroup.GetReadTask();
                    }
                }

                return null;
            }
        }

        /// <inheritdoc/>
        public Task EnqueueAsync(EventItem item, Action ensureConsumerRunning)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (queueLocker)
            {
                if (item.EventType == ReadWriteEventType.Read)
                {
                    // 若备用组已存在写任务，说明当前读操作发生在写操作之后；
                    // 为保证 Read-Your-Writes 因果一致性，读操作必须跟随进入备用组。
                    if (!standbyTaskGroup.IsWriteTaskEmpty)
                    {
                        standbyTaskGroup.Join(item, tcs);
                    }
                    else
                    {
                        currentTaskGroup.Join(item, tcs);
                    }
                }
                else // Write
                {
                    // 若当前组已有写任务，直接加入当前组覆盖旧写（写折叠）
                    if (!currentTaskGroup.IsWriteTaskEmpty)
                    {
                        currentTaskGroup.Join(item, tcs);
                    }
                    else
                    {
                        // 若当前组已有排队的读任务，为避免写优先于读破坏先读后写的语义，将写放入备用组
                        if (!currentTaskGroup.IsReadTaskEmpty)
                        {
                            standbyTaskGroup.Join(item, tcs);
                        }
                        else
                        {
                            currentTaskGroup.Join(item, tcs);
                        }
                    }
                }
            }

            ensureConsumerRunning?.Invoke();
            return tcs.Task;
        }
    }

    /// <summary>
    /// 任务组（原子操作批次）
    /// </summary>
    internal class TaskGroup
    {
        private EventItem writeTask;
        private EventItem readTask;
        private readonly List<TaskCompletionSource<bool>> writeCompletionSources = new List<TaskCompletionSource<bool>>();
        private readonly List<TaskCompletionSource<bool>> readCompletionSources = new List<TaskCompletionSource<bool>>();

        public bool IsEmpty => writeTask == null && readTask == null;
        public bool IsWriteTaskEmpty => writeTask == null;
        public bool IsReadTaskEmpty => readTask == null;

        public void Join(EventItem item, TaskCompletionSource<bool> tcs)
        {
            if (item.EventType == ReadWriteEventType.Write)
            {
                lock (writeCompletionSources)
                {
                    writeCompletionSources.Add(tcs);
                    // 覆盖前序写任务（只落盘最新状态）
                    writeTask = item;
                    item.CompletionSources.Clear();
                    item.CompletionSources.AddRange(writeCompletionSources);
                }
            }
            else
            {
                lock (readCompletionSources)
                {
                    readCompletionSources.Add(tcs);
                    readTask = item;
                    item.CompletionSources.Clear();
                    item.CompletionSources.AddRange(readCompletionSources);
                }
            }
        }

        public EventItem GetWriteTask()
        {
            lock (writeCompletionSources)
            {
                var result = writeTask;
                writeTask = null;
                writeCompletionSources.Clear();
                return result;
            }
        }

        public EventItem GetReadTask()
        {
            lock (readCompletionSources)
            {
                var result = readTask;
                readTask = null;
                readCompletionSources.Clear();
                return result;
            }
        }
    }
}
