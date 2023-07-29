using ApeFree.DataStore.Local;

namespace ApeFree.DataStore
{
    public static class LocalStoreFactoryExtensions
    {
        /// <summary>
        /// 创建一个本地(对象)存储器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static LocalStore<T> CreateLocalStore<T>(this StoreFactory _, LocalStoreAccessSettings settings) where T : class
        {
            return new LocalStore<T>(settings);
        }
    }
}
