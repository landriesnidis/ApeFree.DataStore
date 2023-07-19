using ApeFree.DataStore.Core;
using System.IO;

namespace ApeFree.DataStore.Adapters
{
    public abstract class BaseSerializationAdapter : ISerializationAdapter
    {
        /// <inheritdoc/>
        public abstract T Deserialize<T>(Stream stream);

        /// <inheritdoc/>
        public abstract Stream Serialize(object obj);

        /// <inheritdoc/>
        public abstract void Dispose();
    }
}
