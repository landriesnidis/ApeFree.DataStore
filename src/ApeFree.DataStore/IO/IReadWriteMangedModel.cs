using System;

namespace ApeFree.DataStore.IO
{
    /// <summary>
    /// 读写管理模型接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadWriteMangedModel<T>
    {
        void Enqueue(EventItem item, Action blockBeforeHandler);

        EventItem<T> Dequeue();
    }

}
