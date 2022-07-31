using ApeFree.DataStore.Registry;
using Microsoft.Win32;

namespace ApeFree.DataStore
{
    public static class RegistryStoreFactoryExtensions
    {
        /// <summary>
        /// 创建一个注册表(对象)储存器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static RegistryStore<T> CreateRegistryStore<T>(this StoreFactory _, RegistryStoreAccessSettings settings) where T : class
        {
            return new RegistryStore<T>(settings);
        }
    }
}
