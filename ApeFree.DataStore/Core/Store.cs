using ApeFree.DataStore.IO;
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
        public abstract void Load();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract void Save();

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

        protected void ReadStreamHandler(Stream stream)
        {
            using (stream = AccessSettings.CompressionAdapter.Decompress(stream))
            {
                using (stream = AccessSettings.EncryptionAdapter.Decode(stream))
                {
                    Value = AccessSettings.SerializationAdapter.Deserialize<TValue>(stream);
                }
            }
        }

        protected void WriteStreamHandler(Action<Stream> saveHandler)
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

        /// <summary>
        /// 获取Store专用的复用线程池
        /// </summary>
        /// <param name="_"></param>
        /// <returns></returns>
        protected ReusableThreadPool GetStoreThreadPool()
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
            if (isMangedModelRunning)
            {
                return;
            }

            isMangedModelRunning = true;

            //判断当前是否有线程在工作
            var reusableThread = GetStoreThreadPool().Get();
            reusableThread.TaskAction = () =>
            {
                while (true)
                {
                    var item = model.Dequeue();
                    if (item == null) break;

                    if (item.EventType == ReadWriteEventType.Write)
                    {
                        Save();
                    }
                    else
                    {
                        Load();
                    }
                    item.Release();
                }
                isMangedModelRunning = false;
            };
            reusableThread.TaskCompleted += ReusableThread_TaskCompleted;
            reusableThread.Start();
        }

        private void ReusableThread_TaskCompleted(ReusableThread sender, CompletedEventArgs e)
        {
            sender.TaskCompleted -= ReusableThread_TaskCompleted;
            GetStoreThreadPool().Release(sender);
        }
    }
}
