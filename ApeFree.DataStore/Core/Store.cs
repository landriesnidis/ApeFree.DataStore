using System;
using System.ComponentModel;
using System.IO;

namespace ApeFree.DataStore.Core
{
    public abstract class Store<TValue, TSettings> : IStore<TValue>
        where TValue : new()
        where TSettings : IAccessSettings
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public TSettings AccessSettings { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public TValue Value { get; protected set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract void Load();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract void Save();

        protected Store(TSettings accessSettings)
        {
            AccessSettings = accessSettings;
        }

        protected void LoadHandler(Stream stream)
        {
            using (stream = AccessSettings.CompressionAdapter.Decompress(stream))
            {
                using (stream = AccessSettings.EncryptionAdapter.Decode(stream))
                {
                    Value = AccessSettings.SerializationAdapter.Deserialize<TValue>(stream);
                }
            }
        }

        protected void SaveHandler(Action<Stream> saveHandler)
        {
            Stream stream;
            using (stream = AccessSettings.SerializationAdapter.Serialize(Value))
            {
                using (stream = AccessSettings.EncryptionAdapter.Encode(stream))
                {
                    using (stream = AccessSettings.CompressionAdapter.Compress(stream))
                    {
                        saveHandler.Invoke(stream);
                    }
                }
            }
        }

        public void Dispose()
        {
            AccessSettings.Dispose();
        }
    }
}
