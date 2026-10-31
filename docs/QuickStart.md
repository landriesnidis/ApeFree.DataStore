# ApeFree.DataStore 快速上手指南

## 1. 基础使用：本地文件存储

```csharp
using ApeFree.DataStore;
using ApeFree.DataStore.Local;
using ApeFree.DataStore.Adapters;

// 1. 创建本地存储器配置（支持自定义序列化格式）
var settings = new LocalStoreAccessSettings("./config/appsettings.json")
{
    SerializationAdapter = new JsonSerializationAdapter()
};

// 2. 创建或获取 Store 实例
var store = StoreFactory.Factory.CreateLocalStore<MyConfig>(settings, () => new MyConfig
{
    AppName = "ApeFreeApp",
    Port = 8080
});

// 3. 加载与读取数据
store.Load();
Console.WriteLine($"当前服务端口: {store.Value.Port}");

// 4. 修改并保存数据
store.Value.Port = 9000;
store.Save();
```

---

## 2. 流式过滤器管道：启用压缩与加密

```csharp
using ApeFree.DataStore.Filters;

var settings = new LocalStoreAccessSettings("./config/secure_data.bin")
{
    SerializationAdapter = new JbinSerializationAdapter() // 默认即为 Jbin 高性能序列化
};

// 自由添加流式过滤器（管道由先到后生效）
settings.StreamFilters.Add(new GZipStreamFilter());
settings.StreamFilters.Add(new AesEncryptionStreamFilter(aesKey, aesIV));

var store = StoreFactory.Factory.CreateLocalStore<MySecretData>(settings);
store.Load();
store.Value.Token = "secret-token-123";
store.Save(); // 写入磁盘的文件将自动被先压缩、后加密！
```

---

## 3. 高并发异步存取（自动享受写合并）

```csharp
// 并发调用 SaveAsync 无需担心写风暴，内部双缓冲调度器会自动折叠写入
await Task.WhenAll(
    store.SaveAsync(),
    store.SaveAsync(),
    store.SaveAsync()
);
```

---

## 4. 存储介质扩展

- **Windows 注册表存储**：安装 `ApeFree.DataStore.Registry` 包，使用 `CreateRegistryStore`；
- **Redis 远程缓存存储**：安装 `ApeFree.DataStore.RedisStore` 包，使用 `CreateRedisStore`。
