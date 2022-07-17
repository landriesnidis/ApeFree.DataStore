using STTech.CodePlus.Threading;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApeFree.DataStore.Core
{
    /// <summary>
    /// 复用线程池管理器
    /// </summary>
    internal class ReusableThreadPoolManager
    {
        /// <summary>
        /// 加了Lazy之后，只有当使用到的时候才加载
        /// </summary>
        private static readonly Lazy<ReusableThreadPoolManager> single = new Lazy<ReusableThreadPoolManager>(() => new ReusableThreadPoolManager());
        public static ReusableThreadPoolManager Instance => single.Value;

        public ReusableThreadPool Pool { get; }
        private ReusableThreadPoolManager() {
            Pool = new ReusableThreadPool();
        }

    }
}
