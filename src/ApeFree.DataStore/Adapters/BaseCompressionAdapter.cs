using System;
using System.IO;
using ApeFree.DataStore.Core;

namespace ApeFree.DataStore.Adapters
{
    /// <summary>
    /// 压缩适配器基类（兼容老版本）
    /// </summary>
    [Obsolete("建议使用 Filters 命名空间下的 IStreamFilter，如 GZipStreamFilter")]
    public abstract class BaseCompressionAdapter : ICompressionAdapter
    {
        public abstract Stream Compress(Stream stream);
        public abstract Stream Decompress(Stream stream);

        public virtual Stream WrapWriteStream(Stream outputStream) => Compress(outputStream);
        public virtual Stream WrapReadStream(Stream inputStream) => Decompress(inputStream);

        public virtual void Dispose() { }
    }
}
