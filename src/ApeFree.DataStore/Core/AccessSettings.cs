using System;
using System.Collections.Generic;
using ApeFree.DataStore.Adapters;

namespace ApeFree.DataStore.Core
{
    /// <summary>
    /// 数据存取配置抽象基类
    /// </summary>
    public abstract class AccessSettings : IAccessSettings
    {
        private ISerializationAdapter serializationAdapter;
        private ICompressionAdapter compressionAdapter;
        private IEncryptionAdapter encryptionAdapter;

        /// <inheritdoc/>
        public ISerializationAdapter SerializationAdapter
        {
            get => serializationAdapter ?? (serializationAdapter = new JbinSerializationAdapter());
            set => serializationAdapter = value;
        }

        /// <inheritdoc/>
        public IList<IStreamFilter> StreamFilters { get; } = new List<IStreamFilter>();

        /// <summary>
        /// 兼容属性：获取或设置压缩适配器
        /// </summary>
        [Obsolete("建议直接使用 StreamFilters.Add(new GZipStreamFilter())")]
        public ICompressionAdapter CompressionAdapter
        {
            get => compressionAdapter;
            set
            {
                if (compressionAdapter != null) StreamFilters.Remove(compressionAdapter);
                compressionAdapter = value;
                if (compressionAdapter != null && !StreamFilters.Contains(compressionAdapter))
                {
                    StreamFilters.Add(compressionAdapter);
                }
            }
        }

        /// <summary>
        /// 兼容属性：获取或设置加密适配器
        /// </summary>
        [Obsolete("建议直接使用 StreamFilters.Add(new AesEncryptionStreamFilter(...))")]
        public IEncryptionAdapter EncryptionAdapter
        {
            get => encryptionAdapter;
            set
            {
                if (encryptionAdapter != null) StreamFilters.Remove(encryptionAdapter);
                encryptionAdapter = value;
                if (encryptionAdapter != null && !StreamFilters.Contains(encryptionAdapter))
                {
                    StreamFilters.Add(encryptionAdapter);
                }
            }
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            serializationAdapter?.Dispose();
            foreach (var filter in StreamFilters)
            {
                filter?.Dispose();
            }
        }
    }
}
