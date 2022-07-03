using ApeFree.DataStore.Core;
using Microsoft.Win32;
using System;
using System.IO;

namespace ApeFree.DataStore.Registry
{
    /// <summary>
    /// 注册表(对象)储存器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RegistryStore<T> : Store<T, RegistryStoreAccessSettings> where T : new()
    {
        private static readonly RegistryView registryView
            = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;

        /// <summary>
        /// 构造注册表(对象)储存器
        /// </summary>
        /// <param name="settings"></param>
        public RegistryStore(RegistryStoreAccessSettings settings) : base(settings) { }

        public override void Load()
        {
            using (var rklm = RegistryKey.OpenBaseKey(AccessSettings.BaseKey, registryView))
            {
                var rk = rklm.OpenSubKey(AccessSettings.Path, true);
                if (rk == null)
                {
                    Value = Activator.CreateInstance<T>();
                    Save();
                }
                else
                {
                    using (rk)
                    {
                        var bytes = (byte[])rk.GetValue(AccessSettings.Key);
                        LoadHandler(new MemoryStream(bytes));
                    }
                }
            }
        }

        public override void Save()
        {
            SaveHandler(stream =>
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    var bytes = memoryStream.ToArray();

                    using (var rklm = RegistryKey.OpenBaseKey(AccessSettings.BaseKey, registryView))
                    {
                        var rk = rklm.OpenSubKey(AccessSettings.Path, true);
                        if (rk == null)
                        {
                            rk = rklm.CreateSubKey(AccessSettings.Path);
                        }

                        using (rk)
                        {
                            rk.SetValue(AccessSettings.Key, bytes, RegistryValueKind.Binary);
                        }
                    }
                }
            });
        }

    }

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
