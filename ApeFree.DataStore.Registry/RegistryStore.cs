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

        public override void Load()
        {
            using (var rklm = RegistryKey.OpenBaseKey(AccessSettings.BaseKey, registryView))
            {
                var rk = rklm.OpenSubKey(AccessSettings.Path, true);
                if (rk == null)
                {
                    Value = ValueFactory.Invoke();
                    if (Value != null)
                    {
                        Save();
                    }
                }
                else
                {
                    using (rk)
                    {
                        var bytes = (byte[])rk.GetValue(AccessSettings.Key);
                        ReadStreamHandler(new MemoryStream(bytes));
                    }
                }
            }
        }

        public override void Save()
        {
            WriteStreamHandler(stream =>
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
