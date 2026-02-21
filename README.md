# Bye Bye Steven

**[English](README.md)** | [简体中文](docs/README.zh-CN.md) | [繁體中文](docs/README.zh-TW.md) | [日本語](docs/README.ja.md) | [한국어](docs/README.ko.md)

> Never see Steven again.

A Windows tool to remove the **Steven** anti-save-scum penalty in [Mewgenics](https://store.steampowered.com/app/686060/Mewgenics/).

Steven is an NPC who detects save reloading and punishes you for it. This tool resets the penalty with a single click.

<p align="center">
  <img src="docs/screenshot.png" alt="Bye Bye Steven" width="400">
</p>

## Features

- **One-click penalty removal** — Reset the Steven flag instantly
- **Automatic backups** — Save file is backed up before any changes
- **Restart & Clear** — Close game, remove penalty, relaunch in one click
- **Auto-detect paths** — Save file and game executable found automatically
- **5 languages** — English, 简体中文, 繁體中文, 日本語, 한국어

## How It Works

The Mewgenics save file is a SQLite database. When the game detects save reloading, it sets the `savescumlocation` field in the `properties` table to `1`, which triggers Steven's appearance. This tool simply resets that value back to `0`.

**Tested on:** Mewgenics `v1.0.20695` (latest as of Feb 2026)

> [!WARNING]
> Future game updates may change the save file structure, which could make this tool ineffective or cause unexpected behavior. Always check for updated versions of this tool after a game update.

## Download

Get the latest release from the [Releases](https://github.com/TaliesinYang/mewgenics-bye-bye-steven/releases) page.

Single portable `.exe` — no installation needed.

## Usage

1. Run `ByeByeSteven.exe`
2. Save file path is detected automatically
3. Click **Remove Penalty** to reset the Steven flag
4. Or use **Restart & Clear** to close game, clear penalty, and relaunch

> [!IMPORTANT]
> **Back up your save file before using this tool.** While the tool creates automatic backups, it is strongly recommended to manually back up your save file as well. The save file is located at:
> ```
> %APPDATA%\Glaiel Games\Mewgenics\mewgenics.sav
> ```

## Build from Source

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```bash
dotnet publish src/MewgenicsSaveGuardian.csproj -c Release -o publish
```

## Disclaimer

This tool is provided **as-is** with no warranty of any kind. The author is **not responsible** for any data loss, save file corruption, or other damages resulting from the use of this tool. **Use at your own risk.**

## License

[CC BY-NC 4.0](LICENSE) — Free to use and modify, no commercial use.
