using System;
using System.IO;
using System.IO.Compression;
using ApeFree.DataStore.Core;

namespace ApeFree.DataStore.Filters
{
    /// <summary>
    /// GZip 算法流式压缩过滤器
    /// </summary>
    public class GZipStreamFilter : IStreamFilter
    {
        /// <summary>
        /// 压缩等级
        /// </summary>
        public CompressionLevel CompressionLevel { get; }

        public GZipStreamFilter(CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            CompressionLevel = compressionLevel;
        }

        /// <inheritdoc/>
        public Stream WrapWriteStream(Stream outputStream)
        {
            return new GZipStream(outputStream, CompressionLevel, leaveOpen: true);
        }

        /// <inheritdoc/>
        public Stream WrapReadStream(Stream inputStream)
        {
            return new GZipStream(inputStream, CompressionMode.Decompress, leaveOpen: true);
        }

        /// <inheritdoc/>
        public void Dispose() { }
    }
}
