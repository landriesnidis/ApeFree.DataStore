using System;
using System.IO;

namespace ApeFree.DataStore.Core
{
    /// <summary>
    /// 压缩适配器接口（兼容老版本）
    /// </summary>
    [Obsolete("建议使用 Filters 命名空间下的 IStreamFilter，如 GZipStreamFilter")]
    public interface ICompressionAdapter : IStreamFilter
    {
        Stream Compress(Stream stream);
        Stream Decompress(Stream stream);
    }
}
