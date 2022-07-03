using System;

namespace ApeFree.DataStore
{
    /// <summary>
    /// 对象存储器构造工厂
    /// </summary>
    public sealed class StoreFactory
    {
        private static readonly Lazy<StoreFactory> lazy = new Lazy<StoreFactory>(() => new StoreFactory());
        public static StoreFactory Factory
        {
            get
            {
                return lazy.Value;
            }
        }
        private StoreFactory() { }
    }
}
