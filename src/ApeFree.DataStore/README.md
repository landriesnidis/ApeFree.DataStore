# ApeFree.DataStore

[![NuGet Version](https://img.shields.io/nuget/v/ApeFree.DataStore.svg)](https://www.nuget.org/packages/ApeFree.DataStore/)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/landriesnidis/ApeFree.DataStore/blob/main/LICENSE)

ApeFree.DataStore 是一款现代化轻量级对象持久化存储库。支持在不同平台/介质中对内存对象进行透明存储与还原（如本地文件、注册表、Redis）；内置双缓冲读写因果合并调度器（CoalesceMangedModel）；支持自由编排的流式处理管道（压缩、加密、校验）；深度集成 Jbin 极限性能序列化。

---

## 🌟 核心特性

- **极简心智模型**：以单体聚合对象为中心，提供开箱即用的 `store.Value`、`store.Load()`、`store.Save()` API。
- **流式装饰器管道（Stream Filter Pipeline）**：彻底解除三段式硬编码限制，自由组合 GZip、Deflate、AES 加密等流式过滤器，端到端零全量内存拷贝。
- **自适应写合并（Write Coalescing）**：内置双缓冲因果状态机，将高频并发写入自动折叠为单次物理 IO，且严格保障 Read-Your-Writes 因果一致性。
- **Jbin 极限性能集成**：深度集成 Jbin 二进制混合序列化协议，大数组基元内存直接映射，端到端直接对接 Stream。
- **全平台多目标支持**：全面支持 `netstandard2.0`、`net8.0`、`net10.0`。

---

## 📦 安装方式

```bash
dotnet add package ApeFree.DataStore --version 3.0.0
```

---

## 🚀 快速上手

```csharp
using ApeFree.DataStore;
using ApeFree.DataStore.Local;
using ApeFree.DataStore.Adapters;
using ApeFree.DataStore.Filters;

// 1. 创建本地存储配置
var settings = new LocalStoreAccessSettings("./config/app.bin")
{
    SerializationAdapter = new JbinSerializationAdapter()
};

// 2. 自由追加流式过滤器（先压缩后加密）
settings.StreamFilters.Add(new GZipStreamFilter());
settings.StreamFilters.Add(new AesEncryptionStreamFilter(aesKey, aesIV));

// 3. 构建 Store 实例
var store = StoreFactory.Factory.CreateLocalStore<MyAppSettings>(settings);

// 4. 加载与修改
store.Load();
store.Value.Theme = "Dark";
store.Save();
```

---

## 📄 开源许可证

本项目基于 [Apache-2.0](https://github.com/landriesnidis/ApeFree.DataStore/blob/main/LICENSE) 许可证开源。
