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

        public EventItem<T> Dequeue()
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
                        return (EventItem<T>)currentTaskGroup.GetWriteTask();
                    }
                    else if (!currentTaskGroup.IsReadTaskEmpty)
                    {
                        return (EventItem<T>)currentTaskGroup.GetReadTask();
                    }
                    else
                    {
                        throw new InvalidOperationException("整理Group出问题了才后走到这里！");
                    }
                }
                else
                {
                    return null;
                }

            }
        }

        public void Enqueue(EventItem item)
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

            resetEvent.Set();
            evt.WaitOne();
        }
    }
}
