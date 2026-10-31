using System;
using System.IO;
using ApeFree.DataStore.Core;

namespace ApeFree.DataStore.Local
{
    /// <summary>
    /// 本地文件对象存储器
    /// </summary>
    /// <typeparam name="T">存储对象类型</typeparam>
    public class LocalStore<T> : Store<T, LocalStoreAccessSettings> where T : class
    {
        public LocalStore(LocalStoreAccessSettings accessSettings, Func<T> valueFactory = null)
            : base(accessSettings, valueFactory) { }

        /// <inheritdoc/>
        protected override void OnLoad()
        {
            var path = AccessSettings.SavePath;

            if (File.Exists(path))
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    ReadFromStream(fs);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnSave()
        {
            var path = AccessSettings.SavePath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                WriteToStream(fs);
            }
        }
    }
}
