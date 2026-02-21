# Bye Bye Steven

[English](../README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | **[日本語](README.ja.md)** | [한국어](README.ko.md)

> もう Steven には会わない。

[Mewgenics](https://store.steampowered.com/app/686060/Mewgenics/) の **Steven** アンチセーブスカム ペナルティを解除する Windows ツールです。

Steven はセーブの再読み込みを検出して罰を与える NPC です。このツールはワンクリックでペナルティをリセットします。

<p align="center">
  <img src="screenshot.png" alt="Bye Bye Steven" width="400">
</p>

## 機能

- **ワンクリックでペナルティ解除** — Steven フラグを即座にリセット
- **自動バックアップ** — 変更前にセーブファイルを自動バックアップ
- **再起動して解除** — ゲーム終了、ペナルティ解除、再起動を一括実行
- **パス自動検出** — セーブファイルとゲーム実行ファイルを自動検出
- **5言語対応** — English, 简体中文, 繁體中文, 日本語, 한국어

## 仕組み

Mewgenics のセーブファイルは SQLite データベースです。ゲームがセーブの再読み込みを検出すると、`properties` テーブルの `savescumlocation` フィールドを `1` に設定し、Steven が出現します。このツールはその値を `0` にリセットするだけです。

**テスト済みバージョン：** Mewgenics `v1.0.20695`（2026年2月時点の最新版）

> [!WARNING]
> 今後のゲームアップデートでセーブファイルの構造が変更される可能性があり、本ツールが機能しなくなったり予期しない動作を引き起こす場合があります。ゲーム更新後は本ツールの新バージョンを確認してください。

## ダウンロード

[Releases](https://github.com/TaliesinYang/mewgenics-bye-bye-steven/releases) ページから最新版をダウンロードできます。

単一の `.exe` ファイル、インストール不要。

## 使用方法

1. `ByeByeSteven.exe` を実行
2. セーブファイルのパスが自動検出されます
3. **ペナルティ解除** をクリックして Steven フラグをリセット
4. または **再起動して解除** でゲーム終了、ペナルティ解除、再起動を一括実行

> [!IMPORTANT]
> **使用前にセーブファイルを自分でバックアップしてください。** ツールは自動バックアップを作成しますが、手動でのバックアップも強く推奨します。セーブファイルの場所：
> ```
> %APPDATA%\Glaiel Games\Mewgenics\mewgenics.sav
> ```

## ソースからビルド

[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) が必要です。

```bash
dotnet publish src/MewgenicsSaveGuardian.csproj -c Release -o publish
```

## 免責事項

本ツールは一切の保証なく「現状のまま」提供されます。作者は本ツールの使用に起因するデータ損失、セーブファイルの破損、その他の損害について**一切の責任を負いません。** **自己責任でご使用ください。**

## ライセンス

[CC BY-NC 4.0](../LICENSE) — 自由に使用・改変可能、商用利用は禁止。
