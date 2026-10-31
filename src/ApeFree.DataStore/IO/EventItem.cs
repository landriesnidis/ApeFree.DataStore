using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApeFree.DataStore.IO
{
    /// <summary>
    /// 读写事件项
    /// </summary>
    public class EventItem
    {
        public ReadWriteEventType EventType { get; set; }
        internal List<TaskCompletionSource<bool>> CompletionSources { get; } = new List<TaskCompletionSource<bool>>();
        internal object InnerValue { get; set; }

        public EventItem(ReadWriteEventType eventType, object innerValue = null)
        {
            EventType = eventType;
            InnerValue = innerValue;
        }

        /// <summary>
        /// 成功释放并唤醒所有等待者
        /// </summary>
        public void Release()
        {
            lock (CompletionSources)
            {
                foreach (var tcs in CompletionSources)
                {
                    tcs.TrySetResult(true);
                }
                CompletionSources.Clear();
            }
        }

        /// <summary>
        /// 发生异常时通知所有等待者
        /// </summary>
        public void Fail(Exception ex)
        {
            lock (CompletionSources)
            {
                foreach (var tcs in CompletionSources)
                {
                    tcs.TrySetException(ex);
                }
                CompletionSources.Clear();
            }
        }
    }

    /// <summary>
    /// 泛型读写事件项
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
