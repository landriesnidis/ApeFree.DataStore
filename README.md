# ApeFree.DataStore

<div align="center">

![ApeFree.DataStore](ApeFree_64px.png)

### 现代化轻量级对象透明持久化与状态管理引擎

[![NuGet Version](https://img.shields.io/nuget/v/ApeFree.DataStore.svg?style=flat-square&color=blue)](https://www.nuget.org/packages/ApeFree.DataStore/)
[![License](https://img.shields.io/badge/license-Apache--2.0-green.svg?style=flat-square)](LICENSE)
[![.NET Compatibility](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0%20%7C%20net10.0-purple.svg?style=flat-square)](#)

</div>

---

## 📖 简介

在传统桌面端（WinForms/WPF/Avalonia）、工控软件、小型服务或工具类应用开发中，持久化配置参数、用户偏好或业务单体聚合模型往往非常繁琐：开发者不得不反复编写 `File.ReadAllText`、加解密中转、序列化反序列化等样板代码；在高频参数调节或并发写入时，还极易引发**磁盘写风暴**与**文件锁冲突**。

**ApeFree.DataStore** 专为解决这一系列痛点而生！它将内存单体对象与底层存储介质（本地磁盘、Windows 注册表、Redis 缓存）完全解耦，提供**极简心智模型（`store.Value` / `store.Load()` / `store.Save()`）**，并在底层内嵌了**零拷贝双向流式过滤器管道**与**双缓冲读写因果合并调度器**。

---

## 🤔 为什么选择 ApeFree.DataStore？

| 痛点场景 | 传统手写方案 / 简易库 | ApeFree.DataStore 3.0.0 解决方案 |
| :--- | :--- | :--- |
| **持久化样板代码** | 随处充斥 `File.WriteAllText(JsonConvert.Serialize(...))`，维护成本高 | 统一抽象为 `IStore<T>` 门面，通过 `store.Value` 直观操作，一键读写 |
| **存储介质切换** | 更换存储目标（如文件改存注册表或 Redis）需重构整个 IO 逻辑 | **介质完全透明**：业务代码仅依赖 `IStore<T>`，换介质仅需替换配置对象 |
| **高频并发写风暴** | 滑块拖拽、高频传感器采样导致磁盘或网络 IO 被打崩、文件锁死 | **内置自适应写折叠（Write Coalescing）**：高频并发写自动合并为单次最终落盘 |
| **时序错乱与脏读** | 并发异步写入未完成时发起读取，容易读到旧数据或损坏的文件 | **读写因果一致性保证（Read-Your-Writes）**：写后的读必定等待并读取写后新值 |
| **压缩与加密扩展** | 算法逻辑与文件读写强耦合，内存中反复多次生成 `byte[]` 和全量 `MemoryStream` | **双向装饰流管道（Decorator Pipeline）**：端到端零全量内存拷贝，插件自由插拔 |
| **工业大数组开销** | JSON 序列化点云/大数组时产生严重的 Base64 膨胀与 GC 暂停 | **深度集成 Jbin 极限性能引擎**：基元数组通过非托管指针零拷贝映射，吞吐提升数倍 |

---

## 🏛️ 核心架构全景

```mermaid
flowchart TD
    subgraph 业务层 [Application Layer]
        App["业务调用方 (store.Value / LoadAsync / SaveAsync)"]
    end

    subgraph 调度层 [Coalesce Scheduling Layer]
        Model["CoalesceMangedModel (纯异步双缓冲因果状态机)"]
        Current["CurrentTaskGroup (当前批次 - 自动折叠多次写入)"]
        Standby["StandbyTaskGroup (备用批次 - 隔离因果读取时序)"]
        Model --> Current & Standby
    end

    subgraph 流式管道层 [Stream Pipeline Layer]
        P_Write["WrapWriteStream 管道 (自底向上装饰 - Push 流式直写)"]
        P_Read["WrapReadStream 管道 (对称解包 - Pull 流式直读)"]
        Filters["IStreamFilter 插件列表<br/>[ GZipFilter | DeflateFilter | AesEncryptionFilter | ... ]"]
        Filters -.-> P_Write & P_Read
    end

    subgraph 转换层 [Serialization Layer]
        Serializer["ISerializationAdapter (Jbin / Json / Xml)"]
    end

    subgraph 物理介质层 [Physical Storage Medium]
        Local["LocalStore (本地 FileStream)"]
        Registry["RegistryStore (Windows 注册表)"]
        Redis["RedisStore (Redis 缓存服务)"]
    end

    App --> Model
    Model --> Local & Registry & Redis
    Local & Registry & Redis --> P_Write & P_Read
    P_Write & P_Read --> Serializer
```

---

## 🔄 格式随心换：全套序列化适配器（ISerializationAdapter）

DataStore 3.0.0 提供了全面的序列化格式支持，用户可以通过 `settings.SerializationAdapter` 一键切换，并支持深度自定义配置。

### 1. Jbin 序列化器（`JbinSerializationAdapter`，默认推荐 ⭐）
**Jbin** 是 ApeFree 生态专属的高性能 JSON 二进制混合序列化协议。它将对象的拓扑骨架以 JSON 表达，而将大型浮点/整型数组、矩阵或结构体外置为连续二进制块，实现直接内存映射。
- **端到端纯流式**：直接向底层流写入，零临时 byte[] 堆分配；
- **内存即时释放**：反序列化后自动 Dispose 释放数据块；
- **多态白名单安全防护**：支持指定受信任的程序集白名单。

```csharp
using ApeFree.DataStore.Adapters;

// 1. 使用 Jbin 默认高性能配置（开箱即用）
settings.SerializationAdapter = new JbinSerializationAdapter();

// 2. 带有多态反序列化安全白名单的配置
settings.SerializationAdapter = new JbinSerializationAdapter(
    typeof(MyModel).Assembly,
    typeof(MyPluginContract).Assembly
);
```

### 2. JSON 序列化器（`JsonSerializationAdapter`）
基于成熟的 `Newtonsoft.Json` 构建，采用纯流式 `JsonTextWriter` 与 `JsonTextReader` 对接，避免内存中生成巨大字符串。
- 支持完整的 `JsonSerializerSettings` 配置（缩进排版、忽略 Null、时间格式化、枚举转换等）。

```csharp
using ApeFree.DataStore.Adapters;
using Newtonsoft.Json;

// 自定义 JSON 配置
var jsonSettings = new JsonSerializerSettings
{
    Formatting = Formatting.Indented,                 // 易读缩进格式
    NullValueHandling = NullValueHandling.Ignore,     // 忽略 null 字段
    DateFormatString = "yyyy-MM-dd HH:mm:ss"          // 自定义日期格式
};

settings.SerializationAdapter = new JsonSerializationAdapter(jsonSettings);
```

### 3. XML 序列化器（`XmlSerializationAdapter`）
基于 .NET 原生 `XmlSerializer` 构建，内置类型序列化器的高并发安全缓存（`ConcurrentDictionary`）。
- 支持自定义根节点名称（`RootName`），满足特定工控协议或旧系统 XML 规范。

```csharp
using ApeFree.DataStore.Adapters;

// 1. 标准 XML 序列化
settings.SerializationAdapter = new XmlSerializationAdapter();

// 2. 指定自定义 XML 根节点名称（如生成 <SystemConfiguration>...</SystemConfiguration>）
settings.SerializationAdapter = new XmlSerializationAdapter("SystemConfiguration");
```

---

## 🛡️ 安全与瘦身：全套流过滤器（IStreamFilter）自由组合

DataStore 3.0.0 彻底废除了旧版死板的三段式结构，将流转换全面抽象为 `IStreamFilter` 管道。**过滤器在 `settings.StreamFilters` 列表中由先到后生效**，写入时顺流压缩/加密，读取时对称解密/解压。

### 1. GZip 压缩过滤器（`GZipStreamFilter`）
采用业界标准的 GZip 算法，支持配置压缩等级：
```csharp
using ApeFree.DataStore.Filters;
using System.IO.Compression;

// 快速压缩（适合高频小数据）
settings.StreamFilters.Add(new GZipStreamFilter(CompressionLevel.Fastest));

// 极限压缩（适合落盘存档或低频大数据）
settings.StreamFilters.Add(new GZipStreamFilter(CompressionLevel.Optimal));
```

### 2. Deflate 压缩过滤器（`DeflateStreamFilter`）
采用 Deflate 无损压缩算法，相较于 GZip 略去文件头尾元数据，体积更精简：
```csharp
using ApeFree.DataStore.Filters;
using System.IO.Compression;

settings.StreamFilters.Add(new DeflateStreamFilter(CompressionLevel.Optimal));
```

### 3. AES 对称加密过滤器（`AesEncryptionStreamFilter`）
对存储数据进行端到端高强度对称加密，磁盘或注册表上绝无明文泄露：
- 支持指定自定义 **密钥（Key）** 与 **初始化向量（IV）**；
- 支持自定义加密模式（`CipherMode`，如 CBC/ECB/CFB）与填充模式（`PaddingMode`，如 PKCS7/None/Zeros）。

```csharp
using ApeFree.DataStore.Filters;
using System.Security.Cryptography;
using System.Text;

byte[] key = Encoding.UTF8.GetBytes("01234567890123456789012345678901"); // 32 字节 (AES-256)
byte[] iv  = Encoding.UTF8.GetBytes("0123456789abcdef");                 // 16 字节

var aesFilter = new AesEncryptionStreamFilter(key, iv)
{
    CipherMode = CipherMode.CBC,
    PaddingMode = PaddingMode.PKCS7
};

settings.StreamFilters.Add(aesFilter);
```

### 4. 复合多层管道实战（按需自由组合）

流式过滤器的执行顺序严格取决于你在列表中的添加顺序：

#### 方案 A：先压缩、后加密（推荐最佳实践）
先通过压缩减少数据体积并消除明文统计特征（增加信息熵），再进行加密，既大幅提升加密速度，又显著增强抗密文分析能力：
```csharp
// 管道定义：[0] GZip -> [1] AES
settings.StreamFilters.Add(new GZipStreamFilter(CompressionLevel.Optimal));
settings.StreamFilters.Add(new AesEncryptionStreamFilter(key, iv));
```

#### 方案 B：仅加密不压缩 / 仅压缩不加密
只需向列表中添加对应的单一过滤器，不再强制要求配置冗余的默认占位项。

### 5. 自定义扩展专属 StreamFilter

只需实现 `IStreamFilter` 接口，即可无缝融入 DataStore 管道：
```csharp
public class MyAuditFilter : IStreamFilter
{
    public Stream WrapWriteStream(Stream outputStream)
    {
        Console.WriteLine($"[Audit] 开始写入数据，时间: {DateTime.Now}");
        return outputStream; // 可返回自定义计数流或日志流
    }

    public Stream WrapReadStream(Stream inputStream)
    {
        Console.WriteLine($"[Audit] 开始读取数据，时间: {DateTime.Now}");
        return inputStream;
    }

    public void Dispose() { }
}
```

---

## 💾 介质任你选：存储介质与完整配置字典

### 1. 本地文件存储（`LocalStore<T>`）
最常用的存储介质。自动处理多级目录创建，直接对接物理 `FileStream`。

```csharp
using ApeFree.DataStore;
using ApeFree.DataStore.Local;

var settings = new LocalStoreAccessSettings("./data/config.dat");
var store = StoreFactory.Factory.CreateLocalStore<MyConfig>(settings);
```

### 2. Windows 注册表存储（`RegistryStore<T>`）
*需要引用 `ApeFree.DataStore.Registry` 包*。专用于 Windows 平台下的系统注册表键值持久化，结合 AES 加密可用于商业软件授权、机器指纹或防篡改配置。

```csharp
using ApeFree.DataStore;
using ApeFree.DataStore.Registry;
using Microsoft.Win32;

var settings = new RegistryStoreAccessSettings(
    RegistryHive.CurrentUser,           // 注册表根节点 (CurrentUser / LocalMachine)
    @"Software\MyCompany\MyApp\Config", // 子项路径
    "CoreSettings"                      // 值名称 (Value Key)
);

var store = StoreFactory.Factory.CreateRegistryStore<LicenseConfig>(settings);
```

### 3. Redis 远程缓存存储（`RedisStore<T>`）
*需要引用 `ApeFree.DataStore.RedisStore` 包*。将单体业务模型持久化到集中式 Redis 缓存服务器，多客户端可共享加载与保存。

```csharp
using ApeFree.DataStore;
using ApeFree.DataStore.RedisStore;
using ServiceStack.Redis;

var client = new RedisClient("127.0.0.1", 6379);
var settings = new RedisStoreAccessSettings(client, "App:Config:Production");
var store = StoreFactory.Factory.CreateRedisStore<ClusterConfig>(settings);
```

### 4. 核心配置选项字典

| 配置类 | 属性名称 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- | :--- |
| **`AccessSettings`** | `SerializationAdapter` | `ISerializationAdapter` | `JbinSerializationAdapter` | 序列化适配器（支持 Jbin / Json / Xml） |
| | `StreamFilters` | `IList<IStreamFilter>` | 空列表 | 流过滤器管道列表，按声明顺序依次包装 |
| **`LocalStoreAccessSettings`** | `SavePath` | `string` | *(必填构造参数)* | 本地持久化文件的物理/相对路径 |
| **`RegistryStoreAccessSettings`** | `BaseKey` | `RegistryHive` | *(必填构造参数)* | 注册表根键（`CurrentUser` 或 `LocalMachine`） |
| | `Path` | `string` | *(必填构造参数)* | 注册表子键路径 |
| | `Key` | `string` | *(必填构造参数)* | 存储的数据项名称（注册表值名） |
| **`RedisStoreAccessSettings`** | `Client` | `IRedisClient` | *(必填构造参数)* | Redis 客户端连接实例 |
| | `Key` | `string` | *(必填构造参数)* | Redis 存储目标键名 |

---

## ⚡ 高并发异步写合并（Coalesce Managed Model）

在工业测控、UI 状态实时跟踪等高频写入场景下，短时间内频繁调用保存会造成灾难性的 IO 竞争。

DataStore 内部内置了**双缓冲读写因果合并调度器**：
- **自动写折叠（Write Coalescing）**：当后台正在执行写盘时，前端连续到来的 100 次 `SaveAsync()` 会被自动合并为**最后 1 次物理写**，但前面等待的所有 100 个 `Task` 均能收到写入完成的成功通知；
- **纯异步零阻塞**：完全基于 `TaskCompletionSource<bool>` 实现非阻塞通知，零消耗后台线程池线程；
- **Read-Your-Writes 因果一致性**：写操作后紧接着发起的读操作，绝不会被抢跑在旧写之前执行，100% 确保读取到写盘后的最新值。

```csharp
// 连续触发 100 次并发写任务，无需担心性能崩溃，内部自动折叠为极少数物理 IO
var tasks = Enumerable.Range(0, 100).Select(i =>
{
    store.Value.Progress = i;
    return store.SaveAsync();
});

await Task.WhenAll(tasks);
```

---

## 📦 安装与包生态

| Nuget 包名 | 说明 | 兼容平台 |
| :--- | :--- | :--- |
| [**`ApeFree.DataStore`**](https://www.nuget.org/packages/ApeFree.DataStore/) | 核心引擎、本地文件存储、流管道、Jbin/Json/Xml 适配器 | `netstandard2.0` / `net8.0` / `net10.0` |
| [**`ApeFree.DataStore.Registry`**](https://www.nuget.org/packages/ApeFree.DataStore.Registry/) | Windows 注册表对象安全存储扩展 | `netstandard2.0` / `net8.0-windows` / `net10.0-windows` |
| [**`ApeFree.DataStore.RedisStore`**](https://www.nuget.org/packages/ApeFree.DataStore.RedisStore/) | Redis 分布式远程对象存储扩展 | `netstandard2.0` / `net8.0` / `net10.0` |

---

## 🖥️ 演示工程与性能评测 (Samples & Benchmark)

本项目在 `samples/` 目录下提供了现代化的基准评测与交互式演示工程：

- [**`ApeFree.DataStore.BenchmarkDemo`**](samples/ApeFree.DataStore.BenchmarkDemo/)：**专业级性能基准与大数据存储评测系统（WinForms）**
  - **界面 1：⚡ 高频配置读写对比**：直观对比高频配置修改场景下，`DataStore`（内置 `CoalesceMangedModel` 因果写合并调度器）相较于传统直接文件读写（`File.WriteAllText + JsonConvert`）的性能差异。提供写入耗时（降低 90%+）、物理刷盘次数（削减 98%+）、吞吐率（提升 6~50 倍）及并发锁冲突的详细对比。
  - **界面 2：🚀 超大数据吞吐与压缩比对比**：
    - **海量传感器与 3D 点云时序（连续基元值类型大数组）**：全方位横向对比原生 JSON、原生 XML、DataStore (Json + GZip)、DataStore (Jbin 零拷贝) 以及 DataStore (Jbin + GZip 极限压缩)。深度展示 Jbin 原生基元内存映射与二进制流压缩的强悍性能。
    - **超大业务实体对象集合（对象密集型结构）**：展示 Jbin 的**列式存储（Columnar Storage - `TransposeToDictionary`）**能力，将海量实体对象按列聚合成物理连续数组，彻底消除属性名冗余，配合流压缩管道实现体积暴降 90%~95% 以上并极速还原。
- [**`ApeFree.DataStore.Demo`**](samples/ApeFree.DataStore.Demo/)：基础 WinForms 数据实体增删改查与持久化示例。

---

## 📚 延伸技术文档

- [🏛️ 核心架构与 Stream 装饰器管道原理 (Architecture.md)](docs/Architecture.md)
- [⚡ 双缓冲因果写合并状态机数学推导 (Coalesce_Model.md)](docs/Coalesce_Model.md)
- [🚀 Jbin 极限性能优化与零拷贝实践指南 (Jbin_Optimization.md)](docs/Jbin_Optimization.md)
- [📖 快速上手与多介质代码速查 (QuickStart.md)](docs/QuickStart.md)

---

## 📄 开源许可证

本项目基于 [Apache-2.0](LICENSE) 许可证开源。欢迎提交 Issue 与 Pull Request！
