using System;
using System.IO;
using System.IO.Compression;
using ApeFree.DataStore.Core;

namespace ApeFree.DataStore.Filters
{
    /// <summary>
    /// Deflate 算法流式压缩过滤器
    /// </summary>
    public class DeflateStreamFilter : IStreamFilter
    {
        /// <summary>
        /// 压缩等级
        /// </summary>
        public CompressionLevel CompressionLevel { get; }

        public DeflateStreamFilter(CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            CompressionLevel = compressionLevel;
        }

        /// <inheritdoc/>
        public Stream WrapWriteStream(Stream outputStream)
        {
            return new DeflateStream(outputStream, CompressionLevel, leaveOpen: true);
        }

        /// <inheritdoc/>
        public Stream WrapReadStream(Stream inputStream)
        {
            return new DeflateStream(inputStream, CompressionMode.Decompress, leaveOpen: true);
        }

        /// <inheritdoc/>
        public void Dispose() { }
    }
}
