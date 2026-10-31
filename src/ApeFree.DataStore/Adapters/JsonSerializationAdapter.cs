using System;
using System.IO;
using System.Text;
using ApeFree.DataStore.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ApeFree.DataStore.Adapters
{
    /// <summary>
    /// JSON 格式流式序列化适配器
    /// 直接对接底层流，避免内存中生成巨大临时字符串
    /// </summary>
    public class JsonSerializationAdapter : BaseSerializationAdapter
    {
        private static readonly Lazy<JsonSerializerSettings> defaultSettings = new Lazy<JsonSerializerSettings>(() =>
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            };
            settings.Converters.Add(new StringEnumConverter());
            return settings;
        });

        /// <summary>
        /// JSON 序列化配置
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; }

        /// <summary>
        /// 字符编码
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public JsonSerializationAdapter(JsonSerializerSettings serializerSettings = null)
        {
            SerializerSettings = serializerSettings ?? defaultSettings.Value;
        }

        /// <inheritdoc/>
        public override void Serialize(object obj, Stream stream)
        {
            if (obj == null || stream == null) return;

            using (var writer = new StreamWriter(stream, Encoding, 4096, leaveOpen: true))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                jsonWriter.Formatting = SerializerSettings.Formatting;
                var serializer = JsonSerializer.CreateDefault(SerializerSettings);
                serializer.Serialize(jsonWriter, obj);
                jsonWriter.Flush();
                writer.Flush();
            }
        }

        /// <inheritdoc/>
        public override object Deserialize(Stream stream, Type type)
        {
            if (stream == null || type == null) return null;

            using (var reader = new StreamReader(stream, Encoding, true, 4096, leaveOpen: true))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var serializer = JsonSerializer.CreateDefault(SerializerSettings);
                return serializer.Deserialize(jsonReader, type);
            }
        }
    }
}
