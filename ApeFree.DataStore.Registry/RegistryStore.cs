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
    public class RegistryStore<T> : Store<T, RegistryStoreAccessSettings> where T : class
    {
        private static readonly RegistryView registryView
            = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;

        /// <summary>
        /// 构造注册表(对象)储存器
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="valueFactory"></param>
        public RegistryStore(RegistryStoreAccessSettings settings, Func<T> valueFactory = null) : base(settings, valueFactory) { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void OnLoad()
        {
            using (var rklm = RegistryKey.OpenBaseKey(AccessSettings.BaseKey, registryView))
            {
                var rk = rklm.OpenSubKey(AccessSettings.Path, true);
                if (rk != null)
                {
                    using (rk)
                    {
                        var bytes = (byte[])rk.GetValue(AccessSettings.Key);
                        OnReadStream(new MemoryStream(bytes));
                    }
                }
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void OnSave()
        {
            OnWriteStream(stream =>
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
}
