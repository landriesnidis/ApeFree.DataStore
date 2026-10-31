using System;
using System.Collections.Concurrent;
using System.IO;
using System.Xml.Serialization;
using ApeFree.DataStore.Core;

namespace ApeFree.DataStore.Adapters
{
    /// <summary>
    /// XML 格式流式序列化适配器
    /// </summary>
    public class XmlSerializationAdapter : BaseSerializationAdapter
    {
        /// <summary>
        /// 根节点名称
        /// </summary>
        public string RootName { get; }

        private readonly ConcurrentDictionary<Type, XmlSerializer> serializerCache = new ConcurrentDictionary<Type, XmlSerializer>();

        public XmlSerializationAdapter(string rootName = null)
        {
            RootName = rootName;
        }

        private XmlSerializer GetSerializer(Type type)
        {
            return serializerCache.GetOrAdd(type, t =>
                string.IsNullOrEmpty(RootName) ? new XmlSerializer(t) : new XmlSerializer(t, new XmlRootAttribute(RootName)));
        }

        /// <inheritdoc/>
        public override void Serialize(object obj, Stream stream)
        {
            if (obj == null || stream == null) return;
            var serializer = GetSerializer(obj.GetType());
            serializer.Serialize(stream, obj);
        }

        /// <inheritdoc/>
        public override object Deserialize(Stream stream, Type type)
        {
            if (stream == null || type == null) return null;
            var serializer = GetSerializer(type);
            return serializer.Deserialize(stream);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            serializerCache.Clear();
        }
    }
}
