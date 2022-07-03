using ApeFree.DataStore.Core;

namespace ApeFree.DataStore.Local
{
    /// <summary>
    /// 本地存储器配置
    /// </summary>
    public class LoaclStoreAccessSettings : AccessSettings
    {
        /// <summary>
        /// 本地路存储径
        /// </summary>
        public string SavePath { get; }

        public LoaclStoreAccessSettings(string savePath)
        {
            SavePath = savePath;
        }
    }
}
