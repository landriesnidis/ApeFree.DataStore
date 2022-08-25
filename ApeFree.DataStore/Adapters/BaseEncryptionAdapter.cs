using ApeFree.DataStore.Core;
using System.IO;

namespace ApeFree.DataStore.Adapters
{
    public abstract class BaseEncryptionAdapter : IEncryptionAdapter
    {
        public abstract Stream Decode(Stream stream);
        public abstract void Dispose();
        public abstract Stream Encode(Stream stream);
    }
}
