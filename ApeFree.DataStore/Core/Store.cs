﻿using ApeFree.DataStore.IO;
using STTech.CodePlus.Threading;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace ApeFree.DataStore.Core
{
    public abstract partial class Store<TValue, TSettings> : IStore<TValue>
        where TValue : class
        where TSettings : IAccessSettings
    {
        /// <summary>
        /// 在需要时为生成延迟初始化值而调用的委托
        /// </summary>
        protected Func<TValue> ValueFactory { get; }

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
        protected abstract void OnLoad();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected abstract void OnSave();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Load()
        {
            OnLoad();

            if (Value == null)
            {
                Value = ValueFactory.Invoke();
                if (Value != null)
                {
                    Save();
                }
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Save()
        {
            OnSave();
        }

        protected Store(TSettings accessSettings, Func<TValue> valueFactory = null)
        {
            AccessSettings = accessSettings;
            if (valueFactory != null)
            {
                ValueFactory = valueFactory;
            }
            else
            {
                ValueFactory = () => Activator.CreateInstance<TValue>();
            }
        }

        protected void OnReadStream(Stream stream)
        {
            using (stream = AccessSettings.CompressionAdapter.Decompress(stream))
            {
                using (stream = AccessSettings.EncryptionAdapter.Decode(stream))
                {
                    Value = AccessSettings.SerializationAdapter.Deserialize<TValue>(stream);
                }
            }
        }

        protected void OnWriteStream(Action<Stream> saveHandler)
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

        /// <inheritdoc/>
        public void Dispose()
        {
            AccessSettings.Dispose();
        }

        /// <summary>
        /// 获取Store专用的复用线程池
        /// </summary>
        /// <param name="_"></param>
        /// <returns></returns>
        protected ThreadPool GetStoreThreadPool()
        {
            return ReusableThreadPoolManager.Instance.Pool;
        }
    }

    public abstract partial class Store<TValue, TSettings>
    {
        private readonly IReadWriteMangedModel<TValue> model = new CoalesceMangedModel<TValue>();

        private bool isMangedModelRunning = false;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Task LoadAsync()
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                model.Enqueue(new EventItem<TValue>(ReadWriteEventType.Read), ConcurrentMangedModelHandler);
            });
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Task SaveAsync()
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                model.Enqueue(new EventItem<TValue>(ReadWriteEventType.Write) { Value = Value }, ConcurrentMangedModelHandler);
            });
        }

        private void ConcurrentMangedModelHandler()
        {
            //判断当前是否有线程在工作
            if (isMangedModelRunning)
            {
                return;
            }

            isMangedModelRunning = true;

            GetStoreThreadPool().Run(new TaskDelegation(() => {
                while (true)
                {
                    var item = model.Dequeue();
                    if (item == null) break;

                    if (item.EventType == ReadWriteEventType.Write)
                    {
                        OnSave();
                    }
                    else
                    {
                        OnLoad();
                    }
                    item.Release();
                }
                isMangedModelRunning = false;
            }));
        }
    }
}
