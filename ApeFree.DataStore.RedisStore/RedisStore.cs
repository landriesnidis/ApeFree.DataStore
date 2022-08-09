using ApeFree.DataStore.Core;
using System;
using System.IO;

namespace ApeFree.DataStore.RedisStore
{
    /// <summary>
    /// Redis存储器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RedisStore<T> : Store<T, RedisStoreAccessSettings> where T : class
    {
        /// <summary>
        /// 构造Redis存储器
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="valueFactory"></param>
        public RedisStore(RedisStoreAccessSettings settings, Func<T> valueFactory = null) : base(settings, valueFactory)
        { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void OnLoad()
        {
            byte[] bytes = AccessSettings.Client.Get(AccessSettings.Key);
            if (bytes != null)
            {
                OnReadStream(new MemoryStream(bytes));
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void OnSave()
        {
            OnWriteStream(stream =>
            {
                MemoryStream memoryStream;
                if (stream is MemoryStream)
                {
                    memoryStream = stream as MemoryStream;
                }
                else
                {
                    memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                }

                var bytes = memoryStream.ToArray();
                AccessSettings.Client.Set(AccessSettings.Key, bytes);
            });
        }
    }
}