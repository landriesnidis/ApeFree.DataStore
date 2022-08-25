using ApeFree.DataStore.Core;
using System.IO;

namespace ApeFree.DataStore.Adapters
{
    public abstract class BaseCompressionAdapter : ICompressionAdapter
    {
        public abstract Stream Compress(Stream stream);
        public abstract Stream Decompress(Stream stream);
        public abstract void Dispose();
    }
}
