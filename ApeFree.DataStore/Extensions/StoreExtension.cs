using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ApeFree.DataStore.Core
{
    public static class StoreExtension
    {
        /// <summary>
        /// 异步加载数据
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        public static Task LoadAsync(this IStore store)
        {
            return Task.Run(() => store.Load());
        }

        /// <summary>
        /// 异步保存数据
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        public static Task SaveAsync(this IStore store)
        {
            return Task.Run(() => store.Save());
        }
    }
}
