using ApeFree.DataStore.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ApeFree.DataStore.Adapters
{
    /// <summary>
    /// Json序列化器
    /// </summary>
    public class JsonSerializationAdapter : ISerializationAdapter
    {
        /// <summary>
        /// 默认的Json序列化配置
        /// 私有静态，此对象用作全局的默认配置，不允许被外部获取，以免被修改
        /// </summary>
        private readonly static Lazy<JsonSerializerSettings> serializerSettings = new Lazy<JsonSerializerSettings>(() =>
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
        /// Json序列化配置
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; }

        /// <summary>
        /// 字符编码
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// 构造Json格式的序列化适配器
        /// </summary>
        /// <param name="serializerSettings"></param>
        public JsonSerializationAdapter(JsonSerializerSettings serializerSettings = null)
        {
            SerializerSettings = serializerSettings;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="T"><inheritdoc/></typeparam>
        /// <param name="stream"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public T Deserialize<T>(Stream stream)
        {
            StreamReader reader = new StreamReader(stream, Encoding);
            var json = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<T>(json, SerializerSettings ?? serializerSettings.Value);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="obj"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public Stream Serialize(object obj)
        {
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented, SerializerSettings ?? serializerSettings.Value);
            var bytes = Encoding.GetBytes(json);
            return new MemoryStream(bytes);
        }

        public void Dispose() { }
    }
}
