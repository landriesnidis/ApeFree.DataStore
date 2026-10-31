using System;
using System.IO;

namespace ApeFree.DataStore.Core
{
    /// <summary>
    /// 加密适配器接口（兼容老版本）
    /// </summary>
    [Obsolete("建议使用 Filters 命名空间下的 IStreamFilter，如 AesEncryptionStreamFilter")]
    public interface IEncryptionAdapter : IStreamFilter
    {
        Stream Encode(Stream stream);
        Stream Decode(Stream stream);
    }
}
