using System;
using System.Collections.Generic;
using System.Text;

namespace ApeFree.DataStore.Core
{
    public interface IAccessSettings : IDisposable
    {
        /// <summary>
        /// 序列化适配器
        /// </summary>
        ISerializationAdapter SerializationAdapter { get; }

        /// <summary>
        /// 压缩适配器
        /// </summary>
        ICompressionAdapter CompressionAdapter { get; }

        /// <summary>
        /// 加密适配器
        /// </summary>
        IEncryptionAdapter EncryptionAdapter { get; }
    }
}
