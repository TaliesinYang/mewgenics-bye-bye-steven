# Bye Bye Steven

[English](../README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | **[한국어](README.ko.md)**

> 다시는 Steven 을 만나지 않기.

[Mewgenics](https://store.steampowered.com/app/686060/Mewgenics/)에서 **Steven** 안티 세이브 스캠 페널티를 제거하는 Windows 도구입니다.

Steven은 세이브 재로드를 감지하고 플레이어를 벌하는 NPC입니다. 이 도구는 한 번의 클릭으로 페널티를 리셋합니다.

<p align="center">
  <img src="screenshot.png" alt="Bye Bye Steven" width="400">
</p>

## 기능

- **원클릭 페널티 제거** — Steven 플래그를 즉시 리셋
- **자동 백업** — 변경 전 세이브 파일 자동 백업
- **재시작 및 제거** — 게임 종료, 페널티 제거, 재실행을 한 번에
- **경로 자동 감지** — 세이브 파일 및 게임 실행 파일 경로 자동 감지
- **5개 언어 지원** — English, 简体中文, 繁體中文, 日本語, 한국어

## 작동 원리

Mewgenics의 세이브 파일은 SQLite 데이터베이스입니다. 게임이 세이브 재로드를 감지하면 `properties` 테이블의 `savescumlocation` 필드를 `1`로 설정하여 Steven이 나타납니다. 이 도구는 해당 값을 `0`으로 리셋합니다.

**테스트 버전:** Mewgenics `v1.0.20695` (2026년 2월 기준 최신)

> [!WARNING]
> 향후 게임 업데이트로 세이브 파일 구조가 변경될 수 있으며, 이로 인해 본 도구가 작동하지 않거나 예상치 못한 동작이 발생할 수 있습니다. 게임 업데이트 후 본 도구의 새 버전을 확인하세요.

## 다운로드

[Releases](https://github.com/TaliesinYang/mewgenics-bye-bye-steven/releases) 페이지에서 최신 버전을 다운로드하세요.

단일 `.exe` 파일, 설치 불필요.

## 사용 방법

1. `ByeByeSteven.exe` 실행
2. 세이브 파일 경로가 자동으로 감지됩니다
3. **페널티 제거** 클릭하여 Steven 플래그 리셋
4. 또는 **재시작 및 제거**로 게임 종료, 페널티 제거, 재실행을 한 번에

> [!IMPORTANT]
> **사용 전 세이브 파일을 직접 백업하세요.** 도구가 자동 백업을 생성하지만 수동 백업도 강력히 권장합니다. 세이브 파일 위치:
> ```
> %APPDATA%\Glaiel Games\Mewgenics\mewgenics.sav
> ```

## 소스에서 빌드

[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)가 필요합니다.

```bash
dotnet publish src/MewgenicsSaveGuardian.csproj -c Release -o publish
```

## 면책 조항

본 도구는 어떠한 보증도 없이 「있는 그대로」 제공됩니다. 작성자는 본 도구 사용으로 인한 데이터 손실, 세이브 파일 손상 또는 기타 손해에 대해 **일체의 책임을 지지 않습니다.** **사용에 따른 위험은 본인이 부담합니다.**

## 라이선스

[CC BY-NC 4.0](../LICENSE) — 자유롭게 사용 및 수정 가능, 상업적 사용 금지.
