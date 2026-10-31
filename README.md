# ApeFree.DataStore

<div align="center">

![ApeFree.DataStore](ApeFree_64px.png)

### 现代化轻量级对象持久化存储库

[![NuGet](https://img.shields.io/nuget/v/ApeFree.DataStore.svg?style=flat-square)](https://www.nuget.org/packages/ApeFree.DataStore/)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0%20%7C%20net10.0-purple.svg?style=flat-square)](#)

</div>

---

## 📖 简介

**ApeFree.DataStore** 是一款面向 .NET 平台的轻量级对象透明持久化与状态管理库。旨在帮助开发者摆脱繁琐的文件读写样板代码，以极简的心智模型实现对内存单体聚合对象在不同介质中的存取与还原。

在 **3.0.0** 重构版本中，底层架构全面升级：
1. **流式装饰器管道（Stream Filter Pipeline）**：彻底破除旧版三段式硬编码，解压、解密、校验任意自由编排，端到端零全量内存拷贝；
2. **纯异步写合并调度器（Coalesce Managed Model）**：非阻塞、无锁原子流转的双缓冲因果状态机，高频并发写入自动折叠为单次物理 IO，且严格维持 Read-Your-Writes 因果一致性；
3. **Jbin 极限性能集成**：深度挂载 Jbin 原生高速反射缓存与基元数组内存直接映射，直写底层流并及时释放内存；
4. **现代化标准开源架构**：支持 `netstandard2.0`、`net8.0` 与 `net10.0` 多目标框架。

---

## 🏗️ 目录结构规范

```text
ApeFree.DataStore/
├── src/                          # 核心源码目录
│   ├── ApeFree.DataStore/        # 核心持久化引擎与管道
│   ├── ApeFree.DataStore.RedisStore/   # Redis 存储扩展
│   └── ApeFree.DataStore.Registry/     # Windows 注册表存储扩展
├── tests/                        # 单元测试工程
│   └── ApeFree.DataStore.Tests/  # MSTest 自动化测试套件
├── samples/                      # 示例工程
│   └── ApeFree.DataStore.Demo/   # WinForms 客户端综合示例
├── docs/                         # 技术架构与开发文档
│   ├── Architecture.md           # 核心架构与 Stream 装饰器管道原理
│   ├── Coalesce_Model.md         # 双缓冲写合并因果状态机深度解析
│   ├── Jbin_Optimization.md      # Jbin 极限性能优化与零拷贝实践
│   └── QuickStart.md             # 快速上手与多介质存储指南
├── README.md                     # 项目主自述文件
└── ApeFree.DataStore.sln         # 解决方案文件
```

---

## 🚀 快速上手

### 1. 基础本地持久化

```csharp
using ApeFree.DataStore;
using ApeFree.DataStore.Local;
using ApeFree.DataStore.Adapters;

// 创建配置并指定序列化器（支持 Jbin / Json / Xml）
var settings = new LocalStoreAccessSettings("./config/app.json")
{
    SerializationAdapter = new JsonSerializationAdapter()
};

var store = StoreFactory.Factory.CreateLocalStore<MySettings>(settings, () => new MySettings
{
    AppName = "DataStoreApp",
    Version = 1
});

// 加载数据
store.Load();
Console.WriteLine(store.Value.AppName);

// 修改并持久化
store.Value.Version = 2;
store.Save();
```

### 2. 自由编排流式过滤器管道

```csharp
using ApeFree.DataStore.Filters;

var settings = new LocalStoreAccessSettings("./config/secure_data.bin");

// 自由添加过滤器：先进行 GZip 压缩，再进行 AES 加密
settings.StreamFilters.Add(new GZipStreamFilter());
settings.StreamFilters.Add(new AesEncryptionStreamFilter(aesKey, aesIV));

var store = StoreFactory.Factory.CreateLocalStore<MyData>(settings);
store.Load();
store.Value.SecretCode = "TopSecret123";
store.Save(); // 磁盘数据经过端到端流式实时压缩和加密
```

### 3. 高并发异步写合并（削峰填谷）

```csharp
// 连续并发触发大量写请求，内部调度器自适应合并为极少数物理 IO，且所有 Task 均能正常完成
var tasks = Enumerable.Range(0, 100).Select(i =>
{
    store.Value.Counter = i;
    return store.SaveAsync();
});
await Task.WhenAll(tasks);
```

---

## 🧩 扩展生态包

| 包名 | 描述 | 适用平台 |
| :--- | :--- | :--- |
| **`ApeFree.DataStore`** | 核心持久化引擎、本地文件存储与管道基座 | `netstandard2.0;net8.0;net10.0` |
| **`ApeFree.DataStore.Registry`** | Windows 注册表对象安全存储扩展 | `netstandard2.0;net8.0-windows;net10.0-windows` |
| **`ApeFree.DataStore.RedisStore`** | Redis 分布式远程对象存储扩展 | `netstandard2.0;net8.0;net10.0` |

---

## 📚 详细文档

- [系统核心架构设计与 Stream 管道 (Architecture.md)](docs/Architecture.md)
- [双缓冲因果写合并状态机深度推导 (Coalesce_Model.md)](docs/Coalesce_Model.md)
- [Jbin 极限性能与零拷贝内存映射 (Jbin_Optimization.md)](docs/Jbin_Optimization.md)
- [快速上手指南 (QuickStart.md)](docs/QuickStart.md)

---

## 📄 开源许可证

本项目基于 [Apache-2.0](LICENSE) 许可证开源。
