using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ApeFree.DataStore.Core
{
    public static class StoreExtension
    {
        public static Task LoadAsync(this IStore store)
        {
            return Task.Run(() => store.Load());
        }

        public static Task SaveAsync(this IStore store)
        {
            return Task.Run(() => store.Save());
        }
    }
}
