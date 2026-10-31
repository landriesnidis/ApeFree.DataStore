using System;
using System.Threading.Tasks;

namespace ApeFree.DataStore.Core
{
    /// <summary>
    /// Store 便捷扩展方法
    /// </summary>
    public static class StoreExtension
    {
        /// <summary>
        /// 重新加载数据
        /// </summary>
        public static void Reload(this IStore store)
        {
            store.Load();
        }

        /// <summary>
        /// 异步重新加载数据
        /// </summary>
        public static Task ReloadAsync(this IStore store)
        {
            return store.LoadAsync();
        }
    }
}
