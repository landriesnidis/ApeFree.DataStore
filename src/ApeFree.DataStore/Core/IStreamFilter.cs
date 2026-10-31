using System;
using System.IO;

namespace ApeFree.DataStore.Core
{
    /// <summary>
    /// 流过滤器接口（用于装饰器流管道）
    /// 支持在持久化数据流经管道时，对其进行双向包装（如压缩、解压、加密、解密、校验等）
    /// </summary>
    public interface IStreamFilter : IDisposable
    {
        /// <summary>
        /// 包装写入流（保存时由顶向下装饰目标流）
        /// </summary>
        /// <param name="outputStream">下一级输出流</param>
        /// <returns>包装后的流</returns>
        Stream WrapWriteStream(Stream outputStream);

        /// <summary>
        /// 包装读取流（加载时由底向上装饰输入流）
        /// </summary>
        /// <param name="inputStream">上一级输入流</param>
        /// <returns>包装后的流</returns>
        Stream WrapReadStream(Stream inputStream);
    }
}
