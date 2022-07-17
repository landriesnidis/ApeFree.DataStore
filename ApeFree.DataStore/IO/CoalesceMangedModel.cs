using System;
using System.Collections.Generic;
using System.Threading;

namespace ApeFree.DataStore.IO
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CoalesceMangedModel<T> : IReadWriteMangedModel<T>
    {
        internal TaskGroup currentTaskGroup = new TaskGroup();
        internal TaskGroup standbyTaskGroup = new TaskGroup();
        AutoResetEvent resetEvent = new AutoResetEvent(false);

        private readonly object queueLocker = new object();

        public bool CanDequeue => !currentTaskGroup.IsEmpty || !standbyTaskGroup.IsEmpty;

        public bool Dequeue(out EventItem<T> item)
        {
            if (currentTaskGroup.IsEmpty && standbyTaskGroup.IsEmpty)
            {
                resetEvent.WaitOne();
            }

            lock (queueLocker)
            {
                // 2.整理Group*2
                if (currentTaskGroup.IsEmpty)
                {
                    var temp = currentTaskGroup;
                    currentTaskGroup = standbyTaskGroup;
                    standbyTaskGroup = temp;
                }

                // 1.返回任务Item
                if (!currentTaskGroup.IsEmpty)
                {
                    if (!currentTaskGroup.IsWriteTaskEmpty)
                    {
                        item = (EventItem<T>)currentTaskGroup.GetWriteTask();
                    }
                    else if (!currentTaskGroup.IsReadTaskEmpty)
                    {
                        item = (EventItem<T>)currentTaskGroup.GetReadTask();
                    }
                    else
                    {
                        throw new InvalidOperationException("整理Group出问题了才后走到这里！");
                    }
                }
                else
                {
                    item = null;
                }
            }

            return item != null;
        }

        public void Enqueue(EventItem item, Action blockBeforeHandler = null)
        {
            AutoResetEvent evt;
            lock (queueLocker)
            {

                if (item.EventType == ReadWriteEventType.Read)
                {
                    if (!standbyTaskGroup.IsWriteTaskEmpty)
                    {
                        evt = standbyTaskGroup.Join(item);
                    }
                    else
                    {
                        evt = currentTaskGroup.Join(item);
                    }
                }
                else
                {
                    if (!currentTaskGroup.IsWriteTaskEmpty)
                    {
                        evt = currentTaskGroup.Join(item);
                    }
                    else
                    {
                        if (currentTaskGroup.IsReadTaskEmpty)
                        {
                            evt = currentTaskGroup.Join(item);
                        }
                        else
                        {
                            evt = standbyTaskGroup.Join(item);
                        }
                    }
                }
            }

            blockBeforeHandler?.Invoke();

            resetEvent.Set();
            evt.WaitOne();
        }

        internal class TaskGroup
        {
            private EventItem writeTask;
            private EventItem readTask;

            private readonly AutoResetEvent readResetEvent = new AutoResetEvent(false);
            private readonly AutoResetEvent writeResetEvent = new AutoResetEvent(false);
            internal List<AutoResetEvent> writeResetEvents = new List<AutoResetEvent>();
            internal List<AutoResetEvent> readResetEvents = new List<AutoResetEvent>();

            public bool IsEmpty => writeTask == null && readTask == null;
            public bool IsWriteTaskEmpty => writeTask == null;
            public bool IsReadTaskEmpty => readTask == null;

            public AutoResetEvent Join(EventItem item)
            {

                if (item.EventType == ReadWriteEventType.Write)
                {
                    lock (writeResetEvents)
                    {
                        writeResetEvents.Add(writeResetEvent);
                        // 覆盖旧值，阻塞
                        writeTask = item;
                        item.ResetEvents = writeResetEvents;
                        return writeResetEvent;
                    }
                }
                else
                {
                    lock (readResetEvents)
                    {
                        readResetEvents.Add(readResetEvent);
                        // 阻塞
                        readTask = item;
                        item.ResetEvents = readResetEvents;
                        return readResetEvent;
                    }
                }

            }

            /// <summary>
            /// 获取写操作
            /// </summary>
            /// <returns></returns>
            public EventItem GetWriteTask()
            {
                var result = writeTask;
                writeTask = null;
                return result;
            }

            /// <summary>
            /// 获取读操作
            /// </summary>
            /// <returns></returns>
            public EventItem GetReadTask()
            {
                var result = readTask;
                readTask = null;
                return result;
            }

        }
    }


}
