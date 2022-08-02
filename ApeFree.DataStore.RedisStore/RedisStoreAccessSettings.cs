using ApeFree.DataStore.Core;
using ServiceStack.Redis;

namespace ApeFree.DataStore.RedisStore
{
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