using System;
using System.IO;
using System.IO.Compression;
using ApeFree.DataStore.Filters;

namespace ApeFree.DataStore.Adapters
{
    /// <summary>
    /// GZip 压缩适配器（兼容包装）
    /// </summary>
    [Obsolete("建议直接使用 GZipStreamFilter")]
    public class GZipCompressionAdapter : BaseCompressionAdapter
    {
        private readonly GZipStreamFilter filter = new GZipStreamFilter();

        public override Stream Compress(Stream stream) => filter.WrapWriteStream(stream);
        public override Stream Decompress(Stream stream) => filter.WrapReadStream(stream);

        public override Stream WrapWriteStream(Stream outputStream) => filter.WrapWriteStream(outputStream);
        public override Stream WrapReadStream(Stream inputStream) => filter.WrapReadStream(inputStream);
    }
}
