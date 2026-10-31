using System;
using System.IO;

namespace ApeFree.DataStore.Core
{
    /// <summary>
    /// 序列化适配器
    /// </summary>
    public interface ISerializationAdapter : IDisposable
    {
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="obj">需要被序列化的对象</param>
        /// <returns>序列化后的数据流</returns>
        Stream Serialize(object obj);

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="stream">被序列化对象的数据流</param>
        /// <returns></returns>
        T Deserialize<T>(Stream stream);
    }
}
