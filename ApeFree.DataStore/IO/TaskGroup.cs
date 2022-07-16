using System.Collections.Generic;
using System.Threading;

namespace ApeFree.DataStore.IO
{
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
