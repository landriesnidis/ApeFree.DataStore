using ApeFree.DataStore.Core;
using ServiceStack.Redis;
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
        public RedisStore(RedisStoreAccessSettings settings) : base(settings)
        { }

        public override void Load()
        {
            byte[] bytes = AccessSettings.Client.Get(AccessSettings.Key);
            if (bytes == null)
            {
                Value = Activator.CreateInstance<T>();
                Save();
            }
            else
            {
                ReadStreamHandler(new MemoryStream(bytes));
            }
        }

        public override void Save()
        {
            WriteStreamHandler(stream =>
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

    public class RedisStoreAccessSettings : AccessSettings
    {
        public RedisClient Client { get; set; }
        public string Key { get; set; }

        public RedisStoreAccessSettings(RedisClient client, string key)
        {
            Client = client;
            Key = key;
        }
    }
}