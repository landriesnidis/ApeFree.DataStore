using System;
using System.IO;
using System.Reflection;
using ApeFree.DataStore.Core;
using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Jbin.Binders;
using Newtonsoft.Json;

namespace ApeFree.DataStore.Adapters
{
    /// <summary>
    /// Jbin 二进制混合序列化适配器（极限性能版本）
    /// 深度集成 Jbin 原生 CachedContractResolver 与基元零拷贝内存映射；
    /// 端到端纯流式读写，彻底消除中间全量大字节数组分配；
    /// 强制生命周期管理，及时释放 DataBlocks 内存引用。
    /// </summary>
    public class JbinSerializationAdapter : BaseSerializationAdapter
    {
        /// <summary>
        /// Jbin 序列化配置
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; }

        /// <summary>
        /// 构造 Jbin 序列化适配器
        /// </summary>
        /// <param name="serializerSettings">自定义配置，为 null 时自动使用 Jbin 原生高性能默认配置</param>
        public JbinSerializationAdapter(JsonSerializerSettings serializerSettings = null)
        {
            SerializerSettings = serializerSettings ?? JbinObject.JsonSerializerSettings;
        }

        /// <summary>
        /// 构造带有程序集白名单安全防御的 Jbin 序列化适配器
        /// </summary>
        /// <param name="trustedAssemblies">受信任的程序集列表</param>
        public JbinSerializationAdapter(params Assembly[] trustedAssemblies)
        {
            SerializerSettings = JbinObject.JsonSerializerSettings;
            if (trustedAssemblies != null && trustedAssemblies.Length > 0)
            {
                SerializerSettings.SerializationBinder = new AssemblyWhitelistSerializationBinder(trustedAssemblies);
            }
        }

        /// <inheritdoc/>
        public override void Serialize(object obj, Stream stream)
        {
            if (obj == null || stream == null) return;

            // 构造 JbinObject，内部自动提取骨架与二进制数据块
            using (var jbin = JbinObject.FromObject(obj, SerializerSettings))
            {
                // 端到端直接写入目标输出流，无中间 byte[] 堆分配
                jbin.WriteTo(stream);
            }
        }

        /// <inheritdoc/>
        public override object Deserialize(Stream stream, Type type)
        {
            if (stream == null || type == null) return null;

            // 从流中流式解析 Jbin 帧
            using (var jbin = JbinObject.Parse(stream))
            {
                var result = jbin.ToObject(type, SerializerSettings);
                return result;
            } // 离开 using 作用域立即清理 DataBlocks 内存引用
        }
    }
}
