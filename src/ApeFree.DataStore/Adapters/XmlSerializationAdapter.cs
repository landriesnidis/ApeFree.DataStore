using ApeFree.DataStore.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ApeFree.DataStore.Adapters
{
    /// <summary>
    /// Xml序列化器
    /// </summary>
    public class XmlSerializationAdapter : BaseSerializationAdapter
    {
        /// <summary>
        /// 根节点名称
        /// </summary>
        public string RootName { get; }

        private readonly Dictionary<Type, XmlSerializer> dictXmlSerializers;

        public XmlSerializationAdapter(string rootName = null)
        {
            RootName = rootName;
            dictXmlSerializers = new Dictionary<Type, XmlSerializer>();
        }

        /// <inheritdoc/>
        public override T Deserialize<T>(Stream stream)
        {
            var type = typeof(T);
            XmlSerializer serializer = (XmlSerializer)dictXmlSerializers.GetValue(type, new XmlSerializer(type, RootName ?? type.Name));
            T obj = (T)serializer.Deserialize(stream);
            return obj;
        }

        /// <inheritdoc/>
        public override Stream Serialize(object obj)
        {
            var type = obj.GetType();
            using (MemoryStream stream = new MemoryStream())
            {
                XmlSerializer serializer = (XmlSerializer)dictXmlSerializers.GetValue(type, new XmlSerializer(type, RootName ?? type.Name));
                serializer.Serialize(stream, obj);
                return new MemoryStream(stream.ToArray());
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            dictXmlSerializers.Clear();
        }
    }
}
