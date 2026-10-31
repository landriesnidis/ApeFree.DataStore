using System;
using System.IO;
using ApeFree.DataStore.Core;

namespace ApeFree.DataStore.Adapters
{
    /// <summary>
    /// 加密适配器基类（兼容老版本）
    /// </summary>
    [Obsolete("建议使用 Filters 命名空间下的 IStreamFilter，如 AesEncryptionStreamFilter")]
    public abstract class BaseEncryptionAdapter : IEncryptionAdapter
    {
        public abstract Stream Encode(Stream stream);
        public abstract Stream Decode(Stream stream);

        public virtual Stream WrapWriteStream(Stream outputStream) => Encode(outputStream);
        public virtual Stream WrapReadStream(Stream inputStream) => Decode(inputStream);

        public virtual void Dispose() { }
    }
}
