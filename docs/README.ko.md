# Bye Bye Steven

[English](../README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | **[한국어](README.ko.md)**

[Mewgenics](https://store.steampowered.com/app/686060/Mewgenics/)에서 **Steven** 안티 세이브 스캠 페널티를 제거하는 Windows 도구입니다.

Steven은 세이브 재로드를 감지하고 플레이어를 벌하는 NPC입니다. 이 도구는 한 번의 클릭으로 페널티를 리셋합니다.

<p align="center">
  <img src="screenshot.png" alt="Bye Bye Steven" width="400">
</p>

## 기능

- 원클릭 페널티 제거
- 변경 전 세이브 파일 자동 백업
- 재시작 및 제거: 게임 종료, 페널티 제거, 재실행을 한 번에
- 세이브 파일 및 게임 실행 파일 경로 자동 감지
- 5개 언어 지원 (영/중간/중번/일/한)

## 다운로드

[Releases](https://github.com/TaliesinYang/mewgenics-bye-bye-steven/releases) 페이지에서 최신 버전을 다운로드하세요.

단일 `.exe` 파일, 설치 불필요.

## 사용 방법

1. `ByeByeSteven.exe` 실행
2. 세이브 파일 경로가 자동으로 감지됩니다
3. **페널티 제거** 클릭하여 Steven 플래그 리셋
4. 또는 **재시작 및 제거**로 게임 종료, 페널티 제거, 재실행을 한 번에

> 파일 잠금 문제를 피하려면 페널티 제거 전에 Mewgenics를 종료하세요.

## 소스에서 빌드

[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)가 필요합니다.

```bash
dotnet publish src/MewgenicsSaveGuardian.csproj -c Release -o publish
```

## 라이선스

[CC BY-NC 4.0](../LICENSE) - 자유롭게 사용 및 수정 가능, 상업적 사용 금지.
