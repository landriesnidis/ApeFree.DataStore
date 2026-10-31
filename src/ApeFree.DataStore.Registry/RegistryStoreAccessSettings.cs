using ApeFree.DataStore.Core;
using Microsoft.Win32;

namespace ApeFree.DataStore.Registry
{
    /// <summary>
    /// 本地存储器配置
    /// </summary>
    public class RegistryStoreAccessSettings : AccessSettings
    {
        public RegistryHive BaseKey { get; private set; }
        public string Path { get; private set; }
        public string Key { get; private set; }

        public RegistryStoreAccessSettings(RegistryHive baseKey, string path, string key)
        {
            BaseKey = baseKey;
            Path = path;
            Key = key;
        }
    }
}
