using ApeFree.DataStore.Core;
using System.IO;

namespace ApeFree.DataStore.Adapters
{
    public abstract class BaseSerializationAdapter : ISerializationAdapter
    {
        public abstract T Deserialize<T>(Stream stream);
        public abstract void Dispose();
        public abstract Stream Serialize(object obj);
    }
}
