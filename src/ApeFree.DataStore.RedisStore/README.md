# ApeFree.DataStore.RedisStore

<div align="center">

![ApeFree.DataStore](../../ApeFree_64px.png)

### 基于 ApeFree.DataStore 的 Redis 分布式对象持久化存储扩展

[![NuGet Version](https://img.shields.io/nuget/v/ApeFree.DataStore.RedisStore.svg?style=flat-square&color=blue)](https://www.nuget.org/packages/ApeFree.DataStore.RedisStore/)
[![License](https://img.shields.io/badge/license-Apache--2.0-green.svg?style=flat-square)](https://github.com/landriesnidis/ApeFree.DataStore/blob/main/LICENSE)
[![.NET Compatibility](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0%20%7C%20net10.0-purple.svg?style=flat-square)](#)

</div>

---

## 📖 简介

`ApeFree.DataStore.RedisStore` 是 `ApeFree.DataStore` 面向分布式与网络存储场景的官方扩展包。它允许将内存中的复杂聚合对象直接持久化到 Redis 集中式键值数据库中，支持多进程、多微服务节点之间的数据状态共享。

---

## 🌟 核心特性

- **介质无缝解耦**：业务代码完全面向 `IStore<T>` 编程，与本地文件存储 API 完全统一。
- **透明享受管道能力**：存入 Redis 的数据同样支持 **GZip / Deflate 压缩**（大幅降低 Redis 内存占用与网络带宽）与 **AES 对称加密**（保障敏感配置在共享缓存中的安全）。
- **Jbin 极速二进制适配**：支持使用 Jbin 格式存入 Redis，点云/大数组直接以二进制外置块存储，避免 Base64 膨胀。
- **自适应写合并**：继承 `CoalesceMangedModel` 的写折叠能力，高频更新对象状态时自动削峰，避免 Redis 被高频写请求打崩。

---

## 📦 安装方式

```bash
dotnet add package ApeFree.DataStore.RedisStore
```

---

## 🚀 快速上手

### 1. 基础用法

```csharp
using ApeFree.DataStore;
using ApeFree.DataStore.RedisStore;
using ServiceStack.Redis;

// 1. 初始化 Redis 客户端连接
var client = new RedisClient("127.0.0.1", 6379);

// 2. 创建 Redis 存取配置并指定目标 Key
var settings = new RedisStoreAccessSettings(client, "App:Cluster:Config");

// 3. 构建 RedisStore 实例
var store = StoreFactory.Factory.CreateRedisStore<ClusterConfig>(settings, () => new ClusterConfig
{
    ClusterName = "AlphaNode",
    Port = 9001
});

// 4. 加载数据（若 Redis 中不存在则自动保存默认值）
store.Load();
Console.WriteLine($"集群名称: {store.Value.ClusterName}");

// 5. 修改并保存回 Redis
store.Value.Port = 9002;
store.Save();
```

### 2. 结合压缩与加密存入 Redis

```csharp
using ApeFree.DataStore.Filters;
using System.IO.Compression;

// 自动对存入 Redis 的数据进行 GZip 压缩与 AES 加密
settings.StreamFilters.Add(new GZipStreamFilter(CompressionLevel.Optimal));
settings.StreamFilters.Add(new AesEncryptionStreamFilter(aesKey, aesIV));

store.Save(); // Redis 中存储的将是紧凑的加密密文
```

---

## 💾 配置选项字典

| 配置类 | 属性名称 | 类型 | 说明 |
| :--- | :--- | :--- | :--- |
| **`RedisStoreAccessSettings`** | `Client` | `IRedisClient` | Redis 客户端连接实例（构造函数必填） |
| | `Key` | `string` | 存储的目标 Redis 键名（构造函数必填） |
| | `SerializationAdapter` | `ISerializationAdapter` | 序列化适配器（支持 Jbin / Json / Xml，默认 Jbin） |
| | `StreamFilters` | `IList<IStreamFilter>` | 流过滤器管道列表（支持 GZip、Deflate、AES 等） |

---

## 📄 开源许可证

本项目基于 [Apache-2.0](https://github.com/landriesnidis/ApeFree.DataStore/blob/main/LICENSE) 许可证开源。
