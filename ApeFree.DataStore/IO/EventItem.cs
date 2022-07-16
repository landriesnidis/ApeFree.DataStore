using System.Collections.Generic;
using System.Threading;

namespace ApeFree.DataStore.IO
{
    public class EventItem
    {
        public ReadWriteEventType EventType { get; set; }
        internal List<AutoResetEvent> ResetEvents { get; set; }
        internal object InnerValue { get; set; }
        public void Release()
        {

            lock (ResetEvents)
            {
                //ResetEvents.ForEach(x => x.Set());
                //ResetEvents.Clear();

                //var taskList = new List<AutoResetEvent>();
                //ResetEvents.ForEach(x => taskList.Add(x));

                foreach (AutoResetEvent e in ResetEvents)
                {
                    e.Set();
                }
                ResetEvents.Clear();
            }
        }

        public EventItem(ReadWriteEventType eventType, object innerValue = null)
        {
            EventType = eventType;
            InnerValue = innerValue;
        }
    }

    public class EventItem<T> : EventItem
    {
        public T Value
        {
            get => (T)InnerValue;
            set => InnerValue = value;
        }

        public EventItem(ReadWriteEventType eventType, T value = default) : base(eventType, value) { }
    }

}
