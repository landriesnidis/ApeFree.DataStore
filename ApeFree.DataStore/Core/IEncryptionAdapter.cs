using System;
using System.IO;

namespace ApeFree.DataStore.Core
{
    /// <summary>
    /// 加密适配器
    /// </summary>
    public interface IEncryptionAdapter : IDisposable
    {
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        Stream Encode(Stream stream);

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        Stream Decode(Stream stream);

    }
}
