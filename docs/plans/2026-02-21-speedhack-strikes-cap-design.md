# Speedhack + Steven Strikes Cap - Design Document

Date: 2026-02-21

## Overview

Two new features for Bye Bye Steven:
1. **Speedhack** - Game speed control via DLL injection and time function hooking
2. **Steven Strikes Cap** - Automatically cap Steven strikes to 1 during penalty removal

## Feature 1: Speedhack

### Architecture

```
C# WPF App (SpeedHackService)        Mewgenics.exe Process
+---------------------------+         +------------------------+
| 1. Detect game process    |         |                        |
| 2. Inject speedhack.dll   | ------> | speedhack.dll loaded   |
| 3. Write speed multiplier |         | - Hooks time functions |
|    to shared memory       | <-----> | - Reads shared memory  |
| 4. UI: slider + checkbox  |         | - Applies multiplier   |
+---------------------------+         +------------------------+
         ^                                      |
         |          Named Shared Memory          |
         +------ "BBS_SpeedHack" (double) -------+
```

### Native DLL: `speedhack.dll`

**Source:** `native/speedhack.c`
**Compiler:** GCC (MSYS2 UCRT64), 64-bit
**Build:** `gcc -shared -o speedhack.dll speedhack.c -lntdll`

**Hooks:**
- `QueryPerformanceCounter` - Primary high-resolution timer used by most games
- `GetTickCount64` - Fallback timer

**Hooking method:** IAT (Import Address Table) patching
- Simpler and more reliable than inline hooking
- Walk the PE import table and replace function pointers

**IPC:** Named shared memory (`BBS_SpeedHack`)
- Layout: `double speed_multiplier` (8 bytes)
- DLL reads this on every hooked call
- C# app writes to it when user changes speed

**Logic:**
```
real_elapsed = real_QPC() - last_real_QPC
fake_elapsed = real_elapsed * speed_multiplier
return last_fake_QPC + fake_elapsed
```

### C# Service: `SpeedHackService.cs`

**Responsibilities:**
- `InjectDll(int processId, string dllPath)` - Inject via CreateRemoteThread + LoadLibraryW
- `SetSpeed(double multiplier)` - Write to shared memory
- `IsInjected(int processId)` - Check if DLL is already loaded in process
- `Cleanup()` - Close shared memory handle

**Win32 P/Invoke needed:**
- `OpenProcess`, `VirtualAllocEx`, `WriteProcessMemory`, `CreateRemoteThread`
- `CreateFileMapping`, `MapViewOfFile` (shared memory)
- `EnumProcessModules`, `GetModuleFileNameEx` (check injection)

### Settings Persistence

Add to `AppSettings.cs`:
```csharp
public bool SpeedEnabled { get; set; }
public double SpeedMultiplier { get; set; } = 1.0;
```

### Auto-restore Behavior

In the poll timer (every 2s):
1. If `SpeedEnabled == true` AND game just started running (wasn't running before)
2. Auto-inject DLL and set saved speed multiplier
3. Status message: "Speed hack applied: {multiplier}x"

### UI: New Tab "TOOLS"

```
[STATUS] [BACKUPS] [TOOLS]
                    ^^^^^^^

+----------------------------------+
| SPEED CONTROL                    |
|                                  |
| [x] Enable Speedhack            |
|                                  |
| Speed  [===5.0===]  [ 5.0 ]     |
|        0.1       10.0            |
|                                  |
+----------------------------------+
```

- CheckBox: toggles speed hack on/off (inject/uninject)
- Slider: 0.1 to 10.0, step 0.1
- TextBox: direct number input, synced with slider
- Enabling when game not running: remember setting, inject when game starts

## Feature 2: Steven Strikes Cap

### Implementation

Modify `SaveFileService.ResetPenalty()`:

After resetting `savescumlocation` to 0, also:
1. Count `NPCRSTRACKER_steven_savescum_*` records
2. If count > 1, delete records keeping only the first one (lowest key)

```sql
-- Keep only the record with the smallest key
DELETE FROM properties
WHERE key LIKE 'NPCRSTRACKER_steven_savescum_%'
AND key NOT IN (
    SELECT key FROM properties
    WHERE key LIKE 'NPCRSTRACKER_steven_savescum_%'
    ORDER BY key ASC
    LIMIT 1
)
```

### Integration Points

- `RemovePenaltyAsync` -> calls `ResetPenalty` -> auto caps strikes
- `RestartAndClearAsync` -> calls `ResetPenalty` -> auto caps strikes
- No new UI elements needed

## File Changes Summary

| File | Change |
|------|--------|
| `native/speedhack.c` | NEW - Native DLL source |
| `native/build.sh` | NEW - Build script for GCC |
| `src/Services/SpeedHackService.cs` | NEW - DLL injection + shared memory |
| `src/Services/SaveFileService.cs` | MODIFY - Add strikes cap to ResetPenalty |
| `src/Models/AppSettings.cs` | MODIFY - Add SpeedEnabled, SpeedMultiplier |
| `src/ViewModels/MainViewModel.cs` | MODIFY - Add speed properties, auto-inject logic |
| `src/MainWindow.xaml` | MODIFY - Add TOOLS tab |
| `src/Localization/Loc.cs` | MODIFY - Add speed-related strings |
| `src/Resources/Styles.xaml` | MODIFY - Add Slider style |

## Risks

- **Antivirus false positives**: DLL injection is flagged by many AV. Users may need to whitelist.
- **Game updates**: If Mewgenics changes time APIs, speedhack may break.
- **64-bit only**: DLL must match game architecture (64-bit).
- **Admin rights**: DLL injection may require elevated privileges depending on game process protection.
