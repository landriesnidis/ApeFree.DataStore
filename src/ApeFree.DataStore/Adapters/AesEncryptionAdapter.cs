using System;
using System.IO;
using System.Security.Cryptography;
using ApeFree.DataStore.Filters;

namespace ApeFree.DataStore.Adapters
{
    /// <summary>
    /// AES 加密适配器（兼容包装）
    /// </summary>
    [Obsolete("建议直接使用 AesEncryptionStreamFilter")]
    public class AesEncryptionAdapter : BaseEncryptionAdapter
    {
        private readonly AesEncryptionStreamFilter filter;

        public AesEncryptionAdapter(byte[] key, byte[] iv)
        {
            filter = new AesEncryptionStreamFilter(key, iv);
        }

        public override Stream Encode(Stream stream) => filter.WrapWriteStream(stream);
        public override Stream Decode(Stream stream) => filter.WrapReadStream(stream);

        public override Stream WrapWriteStream(Stream outputStream) => filter.WrapWriteStream(outputStream);
        public override Stream WrapReadStream(Stream inputStream) => filter.WrapReadStream(inputStream);
    }
}
