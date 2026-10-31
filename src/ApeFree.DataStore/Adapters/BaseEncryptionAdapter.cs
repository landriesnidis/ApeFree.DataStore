using ApeFree.DataStore.Core;
using System.IO;

namespace ApeFree.DataStore.Adapters
{
    public abstract class BaseEncryptionAdapter : IEncryptionAdapter
    {
        /// <inheritdoc/>
        public abstract Stream Decode(Stream stream);

        /// <inheritdoc/>
        public abstract void Dispose();

        /// <inheritdoc/>
        public abstract Stream Encode(Stream stream);
    }
}
