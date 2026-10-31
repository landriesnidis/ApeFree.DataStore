# ApeFree.DataStore.Registry

[![NuGet Version](https://img.shields.io/nuget/v/ApeFree.DataStore.Registry.svg)](https://www.nuget.org/packages/ApeFree.DataStore.Registry/)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/landriesnidis/ApeFree.DataStore/blob/main/LICENSE)

基于 `ApeFree.DataStore` 提供 Windows 注册表对象持久化存储器的扩展实现包。

---

## 🌟 核心特性

- **Windows 原生集成**：支持 `RegistryHive.CurrentUser` 与 `RegistryHive.LocalMachine`。
- **配置防篡改**：结合 AES 加密过滤器，可将机密授权与机器参数以密文二进制存储在注册表中，防止普通用户直接查看或篡改。
- **现代化多目标**：针对 `net8.0-windows`、`net10.0-windows` 以及 `netstandard2.0`（通过 `Microsoft.Win32.Registry`）全适配。

---

## 📦 安装方式

```bash
dotnet add package ApeFree.DataStore.Registry --version 3.0.0
```

---

## 🚀 快速上手

```csharp
using ApeFree.DataStore;
using ApeFree.DataStore.Registry;
using Microsoft.Win32;

// 1. 创建注册表存储配置
var settings = new RegistryStoreAccessSettings(
    RegistryHive.CurrentUser,
    @"Software\MyCompany\MyApp",
    "LicenseInfo"
);

// 2. 创建 RegistryStore 实例
var store = StoreFactory.Factory.CreateRegistryStore<LicenseModel>(settings);
store.Load();
store.Value.LicenseKey = "ABCDE-12345-67890";
store.Save();
```

---

## 📄 开源许可证

本项目基于 [Apache-2.0](https://github.com/landriesnidis/ApeFree.DataStore/blob/main/LICENSE) 许可证开源。
