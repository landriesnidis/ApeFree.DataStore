# Jbin 极限性能优化与零拷贝实践

## 1. Jbin 协议概述

Jbin（JSON Binary Hybrid Serialization Protocol）是 ApeFree 生态旗下的高性能混合序列化协议，核心原理为：**“JSON 拓扑骨架 + 64位魔数 CombinedId 指针 + 外置二进制块物理帧”**。在面对包含大基元数组（如点云、波形、工业数据矩阵）的模型时，能够绕过 Base64 编码膨胀并实现零拷贝内存直接映射。

---

## 2. DataStore 3.0.0 的极限优化落地

在 3.0.0 版本中，`JbinSerializationAdapter` 实现了四位一体的性能跃升：

### 2.1 端到端纯流式直写
- 序列化直接调用 `jbin.WriteTo(stream)`，消除旧版本调用 `ToBytes()` 产生的全量连续大字节数组分配；
- 反序列化采用 `JbinObject.Parse(stream)` 流式解析物理帧，无多余二次内存加载。

### 2.2 深度挂载 Jbin 原生高性能转换器
自动复用 `JbinObject.JsonSerializerSettings`，启用：
- `CachedContractResolver` 高速反射元数据缓存；
- `JbinPrimitiveArrayConverter`：通过非托管指针与 `Marshal.Copy` 实现基元数组（`float[]`, `int[]`, `double[]`）在内存与流之间的零拷贝映射；
- `JbinGenericStructConverter`：支持复杂结构体的高性能二进制块抽取。

### 2.3 强制生命周期释放（Dispose）
`JbinObject` 实现了 `IDisposable`。在完成序列化与反序列化后，通过 `using` 强制调用 `Dispose()`，及时清空 `DataBlocks` 引用的内部大内存块，显著减轻 GC 压力。

### 2.4 安全多态白名单支持
支持可选传入 `AssemblyWhitelistSerializationBinder`，防御不可信多态反序列化攻击。
