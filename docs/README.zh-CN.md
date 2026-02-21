# Bye Bye Steven

[English](../README.md) | **[简体中文](README.zh-CN.md)** | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

一个 Windows 工具，用于移除 [Mewgenics](https://store.steampowered.com/app/686060/Mewgenics/) 中 **Steven** 的反 SL 惩罚。

Steven 是一个能检测到你重新加载存档的 NPC，然后会出来惩罚你。这个工具一键重置惩罚。

<p align="center">
  <img src="screenshot.png" alt="Bye Bye Steven" width="400">
</p>

## 功能

- 一键移除惩罚
- 修改前自动备份存档
- 重启并清除：关闭游戏、移除惩罚、重新启动一步到位
- 自动检测存档文件和游戏程序路径
- 支持 5 种语言（英/简中/繁中/日/韩）

## 下载

前往 [Releases](https://github.com/TaliesinYang/mewgenics-bye-bye-steven/releases) 页面下载最新版本。

单个便携 `.exe`，无需安装。

## 使用方法

1. 运行 `ByeByeSteven.exe`
2. 存档路径会自动检测
3. 点击 **移除惩罚** 重置 Steven 标记
4. 或使用 **重启并清除** 一键关闭游戏、清除惩罚并重启

> 移除惩罚前请先关闭 Mewgenics，避免文件锁定问题。

## 从源码构建

需要 [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)。

```bash
dotnet publish src/MewgenicsSaveGuardian.csproj -c Release -o publish
```

## 许可证

[CC BY-NC 4.0](../LICENSE) - 可自由使用和修改，禁止商用。
