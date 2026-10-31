using ApeFree.DataStore.Core;
using System.IO;

namespace ApeFree.DataStore.Adapters
{
    public abstract class BaseCompressionAdapter : ICompressionAdapter
    {
        /// <inheritdoc/>
        public abstract Stream Compress(Stream stream);

        /// <inheritdoc/>
        public abstract Stream Decompress(Stream stream);

        /// <inheritdoc/>
        public abstract void Dispose();
    }
}
