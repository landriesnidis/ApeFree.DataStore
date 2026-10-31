# ApeFree.DataStore.Registry

<div align="center">

![ApeFree.DataStore](../../ApeFree_64px.png)

### 基于 ApeFree.DataStore 的 Windows 注册表对象持久化存储扩展

[![NuGet Version](https://img.shields.io/nuget/v/ApeFree.DataStore.Registry.svg?style=flat-square&color=blue)](https://www.nuget.org/packages/ApeFree.DataStore.Registry/)
[![License](https://img.shields.io/badge/license-Apache--2.0-green.svg?style=flat-square)](https://github.com/landriesnidis/ApeFree.DataStore/blob/main/LICENSE)
[![.NET Compatibility](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0--windows%20%7C%20net10.0--windows-purple.svg?style=flat-square)](#)

</div>

---

## 📖 简介

`ApeFree.DataStore.Registry` 是 `ApeFree.DataStore` 面向 Windows 平台系统级持久化的官方扩展包。它允许将内存中的复杂业务对象透明持久化到 Windows 系统注册表（Registry）中，适用于软件授权激活凭据、机器硬件指纹绑定、工控单机防普通用户篡改参数等场景。

---

## 🌟 核心特性

- **Windows 原生无缝集成**：支持 `RegistryHive.CurrentUser`（当前用户）与 `RegistryHive.LocalMachine`（系统全局）。
- **注册表二进制安全存取**：自动采用 `RegistryValueKind.Binary` 进行原子存取。
- **结合 AES 深度防篡改**：配合 `AesEncryptionStreamFilter`，在注册表中完全以密文形式存储，防止普通用户直接通过 `regedit` 窥视或篡改关键授权配置。
- **现代化多目标兼容**：针对 `net8.0-windows`、`net10.0-windows` 原生 Desktop 框架及 `netstandard2.0`（通过官方 `Microsoft.Win32.Registry`）全适配，自动识别 64位/32位 系统视口。

---

## 📦 安装方式

```bash
dotnet add package ApeFree.DataStore.Registry
```

---

## 🚀 快速上手

### 1. 基础用法

```csharp
using ApeFree.DataStore;
using ApeFree.DataStore.Registry;
using Microsoft.Win32;

// 1. 创建注册表存储配置（存入当前用户的 HKCU\Software\MyCompany\MyApp\Config）
var settings = new RegistryStoreAccessSettings(
    RegistryHive.CurrentUser,
    @"Software\MyCompany\MyApp\Config",
    "AppLicense"
);

// 2. 构建 RegistryStore 实例
var store = StoreFactory.Factory.CreateRegistryStore<LicenseModel>(settings, () => new LicenseModel
{
    LicenseKey = "INITIAL-KEY-0000",
    ExpireDate = DateTime.Now.AddDays(30)
});

// 3. 加载与读取
store.Load();
Console.WriteLine($"授权码: {store.Value.LicenseKey}");

// 4. 修改并持久化到注册表
store.Value.LicenseKey = "ABCDE-12345-67890";
store.Save();
```

### 2. 结合 AES 加密与 GZip 压缩防篡改

```csharp
using ApeFree.DataStore.Filters;
using System.Security.Cryptography;
using System.Text;

// 配置 AES 加密密钥
byte[] key = Encoding.UTF8.GetBytes("01234567890123456789012345678901");
byte[] iv  = Encoding.UTF8.GetBytes("0123456789abcdef");

// 注册表数据经过 GZip 压缩与 AES-256 加密后写入，注册表编辑器中只显示乱码二进制
settings.StreamFilters.Add(new GZipStreamFilter());
settings.StreamFilters.Add(new AesEncryptionStreamFilter(key, iv));

store.Save();
```

---

## 💾 配置选项字典

| 配置类 | 属性名称 | 类型 | 说明 |
| :--- | :--- | :--- | :--- |
| **`RegistryStoreAccessSettings`** | `BaseKey` | `RegistryHive` | 注册表根键（`CurrentUser` 或 `LocalMachine`，构造函数必填） |
| | `Path` | `string` | 注册表子键路径（如 `@"Software\MyCompany\MyApp"`，构造函数必填） |
| | `Key` | `string` | 目标项的值名称（Value Name，构造函数必填） |
| | `SerializationAdapter` | `ISerializationAdapter` | 序列化适配器（支持 Jbin / Json / Xml，默认 Jbin） |
| | `StreamFilters` | `IList<IStreamFilter>` | 流过滤器管道列表（支持 GZip、Deflate、AES 等） |

---

## 📄 开源许可证

本项目基于 [Apache-2.0](https://github.com/landriesnidis/ApeFree.DataStore/blob/main/LICENSE) 许可证开源。
