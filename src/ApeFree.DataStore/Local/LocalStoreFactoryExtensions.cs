using ApeFree.DataStore.Local;

namespace ApeFree.DataStore
{
    /// <summary>
    /// 本地存储器工厂扩展方法
    /// </summary>
    public static class LocalStoreFactoryExtensions
    {
        /// <summary>
        /// 创建一个本地文件对象存储器
        /// </summary>
        /// <typeparam name="T">存储对象类型</typeparam>
        /// <param name="_">工厂实例</param>
        /// <param name="settings">本地存储配置</param>
        /// <returns>本地对象存储器实例</returns>
        public static LocalStore<T> CreateLocalStore<T>(this StoreFactory _, LocalStoreAccessSettings settings) where T : class
        {
            return new LocalStore<T>(settings);
        }

        /// <summary>
        /// 创建一个本地文件对象存储器（快捷方式）
        /// </summary>
        /// <typeparam name="T">存储对象类型</typeparam>
        /// <param name="_">工厂实例</param>
        /// <param name="filePath">本地保存路径</param>
        /// <returns>本地对象存储器实例</returns>
        public static LocalStore<T> CreateLocalStore<T>(this StoreFactory _, string filePath) where T : class
        {
            return new LocalStore<T>(new LocalStoreAccessSettings(filePath));
        }
    }
}
