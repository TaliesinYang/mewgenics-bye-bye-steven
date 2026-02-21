# Bye Bye Steven

[English](../README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | **[日本語](README.ja.md)** | [한국어](README.ko.md)

[Mewgenics](https://store.steampowered.com/app/686060/Mewgenics/) の **Steven** アンチセーブスカム ペナルティを解除する Windows ツールです。

Steven はセーブの再読み込みを検出して罰を与える NPC です。このツールはワンクリックでペナルティをリセットします。

<p align="center">
  <img src="screenshot.png" alt="Bye Bye Steven" width="400">
</p>

## 機能

- ワンクリックでペナルティ解除
- 変更前にセーブファイルを自動バックアップ
- 再起動して解除：ゲーム終了、ペナルティ解除、再起動を一括実行
- セーブファイルとゲーム実行ファイルのパスを自動検出
- 5言語対応（英/簡中/繁中/日/韓）

## ダウンロード

[Releases](https://github.com/TaliesinYang/mewgenics-bye-bye-steven/releases) ページから最新版をダウンロードできます。

単一の `.exe` ファイル、インストール不要。

## 使用方法

1. `ByeByeSteven.exe` を実行
2. セーブファイルのパスが自動検出されます
3. **ペナルティ解除** をクリックして Steven フラグをリセット
4. または **再起動して解除** でゲーム終了、ペナルティ解除、再起動を一括実行

> ファイルロックを避けるため、ペナルティ解除前に Mewgenics を終了してください。

## ソースからビルド

[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) が必要です。

```bash
dotnet publish src/MewgenicsSaveGuardian.csproj -c Release -o publish
```

## ライセンス

[CC BY-NC 4.0](../LICENSE) - 自由に使用・改変可能、商用利用は禁止。
