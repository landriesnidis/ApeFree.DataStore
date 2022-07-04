using ApeFree.DataStore.RedisStore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApeFree.DataStore
{
    public static class RedisStoreFactoryExtensions
    {
        /// <summary>
        /// 创建一个注册表(对象)储存器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static RedisStore<T> CreateRedisStore<T>(this StoreFactory _, RedisStoreAccessSettings settings) where T : new()
        {
            return new RedisStore<T>(settings);
        }
    }
}