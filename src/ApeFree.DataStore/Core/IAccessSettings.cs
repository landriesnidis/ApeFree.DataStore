using System;
using System.Collections.Generic;

namespace ApeFree.DataStore.Core
{
    /// <summary>
    /// 数据存取配置接口
    /// </summary>
    public interface IAccessSettings : IDisposable
    {
        /// <summary>
        /// 序列化适配器
        /// </summary>
        ISerializationAdapter SerializationAdapter { get; set; }

        /// <summary>
        /// 流式过滤器管道列表（按列表顺序依次包装）
        /// </summary>
        IList<IStreamFilter> StreamFilters { get; }
    }
}
