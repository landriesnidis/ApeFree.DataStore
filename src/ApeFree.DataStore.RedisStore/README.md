# ApeFree.DataStore.RedisStore

[![NuGet Version](https://img.shields.io/nuget/v/ApeFree.DataStore.RedisStore.svg)](https://www.nuget.org/packages/ApeFree.DataStore.RedisStore/)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/landriesnidis/ApeFree.DataStore/blob/main/LICENSE)

基于 `ApeFree.DataStore` 提供 Redis 远程对象持久化存储器的扩展实现包。

---

## 🌟 核心特性

- **介质无缝切换**：将业务模型以统一的 `IStore<T>` 方式持久化到 Redis 集中式缓存中。
- **透明享受管道能力**：存入 Redis 的数据同样支持 GZip 压缩、AES 加密与 Jbin 极速二进制序列化。
- **写合并支持**：高频更新状态时自动合并写入，减轻 Redis 网络 IO 负载。

---

## 📦 安装方式

```bash
dotnet add package ApeFree.DataStore.RedisStore --version 3.0.0
```

---

## 🚀 快速上手

```csharp
using ApeFree.DataStore;
using ApeFree.DataStore.RedisStore;
using ServiceStack.Redis;

// 1. 创建 Redis 存储配置
var client = new RedisClient("127.0.0.1", 6379);
var settings = new RedisStoreAccessSettings(client, "App:Config:Cluster");

// 2. 创建 RedisStore 实例
var store = StoreFactory.Factory.CreateRedisStore<ClusterConfig>(settings);
store.Load();
store.Value.NodeCount = 5;
store.Save();
```

---

## 📄 开源许可证

本项目基于 [Apache-2.0](https://github.com/landriesnidis/ApeFree.DataStore/blob/main/LICENSE) 许可证开源。
