using System;
using System.IO;
using System.IO.Compression;
using ApeFree.DataStore.Filters;

namespace ApeFree.DataStore.Adapters
{
    /// <summary>
    /// Deflate 压缩适配器（兼容包装）
    /// </summary>
    [Obsolete("建议直接使用 DeflateStreamFilter")]
    public class DeflateCompressionAdapter : BaseCompressionAdapter
    {
        private readonly DeflateStreamFilter filter = new DeflateStreamFilter();

        public override Stream Compress(Stream stream) => filter.WrapWriteStream(stream);
        public override Stream Decompress(Stream stream) => filter.WrapReadStream(stream);

        public override Stream WrapWriteStream(Stream outputStream) => filter.WrapWriteStream(outputStream);
        public override Stream WrapReadStream(Stream inputStream) => filter.WrapReadStream(inputStream);
    }
}
