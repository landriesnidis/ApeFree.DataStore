using ApeFree.DataStore.Core;
using System.IO;
using System.IO.Compression;

namespace ApeFree.DataStore.Adapters
{
    /// <summary>
    /// Deflate算法压缩适配器
    /// </summary>
    public class DeflateCompressionAdapter : BaseCompressionAdapter
    {
        /// <inheritdoc/>
        public override Stream Compress(Stream stream)
        {
            MemoryStream compressStream = new MemoryStream();
            using (var zipStream = new DeflateStream(compressStream, CompressionMode.Compress, true))
            {
                stream.CopyTo(zipStream);
            }
            return compressStream;
        }

        /// <inheritdoc/>
        public override Stream Decompress(Stream stream)
        {
            return new DeflateStream(stream, CompressionMode.Decompress);
        }

        /// <inheritdoc/>
        public override void Dispose() { }
    }
}
