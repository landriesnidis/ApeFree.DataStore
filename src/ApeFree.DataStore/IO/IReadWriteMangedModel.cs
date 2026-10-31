using System;
using System.Threading.Tasks;

namespace ApeFree.DataStore.IO
{
    /// <summary>
    /// 读写事务管理模型接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadWriteMangedModel<T>
    {
        /// <summary>
        /// 异步入队并等待执行完成
        /// </summary>
        /// <param name="item">读写事件项</param>
        /// <param name="ensureConsumerRunning">确保消费循环运行的回调</param>
        /// <returns>等待任务</returns>
        Task EnqueueAsync(EventItem item, Action ensureConsumerRunning);

        /// <summary>
        /// 取出下一个待执行任务（若无任务返回 null）
        /// </summary>
        /// <returns></returns>
        EventItem<T> Dequeue();
    }
}
