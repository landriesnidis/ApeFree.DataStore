using System;
using System.IO;
using ApeFree.DataStore.Core;

namespace ApeFree.DataStore.RedisStore
{
    /// <summary>
    /// Redis 对象存储器
    /// </summary>
    /// <typeparam name="T">存储对象类型</typeparam>
    public class RedisStore<T> : Store<T, RedisStoreAccessSettings> where T : class
    {
        public RedisStore(RedisStoreAccessSettings settings, Func<T> valueFactory = null)
            : base(settings, valueFactory) { }

        /// <inheritdoc/>
        protected override void OnLoad()
        {
            byte[] bytes = AccessSettings.Client.Get(AccessSettings.Key);
            if (bytes != null && bytes.Length > 0)
            {
                using (var ms = new MemoryStream(bytes, false))
                {
                    ReadFromStream(ms);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnSave()
        {
            using (var ms = new MemoryStream())
            {
                WriteToStream(ms);
                AccessSettings.Client.Set(AccessSettings.Key, ms.ToArray());
            }
        }
    }
}
