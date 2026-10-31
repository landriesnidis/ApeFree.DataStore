using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ApeFree.DataStore.IO;

namespace ApeFree.DataStore.Core
{
    /// <summary>
    /// 对象存储器抽象基类
    /// </summary>
    /// <typeparam name="TValue">存储的数据对象类型</typeparam>
    /// <typeparam name="TSettings">存取配置类型</typeparam>
    public abstract class Store<TValue, TSettings> : IStore<TValue>
        where TValue : class
        where TSettings : IAccessSettings
    {
        private readonly IReadWriteMangedModel<TValue> model = new CoalesceMangedModel<TValue>();
        private int isRunning = 0;

        /// <summary>
        /// 延迟初始化对象工厂
        /// </summary>
        protected Func<TValue> ValueFactory { get; }

        /// <summary>
        /// 存取配置
        /// </summary>
        public TSettings AccessSettings { get; }

        private TValue value;

        /// <summary>
        /// 存储的对象实体（支持延迟初始化与动态替换）
        /// </summary>
        public TValue Value
        {
            get
            {
                if (value == null && ValueFactory != null)
                {
                    value = ValueFactory.Invoke();
                }
                return value;
            }
            set => this.value = value;
        }

        protected Store(TSettings accessSettings, Func<TValue> valueFactory = null)
        {
            AccessSettings = accessSettings ?? throw new ArgumentNullException(nameof(accessSettings));
            if (valueFactory != null)
            {
                ValueFactory = valueFactory;
            }
            else
            {
                ValueFactory = () => Activator.CreateInstance<TValue>();
            }
        }

        /// <summary>
        /// 执行具体的持久化保存（由子类根据特定介质实现）
        /// </summary>
        protected abstract void OnSave();

        /// <summary>
        /// 执行具体的持久化加载（由子类根据特定介质实现）
        /// </summary>
        protected abstract void OnLoad();

        /// <summary>
        /// 流式管道写入：将对象通过过滤器管道流式写入目标流
        /// </summary>
        /// <param name="destinationStream">物理底层输出流</param>
        protected void WriteToStream(Stream destinationStream)
        {
            if (destinationStream == null) throw new ArgumentNullException(nameof(destinationStream));

            var wrappers = new List<Stream>();
            Stream pipelineStream = destinationStream;
            var filters = AccessSettings.StreamFilters;
            if (filters != null && filters.Count > 0)
            {
                // 逆序装饰：例如 filters = [GZip, Aes]
                // 先包装 Aes(destinationStream)，再包装 GZip(aesStream)
                // 序列化器往 GZip 写 -> GZip 写给 Aes -> Aes 加密写给 destinationStream
                for (int i = filters.Count - 1; i >= 0; i--)
                {
                    pipelineStream = filters[i].WrapWriteStream(pipelineStream);
                    wrappers.Add(pipelineStream);
                }
            }

            try
            {
                // 直接流式序列化
                AccessSettings.SerializationAdapter.Serialize(Value, pipelineStream);
            }
            finally
            {
                // 关键：逆序刷新并处置所有包装流，确保 GZip 写入 trailer、CryptoStream 完成 FlushFinalBlock
                for (int i = wrappers.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        wrappers[i].Flush();
                        wrappers[i].Dispose();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// 流式管道读取：将输入流通过过滤器管道流式解包并反序列化为对象
        /// </summary>
        /// <param name="sourceStream">物理底层输入流</param>
        protected void ReadFromStream(Stream sourceStream)
        {
            if (sourceStream == null) throw new ArgumentNullException(nameof(sourceStream));

            var wrappers = new List<Stream>();
            Stream pipelineStream = sourceStream;
            var filters = AccessSettings.StreamFilters;
            if (filters != null && filters.Count > 0)
            {
                // 对称解包：例如 filters = [GZip, Aes]
                // 先包装 Aes 解密 sourceStream，再包装 GZip 解压 aesStream
                for (int i = filters.Count - 1; i >= 0; i--)
                {
                    pipelineStream = filters[i].WrapReadStream(pipelineStream);
                    wrappers.Add(pipelineStream);
                }
            }

            try
            {
                Value = AccessSettings.SerializationAdapter.Deserialize<TValue>(pipelineStream);
            }
            finally
            {
                for (int i = wrappers.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        wrappers[i].Dispose();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// 兼容老版本的流读取入口
        /// </summary>
        protected void OnReadStream(Stream stream) => ReadFromStream(stream);

        /// <summary>
        /// 兼容老版本的流写入入口
        /// </summary>
        protected void OnWriteStream(Action<Stream> saveHandler)
        {
            using (var ms = new MemoryStream())
            {
                WriteToStream(ms);
                ms.Position = 0;
                saveHandler(ms);
            }
        }

        /// <inheritdoc/>
        public virtual void Load()
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

        /// <inheritdoc/>
        public virtual void Save()
        {
            OnSave();
        }

        /// <inheritdoc/>
        public virtual Task LoadAsync()
        {
            return model.EnqueueAsync(new EventItem<TValue>(ReadWriteEventType.Read), EnsureConsumerRunning);
        }

        /// <inheritdoc/>
        public virtual Task SaveAsync()
        {
            return model.EnqueueAsync(new EventItem<TValue>(ReadWriteEventType.Write) { Value = Value }, EnsureConsumerRunning);
        }

        private void EnsureConsumerRunning()
        {
            if (Interlocked.CompareExchange(ref isRunning, 1, 0) == 0)
            {
                Task.Run(ProcessQueue);
            }
        }

        private void ProcessQueue()
        {
            while (true)
            {
                var item = model.Dequeue();
                if (item == null)
                {
                    Interlocked.Exchange(ref isRunning, 0);

                    // 双重检查防漏
                    item = model.Dequeue();
                    if (item != null)
                    {
                        if (Interlocked.CompareExchange(ref isRunning, 1, 0) != 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                try
                {
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
                catch (Exception ex)
                {
                    item.Fail(ex);
                }
            }
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            AccessSettings?.Dispose();
        }
    }
}
