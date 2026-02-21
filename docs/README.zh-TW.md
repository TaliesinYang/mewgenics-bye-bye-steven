# Bye Bye Steven

[English](../README.md) | [简体中文](README.zh-CN.md) | **[繁體中文](README.zh-TW.md)** | [日本語](README.ja.md) | [한국어](README.ko.md)

> 讓 Steven 永遠消失。

一個 Windows 工具，用於移除 [Mewgenics](https://store.steampowered.com/app/686060/Mewgenics/) 中 **Steven** 的反 SL 懲罰。

Steven 是一個能偵測到你重新載入存檔的 NPC，然後會出來懲罰你。這個工具一鍵重置懲罰。

<p align="center">
  <img src="screenshot.png" alt="Bye Bye Steven" width="400">
</p>

## 功能

- **一鍵移除懲罰** — 立即重置 Steven 標記
- **自動備份** — 修改前自動備份存檔
- **重啟並清除** — 關閉遊戲、移除懲罰、重新啟動一步到位
- **自動偵測路徑** — 自動偵測存檔檔案和遊戲程式路徑
- **5 種語言** — English, 简体中文, 繁體中文, 日本語, 한국어

## 原理

Mewgenics 的存檔檔案是一個 SQLite 資料庫。當遊戲偵測到存檔重載時，會將 `properties` 表中的 `savescumlocation` 欄位設為 `1`，從而觸發 Steven 出現。本工具只是將該值重置為 `0`。

**測試版本：** Mewgenics `v1.0.20695`（截至 2026 年 2 月最新版）

> [!WARNING]
> 未來的遊戲更新可能會更改存檔檔案結構，這可能導致本工具失效或產生意外行為。遊戲更新後請檢查本工具是否有新版本。

## 下載

前往 [Releases](https://github.com/TaliesinYang/mewgenics-bye-bye-steven/releases) 頁面下載最新版本。

單個便攜 `.exe`，無需安裝。

## 使用方法

1. 執行 `ByeByeSteven.exe`
2. 存檔路徑會自動偵測
3. 點擊 **移除懲罰** 重置 Steven 標記
4. 或使用 **重啟並清除** 一鍵關閉遊戲、清除懲罰並重啟

> [!IMPORTANT]
> **使用前請自行備份存檔檔案。** 雖然工具會自動建立備份，但強烈建議手動備份。存檔檔案位於：
> ```
> %APPDATA%\Glaiel Games\Mewgenics\mewgenics.sav
> ```

## 從原始碼構建

需要 [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)。

```bash
dotnet publish src/MewgenicsSaveGuardian.csproj -c Release -o publish
```

## 免責聲明

本工具按「現狀」提供，不附帶任何形式的擔保。作者**不對**因使用本工具造成的任何資料遺失、存檔損壞或其他損失負責。**使用風險自負。**

## 授權條款

[CC BY-NC 4.0](../LICENSE) — 可自由使用和修改，禁止商用。
