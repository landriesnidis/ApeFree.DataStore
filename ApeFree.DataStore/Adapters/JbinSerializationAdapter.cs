using ApeFree.DataStore.Core;
using ApeFree.Protocols.Json.Jbin;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ApeFree.DataStore.Adapters
{
    /// <summary>
    /// Jbin序列化器
    /// </summary>
    public class JbinSerializationAdapter : BaseSerializationAdapter
    {
        /// <summary>
        /// 默认的Jbin序列化配置
        /// 私有静态，此对象用作全局的默认配置，不允许被外部获取，以免被修改
        /// </summary>
        protected readonly static Lazy<JsonSerializerSettings> serializerSettings = new Lazy<JsonSerializerSettings>(() =>
        {
            return new JsonSerializerSettings().With(s =>
            {
                s.NullValueHandling = NullValueHandling.Ignore;
                s.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                s.Converters.Add(new StringEnumConverter());
                s.TypeNameHandling = TypeNameHandling.All;
            });
        });

        /// <summary>
        /// Jbin序列化配置
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; }

        /// <summary>
        /// 字符编码
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// 构造Jbin格式的序列化适配器
        /// </summary>
        /// <param name="serializerSettings"></param>
        public JbinSerializationAdapter(JsonSerializerSettings serializerSettings = null)
        {
            SerializerSettings = serializerSettings;
        }

        /// <inheritdoc/>
        public override T Deserialize<T>(Stream stream)
        {
            var jo = JbinObject.Parse(stream);
            var obj = jo.ToObject<T>(SerializerSettings ?? serializerSettings.Value);

            return obj;
        }

        /// <inheritdoc/>
        public override Stream Serialize(object obj)
        {
            var jo = JbinObject.FromObject(obj, SerializerSettings ?? serializerSettings.Value);
            var bytes = jo.ToBytes();
            return new MemoryStream(bytes);
        }

        /// <inheritdoc/>
        public override void Dispose() { }
    }

}
