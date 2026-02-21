# Mewgenics Save Guardian

## English

Mewgenics Save Guardian is a Windows WPF tool that removes the "Steven" anti-save-scum penalty in Mewgenics. Steven is an NPC who detects when players reload saves, and when the savescumlocation flag is set to 1, he appears to punish the player. This tool resets that penalty with a single click.

### Features

- **One-Click Penalty Removal** — Remove the Steven penalty instantly
- **Automatic Backups** — Creates backups of your save file before making changes
- **Game Process Management** — Detects and handles Mewgenics game process automatically
- **Auto-Detect Save Path** — Automatically locates your Mewgenics save file
- **5-Language UI** — Supports English, Simplified Chinese, Traditional Chinese, Japanese, and Korean

### Usage

1. Run `MewgenicsSaveGuardian.exe`
2. The tool will automatically detect your Mewgenics save file location
3. Close the Mewgenics game if it's currently running
4. Click the "Remove Penalty" button to reset the Steven penalty

### Build Instructions

Requirements: .NET 9 SDK

```bash
dotnet publish src/MewgenicsSaveGuardian.csproj -c Release -o publish
```

The compiled executable will be available in the `publish` folder.

### Note

Always close the Mewgenics game before removing the penalty to ensure the save file is not locked.

---

## 简体中文

Mewgenics Save Guardian 是一个 Windows WPF 工具，用于移除 Mewgenics 游戏中的"Steven"防存档重置惩罚。Steven 是一个 NPC，他能检测玩家是否重新加载存档，当 savescumlocation 标志设置为 1 时，他会出现惩罚玩家。这个工具只需点击一下即可重置该惩罚。

### 功能

- **一键移除惩罚** — 立即移除 Steven 惩罚
- **自动备份** — 更改前自动创建存档文件备份
- **游戏进程管理** — 自动检测并管理 Mewgenics 游戏进程
- **自动检测存档路径** — 自动定位您的 Mewgenics 存档文件
- **5 种语言 UI** — 支持英文、简体中文、繁体中文、日文和韩文

### 使用方法

1. 运行 `MewgenicsSaveGuardian.exe`
2. 工具将自动检测您的 Mewgenics 存档文件位置
3. 如果 Mewgenics 游戏正在运行，请关闭它
4. 点击"移除惩罚"按钮来重置 Steven 惩罚

### 构建说明

要求: .NET 9 SDK

```bash
dotnet publish src/MewgenicsSaveGuardian.csproj -c Release -o publish
```

编译的可执行文件将在 `publish` 文件夹中可用。

### 注意

在移除惩罚前，请务必关闭 Mewgenics 游戏，以确保存档文件未被锁定。

---

## 繁體中文

Mewgenics Save Guardian 是一個 Windows WPF 工具，用於移除 Mewgenics 遊戲中的「Steven」防存檔重置懲罰。Steven 是一個 NPC，他能偵測玩家是否重新載入存檔，當 savescumlocation 標誌設置為 1 時，他會出現懲罰玩家。這個工具只需點擊一下即可重置該懲罰。

### 功能

- **一鍵移除懲罰** — 立即移除 Steven 懲罰
- **自動備份** — 更改前自動建立存檔文件備份
- **遊戲程序管理** — 自動偵測並管理 Mewgenics 遊戲程序
- **自動偵測存檔路徑** — 自動定位您的 Mewgenics 存檔文件
- **5 種語言 UI** — 支援英文、簡體中文、繁體中文、日文和韓文

### 使用方法

1. 執行 `MewgenicsSaveGuardian.exe`
2. 工具將自動偵測您的 Mewgenics 存檔文件位置
3. 如果 Mewgenics 遊戲正在執行，請關閉它
4. 點擊「移除懲罰」按鈕來重置 Steven 懲罰

### 構建說明

要求：.NET 9 SDK

```bash
dotnet publish src/MewgenicsSaveGuardian.csproj -c Release -o publish
```

編譯的可執行文件將在 `publish` 資料夾中可用。

### 注意

在移除懲罰前，請務必關閉 Mewgenics 遊戲，以確保存檔文件未被鎖定。

---

## 日本語

Mewgenics Save Guardian は、Mewgenics ゲームの「Steven」アンチセーブスカム ペナルティを削除する Windows WPF ツールです。Steven はプレイヤーがセーブをリロードしたことを検出する NPC で、savescumlocation フラグが 1 に設定されている場合、プレイヤーを罰するために現れます。このツールは 1 回のクリックでそのペナルティをリセットします。

### 機能

- **ワンクリック ペナルティ削除** — Steven ペナルティを即座に削除
- **自動バックアップ** — 変更前に自動的にセーブファイルのバックアップを作成
- **ゲームプロセス管理** — Mewgenics ゲームプロセスを自動的に検出および管理
- **セーブパス自動検出** — Mewgenics セーブファイルを自動的に検出
- **5 言語 UI** — 英語、簡体中国語、繁体中国語、日本語、韓国語に対応

### 使用方法

1. `MewgenicsSaveGuardian.exe` を実行
2. ツールが自動的に Mewgenics セーブファイルの場所を検出します
3. Mewgenics ゲームが実行中の場合は終了してください
4. 「ペナルティを削除」ボタンをクリックして Steven ペナルティをリセット

### ビルド手順

要件：.NET 9 SDK

```bash
dotnet publish src/MewgenicsSaveGuardian.csproj -c Release -o publish
```

コンパイルされた実行可能ファイルは `publish` フォルダで利用可能になります。

### 注意

ペナルティを削除する前に、必ず Mewgenics ゲームを終了して、セーブファイルがロックされていないことを確認してください。

---

## 한국어

Mewgenics Save Guardian는 Mewgenics 게임에서 「Steven」 안티 세이브 스캠 페널티를 제거하는 Windows WPF 도구입니다. Steven은 플레이어가 세이브를 다시 로드했을 때를 감지하는 NPC이며, savescumlocation 플래그가 1로 설정되면 플레이어를 벌합니다. 이 도구는 한 번의 클릭으로 해당 페널티를 리셋합니다.

### 기능

- **원클릭 페널티 제거** — Steven 페널티를 즉시 제거
- **자동 백업** — 변경하기 전에 자동으로 세이브 파일 백업 생성
- **게임 프로세스 관리** — Mewgenics 게임 프로세스를 자동으로 감지 및 관리
- **자동 세이브 경로 감지** — Mewgenics 세이브 파일을 자동으로 찾음
- **5개 언어 UI** — 영어, 중국어 간체, 중국어 번체, 일본어, 한국어 지원

### 사용 방법

1. `MewgenicsSaveGuardian.exe` 실행
2. 도구가 자동으로 Mewgenics 세이브 파일 위치를 감지합니다
3. Mewgenics 게임이 실행 중이면 종료하세요
4. 「페널티 제거」버튼을 클릭하여 Steven 페널티 리셋

### 빌드 지침

요구 사항: .NET 9 SDK

```bash
dotnet publish src/MewgenicsSaveGuardian.csproj -c Release -o publish
```

컴파일된 실행 파일은 `publish` 폴더에서 사용할 수 있습니다.

### 주의

페널티를 제거하기 전에 반드시 Mewgenics 게임을 종료하여 세이브 파일이 잠기지 않도록 하세요.
