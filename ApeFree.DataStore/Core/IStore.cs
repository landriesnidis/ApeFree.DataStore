using System;

namespace ApeFree.DataStore.Core
{
    public interface IStore:IDisposable
    {
        /// <summary>
        /// 加载数据
        /// </summary>
        void Load();

        /// <summary>
        /// 保存数据
        /// </summary>
        void Save();
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
