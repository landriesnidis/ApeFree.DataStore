using System;
using System.IO;
using ApeFree.DataStore.Core;
using Microsoft.Win32;

namespace ApeFree.DataStore.Registry
{
    /// <summary>
    /// Windows 注册表对象存储器
    /// </summary>
    /// <typeparam name="T">存储对象类型</typeparam>
    public class RegistryStore<T> : Store<T, RegistryStoreAccessSettings> where T : class
    {
        private static readonly RegistryView registryView
            = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;

        public RegistryStore(RegistryStoreAccessSettings settings, Func<T> valueFactory = null)
            : base(settings, valueFactory) { }

        /// <inheritdoc/>
        protected override void OnLoad()
        {
            using (var rklm = RegistryKey.OpenBaseKey(AccessSettings.BaseKey, registryView))
            using (var rk = rklm.OpenSubKey(AccessSettings.Path, false))
            {
                if (rk != null)
                {
                    if (rk.GetValue(AccessSettings.Key) is byte[] bytes && bytes.Length > 0)
                    {
                        using (var ms = new MemoryStream(bytes, false))
                        {
                            ReadFromStream(ms);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnSave()
        {
            using (var ms = new MemoryStream())
            {
                WriteToStream(ms);
                var bytes = ms.ToArray();

                using (var rklm = RegistryKey.OpenBaseKey(AccessSettings.BaseKey, registryView))
                {
                    using (var rk = rklm.CreateSubKey(AccessSettings.Path))
                    {
                        if (rk != null)
                        {
                            rk.SetValue(AccessSettings.Key, bytes, RegistryValueKind.Binary);
                        }
                    }
                }
            }
        }
    }
}
