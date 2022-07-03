using ApeFree.DataStore.Local;

namespace ApeFree.DataStore
{
    public static class LoaclStoreFactoryExtensions
    {
        /// <summary>
        /// 创建一个本地(对象)存储器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static LoaclStore<T> CreateLoaclStore<T>(this StoreFactory _, LoaclStoreAccessSettings settings) where T : new()
        {
            return new LoaclStore<T>(settings);
        }
    }
}
