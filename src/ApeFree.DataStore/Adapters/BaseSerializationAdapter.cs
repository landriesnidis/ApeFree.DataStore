using System;
using System.IO;
using ApeFree.DataStore.Core;

namespace ApeFree.DataStore.Adapters
{
    /// <summary>
    /// 序列化适配器抽象基类
    /// </summary>
    public abstract class BaseSerializationAdapter : ISerializationAdapter
    {
        /// <inheritdoc/>
        public abstract void Serialize(object obj, Stream stream);

        /// <inheritdoc/>
        public virtual T Deserialize<T>(Stream stream)
        {
            return (T)Deserialize(stream, typeof(T));
        }

        /// <inheritdoc/>
        public abstract object Deserialize(Stream stream, Type type);

        /// <inheritdoc/>
        public virtual void Dispose() { }
    }
}
