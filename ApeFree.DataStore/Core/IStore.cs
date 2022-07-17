using System;
using System.Threading.Tasks;

namespace ApeFree.DataStore.Core
{
    public interface IStore : IDisposable
    {
        /// <summary>
        /// 加载数据
        /// </summary>
        void Load();

        /// <summary>
        /// 异步加载数据
        /// </summary>
        /// <returns></returns>
        Task LoadAsync();

        /// <summary>
        /// 保存数据
        /// </summary>
        void Save();

        /// <summary>
        /// 异步保存数据
        /// </summary>
        /// <returns></returns>
        Task SaveAsync();
    }

    public interface IStore<TValue> : IStore
        where TValue : new()
    {
        /// <summary>
        /// 存储对象
        /// </summary>
        TValue Value { get; }
    }
}
