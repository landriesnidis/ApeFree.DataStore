using System;
using System.IO;

namespace ApeFree.DataStore.Core
{
    /// <summary>
    /// 序列化适配器接口
    /// 负责对象与数据流之间的流式互相转换
    /// </summary>
    public interface ISerializationAdapter : IDisposable
    {
        /// <summary>
        /// 将对象序列化并直接写入目标流
        /// </summary>
        /// <param name="obj">待序列化对象</param>
        /// <param name="stream">输出目标流</param>
        void Serialize(object obj, Stream stream);

        /// <summary>
        /// 从输入流中读取并反序列化为指定类型的对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="stream">输入源流</param>
        /// <returns>反序列化后的对象</returns>
        T Deserialize<T>(Stream stream);

        /// <summary>
        /// 从输入流中读取并反序列化为指定类型的对象
        /// </summary>
        /// <param name="stream">输入源流</param>
        /// <param name="type">目标对象类型</param>
        /// <returns>反序列化后的对象</returns>
        object Deserialize(Stream stream, Type type);
    }
}
