using System;
using System.IO;

namespace ApeFree.DataStore.Core
{
    /// <summary>
    /// 压缩适配器
    /// </summary>
    public interface ICompressionAdapter: IDisposable
    {
        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        Stream Compress(Stream stream);

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        Stream Decompress(Stream stream);
    }
}
