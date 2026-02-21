# Mewgenics Save Guardian - Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a Windows GUI tool that resets the savescumlocation flag in Mewgenics save files, bypassing the "Steven" anti-save-scum penalty system with one click.

**Architecture:** C# WPF app using MVVM pattern. Services handle SQLite, backup, process, and settings. ViewModel binds UI state and commands. All modifications are preceded by automatic backups and integrity checks.

**Tech Stack:** .NET 9, WPF, Microsoft.Data.Sqlite, System.Text.Json, xUnit + Moq for tests

**Project Root:** `C:\Users\Alex\AppData\Roaming\Glaiel Games\Mewgenics\MewgenicsSaveGuardian\`

**Save File Location (verified):** `%APPDATA%/Glaiel Games/Mewgenics/{SteamID}/saves/steamcampaign01.sav`

**DB Schema (verified):**
- Table: `properties` — `CREATE TABLE properties (key TEXT PRIMARY KEY, data ANY) STRICT;`
- Key: `savescumlocation` — value `0` or `1` (1 = penalty active)
- Key: `PlotFlag_StevenMet` — value `1` if Steven encountered
- Key pattern: `NPCRSTRACKER_steven_savescum_*` — Steven strike history (count = strikes)
- Key: `on_adventure` — `1` if currently on adventure
- Key: `current_day` — integer day number
- Key: `adventure_started` — `1` if adventure begun

---

### Task 1: Create Solution and Project Structure

**Files:**
- Create: `MewgenicsSaveGuardian.sln`
- Create: `src/MewgenicsSaveGuardian.csproj`
- Create: `tests/MewgenicsSaveGuardian.Tests/MewgenicsSaveGuardian.Tests.csproj`
- Create: `.gitignore`

**Step 1: Initialize WPF project**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet new wpf -n MewgenicsSaveGuardian -o src --framework net9.0
```
Expected: WPF project created in `src/`

**Step 2: Initialize test project**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet new xunit -n MewgenicsSaveGuardian.Tests -o tests/MewgenicsSaveGuardian.Tests --framework net9.0
```
Expected: xUnit project created in `tests/`

**Step 3: Create solution and add projects**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet new sln -n MewgenicsSaveGuardian
dotnet sln add src/MewgenicsSaveGuardian.csproj
dotnet sln add tests/MewgenicsSaveGuardian.Tests/MewgenicsSaveGuardian.Tests.csproj
dotnet add tests/MewgenicsSaveGuardian.Tests/MewgenicsSaveGuardian.Tests.csproj reference src/MewgenicsSaveGuardian.csproj
```

**Step 4: Add NuGet packages**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet add src/MewgenicsSaveGuardian.csproj package Microsoft.Data.Sqlite
dotnet add src/MewgenicsSaveGuardian.csproj package CommunityToolkit.Mvvm
dotnet add tests/MewgenicsSaveGuardian.Tests/MewgenicsSaveGuardian.Tests.csproj package Microsoft.Data.Sqlite
dotnet add tests/MewgenicsSaveGuardian.Tests/MewgenicsSaveGuardian.Tests.csproj package Moq
```

**Step 5: Create directory structure**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian/src"
mkdir -p Models Services ViewModels Converters Resources
```

**Step 6: Create .gitignore**

Create file `.gitignore`:
```
bin/
obj/
.vs/
*.user
*.suo
*.DotSettings
```

**Step 7: Verify build**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet build
```
Expected: Build succeeded. 0 Warning(s). 0 Error(s).

**Step 8: Commit**

```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
git init
git add -A
git commit -m "chore: initialize solution with WPF and test projects"
```

---

### Task 2: Implement Models

**Files:**
- Create: `src/Models/SaveFileInfo.cs`
- Create: `src/Models/BackupEntry.cs`
- Create: `src/Models/AppSettings.cs`

**Step 1: Write SaveFileInfo model**

Create `src/Models/SaveFileInfo.cs`:
```csharp
namespace MewgenicsSaveGuardian.Models;

public record SaveFileInfo
{
    public int SaveScumLocation { get; init; }
    public bool OnAdventure { get; init; }
    public int CurrentDay { get; init; }
    public bool StevenMet { get; init; }
    public int StevenStrikes { get; init; }
    public bool AdventureStarted { get; init; }
    public DateTime LastModified { get; init; }
    public long FileSizeBytes { get; init; }

    public bool IsPenaltyActive => SaveScumLocation == 1;
}
```

**Step 2: Write BackupEntry model**

Create `src/Models/BackupEntry.cs`:
```csharp
namespace MewgenicsSaveGuardian.Models;

public record BackupEntry
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public DateTime Timestamp { get; init; }
    public int CurrentDay { get; init; }
    public int SaveScumFlag { get; init; }
    public long FileSizeBytes { get; init; }
}
```

**Step 3: Write AppSettings model**

Create `src/Models/AppSettings.cs`:
```csharp
namespace MewgenicsSaveGuardian.Models;

public class AppSettings
{
    public string SaveFilePath { get; set; } = string.Empty;
    public int MaxBackups { get; set; } = 5;
    public bool AutoRelaunchGame { get; set; }
    public bool ClearStevenHistory { get; set; }
}
```

**Step 4: Verify build**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet build
```
Expected: Build succeeded.

**Step 5: Commit**

```bash
git add src/Models/
git commit -m "feat: add data models for save info, backup entries, and settings"
```

---

### Task 3: Implement SaveFileService with Tests (TDD)

**Files:**
- Create: `src/Services/SaveFileService.cs`
- Create: `tests/MewgenicsSaveGuardian.Tests/SaveFileServiceTests.cs`

**Step 1: Write the failing tests**

Create `tests/MewgenicsSaveGuardian.Tests/SaveFileServiceTests.cs`:
```csharp
using Microsoft.Data.Sqlite;
using MewgenicsSaveGuardian.Models;
using MewgenicsSaveGuardian.Services;

namespace MewgenicsSaveGuardian.Tests;

public class SaveFileServiceTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SaveFileService _service;

    public SaveFileServiceTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_save_{Guid.NewGuid()}.sav");
        CreateTestDatabase(_testDbPath);
        _service = new SaveFileService();
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }

    private static void CreateTestDatabase(string path, int savescumLocation = 0,
        int currentDay = 5, int onAdventure = 1, int stevenMet = 1,
        int adventureStarted = 1, int stevenStrikes = 2)
    {
        using var conn = new SqliteConnection($"Data Source={path}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE properties (key TEXT PRIMARY KEY, data ANY) STRICT;";
        cmd.ExecuteNonQuery();

        var props = new Dictionary<string, object>
        {
            ["savescumlocation"] = savescumLocation,
            ["current_day"] = currentDay,
            ["on_adventure"] = onAdventure,
            ["PlotFlag_StevenMet"] = stevenMet,
            ["adventure_started"] = adventureStarted,
        };

        foreach (var (key, val) in props)
        {
            cmd.CommandText = "INSERT INTO properties (key, data) VALUES (@k, @v)";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@k", key);
            cmd.Parameters.AddWithValue("@v", val);
            cmd.ExecuteNonQuery();
        }

        for (int i = 1; i <= stevenStrikes; i++)
        {
            cmd.CommandText = "INSERT INTO properties (key, data) VALUES (@k, 1)";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@k", $"NPCRSTRACKER_steven_savescum_{i}alt1");
            cmd.ExecuteNonQuery();
        }
    }

    [Fact]
    public void ReadStatus_should_return_correct_save_info()
    {
        var info = _service.ReadStatus(_testDbPath);

        Assert.Equal(0, info.SaveScumLocation);
        Assert.True(info.OnAdventure);
        Assert.Equal(5, info.CurrentDay);
        Assert.True(info.StevenMet);
        Assert.Equal(2, info.StevenStrikes);
        Assert.True(info.AdventureStarted);
        Assert.False(info.IsPenaltyActive);
    }

    [Fact]
    public void ReadStatus_should_detect_penalty_active()
    {
        var penaltyDb = Path.Combine(Path.GetTempPath(), $"test_penalty_{Guid.NewGuid()}.sav");
        try
        {
            CreateTestDatabase(penaltyDb, savescumLocation: 1);
            var info = _service.ReadStatus(penaltyDb);
            Assert.Equal(1, info.SaveScumLocation);
            Assert.True(info.IsPenaltyActive);
        }
        finally
        {
            if (File.Exists(penaltyDb)) File.Delete(penaltyDb);
        }
    }

    [Fact]
    public void ResetPenalty_should_set_savescumlocation_to_zero()
    {
        var penaltyDb = Path.Combine(Path.GetTempPath(), $"test_reset_{Guid.NewGuid()}.sav");
        try
        {
            CreateTestDatabase(penaltyDb, savescumLocation: 1);

            _service.ResetPenalty(penaltyDb, clearHistory: false);

            var info = _service.ReadStatus(penaltyDb);
            Assert.Equal(0, info.SaveScumLocation);
            Assert.False(info.IsPenaltyActive);
            Assert.Equal(2, info.StevenStrikes); // history preserved
        }
        finally
        {
            if (File.Exists(penaltyDb)) File.Delete(penaltyDb);
        }
    }

    [Fact]
    public void ResetPenalty_with_clear_history_should_remove_steven_trackers()
    {
        var penaltyDb = Path.Combine(Path.GetTempPath(), $"test_clear_{Guid.NewGuid()}.sav");
        try
        {
            CreateTestDatabase(penaltyDb, savescumLocation: 1, stevenStrikes: 3);

            _service.ResetPenalty(penaltyDb, clearHistory: true);

            var info = _service.ReadStatus(penaltyDb);
            Assert.Equal(0, info.SaveScumLocation);
            Assert.Equal(0, info.StevenStrikes);
        }
        finally
        {
            if (File.Exists(penaltyDb)) File.Delete(penaltyDb);
        }
    }

    [Fact]
    public void VerifyIntegrity_should_return_true_for_valid_db()
    {
        Assert.True(_service.VerifyIntegrity(_testDbPath));
    }

    [Fact]
    public void ReadStatus_should_throw_for_nonexistent_file()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _service.ReadStatus("/nonexistent/path.sav"));
    }

    [Fact]
    public void ReadStatus_should_handle_missing_keys_gracefully()
    {
        var emptyDb = Path.Combine(Path.GetTempPath(), $"test_empty_{Guid.NewGuid()}.sav");
        try
        {
            using var conn = new SqliteConnection($"Data Source={emptyDb}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE properties (key TEXT PRIMARY KEY, data ANY) STRICT;";
            cmd.ExecuteNonQuery();

            var info = _service.ReadStatus(emptyDb);
            Assert.Equal(0, info.SaveScumLocation);
            Assert.Equal(0, info.CurrentDay);
            Assert.False(info.OnAdventure);
        }
        finally
        {
            if (File.Exists(emptyDb)) File.Delete(emptyDb);
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet test --verbosity normal
```
Expected: FAIL — `SaveFileService` class does not exist yet

**Step 3: Implement SaveFileService**

Create `src/Services/SaveFileService.cs`:
```csharp
using Microsoft.Data.Sqlite;
using MewgenicsSaveGuardian.Models;

namespace MewgenicsSaveGuardian.Services;

public class SaveFileService
{
    public SaveFileInfo ReadStatus(string savePath)
    {
        if (!File.Exists(savePath))
            throw new FileNotFoundException("Save file not found.", savePath);

        var fileInfo = new FileInfo(savePath);

        using var conn = new SqliteConnection($"Data Source={savePath};Mode=ReadOnly");
        conn.Open();

        return new SaveFileInfo
        {
            SaveScumLocation = ReadInt(conn, "savescumlocation"),
            OnAdventure = ReadInt(conn, "on_adventure") == 1,
            CurrentDay = ReadInt(conn, "current_day"),
            StevenMet = ReadInt(conn, "PlotFlag_StevenMet") == 1,
            StevenStrikes = CountStevenStrikes(conn),
            AdventureStarted = ReadInt(conn, "adventure_started") == 1,
            LastModified = fileInfo.LastWriteTime,
            FileSizeBytes = fileInfo.Length,
        };
    }

    public void ResetPenalty(string savePath, bool clearHistory)
    {
        if (!File.Exists(savePath))
            throw new FileNotFoundException("Save file not found.", savePath);

        using var conn = new SqliteConnection($"Data Source={savePath}");
        conn.Open();

        using var transaction = conn.BeginTransaction();
        try
        {
            using var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = "UPDATE properties SET data = 0 WHERE key = 'savescumlocation'";
            updateCmd.ExecuteNonQuery();

            if (clearHistory)
            {
                using var deleteCmd = conn.CreateCommand();
                deleteCmd.CommandText = "DELETE FROM properties WHERE key LIKE 'NPCRSTRACKER_steven_savescum_%'";
                deleteCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool VerifyIntegrity(string savePath)
    {
        if (!File.Exists(savePath))
            return false;

        using var conn = new SqliteConnection($"Data Source={savePath};Mode=ReadOnly");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA integrity_check";
        var result = cmd.ExecuteScalar()?.ToString();
        return result == "ok";
    }

    private static int ReadInt(SqliteConnection conn, string key)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT data FROM properties WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);
        var result = cmd.ExecuteScalar();
        if (result is null or DBNull)
            return 0;
        return Convert.ToInt32(result);
    }

    private static int CountStevenStrikes(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM properties WHERE key LIKE 'NPCRSTRACKER_steven_savescum_%'";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
```

**Step 4: Run tests to verify they pass**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet test --verbosity normal
```
Expected: All 7 tests pass.

**Step 5: Commit**

```bash
git add src/Services/SaveFileService.cs tests/MewgenicsSaveGuardian.Tests/SaveFileServiceTests.cs
git commit -m "feat: implement SaveFileService with TDD tests"
```

---

### Task 4: Implement BackupService with Tests (TDD)

**Files:**
- Create: `src/Services/BackupService.cs`
- Create: `tests/MewgenicsSaveGuardian.Tests/BackupServiceTests.cs`

**Step 1: Write the failing tests**

Create `tests/MewgenicsSaveGuardian.Tests/BackupServiceTests.cs`:
```csharp
using Microsoft.Data.Sqlite;
using MewgenicsSaveGuardian.Services;

namespace MewgenicsSaveGuardian.Tests;

public class BackupServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testSavePath;
    private readonly BackupService _service;

    public BackupServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"guardian_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        var savesDir = Path.Combine(_testDir, "saves");
        Directory.CreateDirectory(savesDir);
        _testSavePath = Path.Combine(savesDir, "steamcampaign01.sav");
        CreateTestSave(_testSavePath);
        _service = new BackupService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    private static void CreateTestSave(string path, int day = 5, int flag = 0)
    {
        using var conn = new SqliteConnection($"Data Source={path}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE properties (key TEXT PRIMARY KEY, data ANY) STRICT;";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO properties (key, data) VALUES ('current_day', @d)";
        cmd.Parameters.AddWithValue("@d", day);
        cmd.ExecuteNonQuery();
        cmd.Parameters.Clear();
        cmd.CommandText = "INSERT INTO properties (key, data) VALUES ('savescumlocation', @f)";
        cmd.Parameters.AddWithValue("@f", flag);
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void CreateBackup_should_copy_save_file()
    {
        var entry = _service.CreateBackup(_testSavePath);

        Assert.True(File.Exists(entry.FilePath));
        Assert.Contains("guardian_backup_", entry.FileName);
        Assert.True(entry.FileSizeBytes > 0);
    }

    [Fact]
    public void CreateBackup_should_create_backups_in_guardian_backups_dir()
    {
        var entry = _service.CreateBackup(_testSavePath);
        var expectedDir = Path.Combine(Path.GetDirectoryName(_testSavePath)!, "guardian_backups");
        Assert.StartsWith(expectedDir, entry.FilePath);
    }

    [Fact]
    public void GetBackups_should_return_sorted_by_date_desc()
    {
        _service.CreateBackup(_testSavePath);
        Thread.Sleep(50);
        _service.CreateBackup(_testSavePath);

        var backups = _service.GetBackups(_testSavePath);

        Assert.Equal(2, backups.Count);
        Assert.True(backups[0].Timestamp >= backups[1].Timestamp);
    }

    [Fact]
    public void RotateBackups_should_delete_oldest_when_exceeding_max()
    {
        for (int i = 0; i < 4; i++)
        {
            _service.CreateBackup(_testSavePath);
            Thread.Sleep(50);
        }

        _service.RotateBackups(_testSavePath, maxCount: 2);

        var backups = _service.GetBackups(_testSavePath);
        Assert.Equal(2, backups.Count);
    }

    [Fact]
    public void RestoreBackup_should_overwrite_save_with_backup()
    {
        var backup = _service.CreateBackup(_testSavePath);

        // Modify current save
        using (var conn = new SqliteConnection($"Data Source={_testSavePath}"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE properties SET data = 99 WHERE key = 'current_day'";
            cmd.ExecuteNonQuery();
        }

        _service.RestoreBackup(_testSavePath, backup);

        // Verify the save now matches the backup
        using var checkConn = new SqliteConnection($"Data Source={_testSavePath};Mode=ReadOnly");
        checkConn.Open();
        using var checkCmd = checkConn.CreateCommand();
        checkCmd.CommandText = "SELECT data FROM properties WHERE key = 'current_day'";
        var day = Convert.ToInt32(checkCmd.ExecuteScalar());
        Assert.Equal(5, day);
    }

    [Fact]
    public void RestoreBackup_should_create_safety_backup_first()
    {
        var backup = _service.CreateBackup(_testSavePath);
        var countBefore = _service.GetBackups(_testSavePath).Count;

        _service.RestoreBackup(_testSavePath, backup);

        var countAfter = _service.GetBackups(_testSavePath).Count;
        Assert.True(countAfter > countBefore);
    }
}
```

**Step 2: Run tests to verify they fail**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet test --verbosity normal
```
Expected: FAIL — `BackupService` does not exist

**Step 3: Implement BackupService**

Create `src/Services/BackupService.cs`:
```csharp
using Microsoft.Data.Sqlite;
using MewgenicsSaveGuardian.Models;

namespace MewgenicsSaveGuardian.Services;

public class BackupService
{
    private const string BackupDirName = "guardian_backups";
    private const string BackupPrefix = "guardian_backup_";

    public BackupEntry CreateBackup(string savePath)
    {
        if (!File.Exists(savePath))
            throw new FileNotFoundException("Save file not found.", savePath);

        var backupDir = GetBackupDir(savePath);
        Directory.CreateDirectory(backupDir);

        var timestamp = DateTime.Now;
        var fileName = $"{BackupPrefix}{timestamp:yyyyMMdd_HHmmss_fff}.sav";
        var backupPath = Path.Combine(backupDir, fileName);

        File.Copy(savePath, backupPath, overwrite: false);

        var (day, flag) = ReadBasicInfo(savePath);
        var fileInfo = new FileInfo(backupPath);

        return new BackupEntry
        {
            FilePath = backupPath,
            FileName = fileName,
            Timestamp = timestamp,
            CurrentDay = day,
            SaveScumFlag = flag,
            FileSizeBytes = fileInfo.Length,
        };
    }

    public void RestoreBackup(string savePath, BackupEntry backup)
    {
        if (!File.Exists(backup.FilePath))
            throw new FileNotFoundException("Backup file not found.", backup.FilePath);

        // Safety backup before overwriting
        CreateBackup(savePath);

        File.Copy(backup.FilePath, savePath, overwrite: true);
    }

    public List<BackupEntry> GetBackups(string savePath)
    {
        var backupDir = GetBackupDir(savePath);
        if (!Directory.Exists(backupDir))
            return [];

        return Directory.GetFiles(backupDir, $"{BackupPrefix}*.sav")
            .Select(path =>
            {
                var fileInfo = new FileInfo(path);
                var (day, flag) = ReadBasicInfo(path);
                return new BackupEntry
                {
                    FilePath = path,
                    FileName = fileInfo.Name,
                    Timestamp = fileInfo.CreationTime,
                    CurrentDay = day,
                    SaveScumFlag = flag,
                    FileSizeBytes = fileInfo.Length,
                };
            })
            .OrderByDescending(b => b.Timestamp)
            .ToList();
    }

    public void RotateBackups(string savePath, int maxCount)
    {
        var backups = GetBackups(savePath);
        if (backups.Count <= maxCount)
            return;

        foreach (var old in backups.Skip(maxCount))
        {
            File.Delete(old.FilePath);
        }
    }

    private static string GetBackupDir(string savePath)
    {
        return Path.Combine(Path.GetDirectoryName(savePath)!, BackupDirName);
    }

    private static (int day, int flag) ReadBasicInfo(string path)
    {
        try
        {
            using var conn = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
            conn.Open();
            return (ReadInt(conn, "current_day"), ReadInt(conn, "savescumlocation"));
        }
        catch
        {
            return (0, 0);
        }
    }

    private static int ReadInt(SqliteConnection conn, string key)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT data FROM properties WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);
        var result = cmd.ExecuteScalar();
        if (result is null or DBNull)
            return 0;
        return Convert.ToInt32(result);
    }
}
```

**Step 4: Run tests to verify they pass**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet test --verbosity normal
```
Expected: All 13 tests pass (7 SaveFile + 6 Backup).

**Step 5: Commit**

```bash
git add src/Services/BackupService.cs tests/MewgenicsSaveGuardian.Tests/BackupServiceTests.cs
git commit -m "feat: implement BackupService with TDD tests"
```

---

### Task 5: Implement ProcessService with Tests (TDD)

**Files:**
- Create: `src/Services/ProcessService.cs`
- Create: `tests/MewgenicsSaveGuardian.Tests/ProcessServiceTests.cs`

**Step 1: Write the failing tests**

Create `tests/MewgenicsSaveGuardian.Tests/ProcessServiceTests.cs`:
```csharp
using MewgenicsSaveGuardian.Services;

namespace MewgenicsSaveGuardian.Tests;

public class ProcessServiceTests
{
    private readonly ProcessService _service = new();

    [Fact]
    public void IsGameRunning_should_return_false_when_game_not_running()
    {
        Assert.False(_service.IsGameRunning());
    }

    [Fact]
    public void GetGameProcess_should_return_null_when_game_not_running()
    {
        Assert.Null(_service.GetGameProcess());
    }

    [Fact]
    public void LaunchGame_should_not_throw()
    {
        // We don't actually launch the game in tests, just verify the method exists
        // and accepts the right signature. Integration test only.
        Assert.NotNull((Action)_service.LaunchGame);
    }
}
```

**Step 2: Run tests to verify they fail**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet test --verbosity normal
```
Expected: FAIL — `ProcessService` does not exist

**Step 3: Implement ProcessService**

Create `src/Services/ProcessService.cs`:
```csharp
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MewgenicsSaveGuardian.Services;

public class ProcessService
{
    private const string GameProcessName = "Mewgenics";
    private const string SteamGameUrl = "steam://rungameid/686060";
    private const int GracefulCloseTimeoutMs = 10_000;

    public bool IsGameRunning()
    {
        return GetGameProcess() is not null;
    }

    public Process? GetGameProcess()
    {
        return Process.GetProcessesByName(GameProcessName).FirstOrDefault();
    }

    public bool CloseGame()
    {
        var process = GetGameProcess();
        if (process is null)
            return true;

        try
        {
            process.CloseMainWindow();
            if (process.WaitForExit(GracefulCloseTimeoutMs))
                return true;

            process.Kill();
            return process.WaitForExit(5_000);
        }
        catch (InvalidOperationException)
        {
            // Process already exited
            return true;
        }
    }

    public void LaunchGame()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = SteamGameUrl,
            UseShellExecute = true,
        });
    }
}
```

**Step 4: Run tests to verify they pass**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet test --verbosity normal
```
Expected: All 16 tests pass.

**Step 5: Commit**

```bash
git add src/Services/ProcessService.cs tests/MewgenicsSaveGuardian.Tests/ProcessServiceTests.cs
git commit -m "feat: implement ProcessService for game process management"
```

---

### Task 6: Implement SettingsService

**Files:**
- Create: `src/Services/SettingsService.cs`

**Step 1: Implement SettingsService**

Create `src/Services/SettingsService.cs`:
```csharp
using System.Text.Json;
using MewgenicsSaveGuardian.Models;

namespace MewgenicsSaveGuardian.Services;

public class SettingsService
{
    private const string SettingsFileName = "guardian_settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _settingsPath;

    public SettingsService()
    {
        var exeDir = AppContext.BaseDirectory;
        _settingsPath = Path.Combine(exeDir, SettingsFileName);
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            var settings = new AppSettings
            {
                SaveFilePath = AutoDetectSavePath() ?? string.Empty,
            };
            Save(settings);
            return settings;
        }

        var json = File.ReadAllText(_settingsPath);
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
               ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    public static string? AutoDetectSavePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var mewgenicsDir = Path.Combine(appData, "Glaiel Games", "Mewgenics");

        if (!Directory.Exists(mewgenicsDir))
            return null;

        var saveFiles = new List<string>();
        foreach (var steamIdDir in Directory.GetDirectories(mewgenicsDir))
        {
            var savesDir = Path.Combine(steamIdDir, "saves");
            if (!Directory.Exists(savesDir))
                continue;

            saveFiles.AddRange(
                Directory.GetFiles(savesDir, "steamcampaign*.sav"));
        }

        return saveFiles.Count == 1 ? saveFiles[0] : saveFiles.FirstOrDefault();
    }

    public static List<string> FindAllSavePaths()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var mewgenicsDir = Path.Combine(appData, "Glaiel Games", "Mewgenics");

        if (!Directory.Exists(mewgenicsDir))
            return [];

        var saveFiles = new List<string>();
        foreach (var steamIdDir in Directory.GetDirectories(mewgenicsDir))
        {
            // Skip backup copies
            var dirName = Path.GetFileName(steamIdDir);
            if (dirName.Contains("Copy") || dirName.Contains("bak") ||
                dirName == "MewgenicsSaveGuardian" || dirName.StartsWith("."))
                continue;

            var savesDir = Path.Combine(steamIdDir, "saves");
            if (!Directory.Exists(savesDir))
                continue;

            saveFiles.AddRange(
                Directory.GetFiles(savesDir, "steamcampaign*.sav"));
        }

        return saveFiles;
    }
}
```

**Step 2: Verify build**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet build
```
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/Services/SettingsService.cs
git commit -m "feat: implement SettingsService with auto-detect save path"
```

---

### Task 7: Implement StatusToColorConverter

**Files:**
- Create: `src/Converters/StatusToColorConverter.cs`

**Step 1: Implement converter**

Create `src/Converters/StatusToColorConverter.cs`:
```csharp
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MewgenicsSaveGuardian.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            bool b => b ? new SolidColorBrush(Color.FromRgb(76, 175, 80))    // green
                        : new SolidColorBrush(Color.FromRgb(158, 158, 158)), // grey
            int i when parameter?.ToString() == "penalty" =>
                i == 1 ? new SolidColorBrush(Color.FromRgb(244, 67, 54))     // red
                       : new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // green
            _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
```

**Step 2: Verify build**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet build
```
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/Converters/StatusToColorConverter.cs
git commit -m "feat: add StatusToColorConverter for UI status indicators"
```

---

### Task 8: Implement MainViewModel (MVVM Core)

**Files:**
- Create: `src/ViewModels/MainViewModel.cs`

**Step 1: Implement MainViewModel**

Create `src/ViewModels/MainViewModel.cs`:
```csharp
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MewgenicsSaveGuardian.Models;
using MewgenicsSaveGuardian.Services;

namespace MewgenicsSaveGuardian.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SaveFileService _saveFileService = new();
    private readonly BackupService _backupService = new();
    private readonly ProcessService _processService = new();
    private readonly SettingsService _settingsService = new();
    private readonly DispatcherTimer _pollTimer;

    [ObservableProperty] private string _saveFilePath = string.Empty;
    [ObservableProperty] private bool _isGameRunning;
    [ObservableProperty] private int _saveScumLocation;
    [ObservableProperty] private bool _stevenMet;
    [ObservableProperty] private int _stevenStrikes;
    [ObservableProperty] private int _currentDay;
    [ObservableProperty] private bool _onAdventure;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _clearStevenHistory;
    [ObservableProperty] private bool _autoRelaunchGame;
    [ObservableProperty] private int _maxBackups = 5;
    [ObservableProperty] private BackupEntry? _selectedBackup;

    public ObservableCollection<BackupEntry> Backups { get; } = [];

    public bool IsPenaltyActive => SaveScumLocation == 1;
    public bool HasSaveFile => !string.IsNullOrEmpty(SaveFilePath) && File.Exists(SaveFilePath);

    public MainViewModel()
    {
        var settings = _settingsService.Load();
        SaveFilePath = settings.SaveFilePath;
        MaxBackups = settings.MaxBackups;
        ClearStevenHistory = settings.ClearStevenHistory;
        AutoRelaunchGame = settings.AutoRelaunchGame;

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _pollTimer.Tick += (_, _) => PollStatus();
        _pollTimer.Start();

        if (HasSaveFile)
        {
            RefreshStatus();
            RefreshBackups();
        }
    }

    partial void OnSaveFilePathChanged(string value)
    {
        OnPropertyChanged(nameof(HasSaveFile));
        SaveSettings();
        if (HasSaveFile)
        {
            RefreshStatus();
            RefreshBackups();
        }
    }

    partial void OnSaveScumLocationChanged(int value)
    {
        OnPropertyChanged(nameof(IsPenaltyActive));
    }

    partial void OnMaxBackupsChanged(int value) => SaveSettings();
    partial void OnClearStevenHistoryChanged(bool value) => SaveSettings();
    partial void OnAutoRelaunchGameChanged(bool value) => SaveSettings();

    [RelayCommand]
    private void AutoFindPath()
    {
        var paths = SettingsService.FindAllSavePaths();
        if (paths.Count == 1)
        {
            SaveFilePath = paths[0];
            StatusMessage = "Save file found automatically.";
        }
        else if (paths.Count > 1)
        {
            // Use the first one; user can browse for others
            SaveFilePath = paths[0];
            StatusMessage = $"Found {paths.Count} save files. Using first one.";
        }
        else
        {
            StatusMessage = "No save files found. Please browse manually.";
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySave))]
    private async Task RemovePenaltyAsync()
    {
        if (IsGameRunning)
        {
            var result = MessageBox.Show(
                "Mewgenics is running. Close it first?",
                "Game Running",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsBusy = true;
                StatusMessage = "Closing game...";
                var closed = await Task.Run(() => _processService.CloseGame());
                if (!closed)
                {
                    StatusMessage = "Failed to close game. Please close it manually.";
                    IsBusy = false;
                    return;
                }
                // Wait a moment for file locks to release
                await Task.Delay(1000);
            }
            else
            {
                return;
            }
        }

        IsBusy = true;
        StatusMessage = "Creating backup...";

        try
        {
            await Task.Run(() =>
            {
                _backupService.CreateBackup(SaveFilePath);
                _backupService.RotateBackups(SaveFilePath, MaxBackups);
            });

            StatusMessage = "Removing penalty...";
            await Task.Run(() =>
            {
                _saveFileService.ResetPenalty(SaveFilePath, ClearStevenHistory);

                if (!_saveFileService.VerifyIntegrity(SaveFilePath))
                    throw new InvalidOperationException("Database integrity check failed after modification.");
            });

            RefreshStatus();
            RefreshBackups();
            StatusMessage = "Penalty removed successfully!";

            if (AutoRelaunchGame)
            {
                _processService.LaunchGame();
                StatusMessage = "Penalty removed. Game relaunching...";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void LaunchGame()
    {
        _processService.LaunchGame();
        StatusMessage = "Launching game via Steam...";
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        if (!HasSaveFile) return;

        IsBusy = true;
        StatusMessage = "Creating backup...";
        try
        {
            await Task.Run(() =>
            {
                _backupService.CreateBackup(SaveFilePath);
                _backupService.RotateBackups(SaveFilePath, MaxBackups);
            });
            RefreshBackups();
            StatusMessage = "Backup created.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backup error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        if (SelectedBackup is null) return;

        if (IsGameRunning)
        {
            MessageBox.Show("Please close Mewgenics before restoring.", "Game Running",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Restore backup from {SelectedBackup.Timestamp:yyyy-MM-dd HH:mm:ss}?\nA safety backup will be created first.",
            "Confirm Restore",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsBusy = true;
        StatusMessage = "Restoring backup...";
        try
        {
            await Task.Run(() => _backupService.RestoreBackup(SaveFilePath, SelectedBackup));
            RefreshStatus();
            RefreshBackups();
            StatusMessage = "Backup restored successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanModifySave() => HasSaveFile && !IsBusy;

    private void PollStatus()
    {
        var wasRunning = IsGameRunning;
        IsGameRunning = _processService.IsGameRunning();

        // Auto-refresh save status when game exits
        if (wasRunning && !IsGameRunning && HasSaveFile)
        {
            RefreshStatus();
        }

        RemovePenaltyCommand.NotifyCanExecuteChanged();
    }

    private void RefreshStatus()
    {
        if (!HasSaveFile) return;

        try
        {
            var info = _saveFileService.ReadStatus(SaveFilePath);
            SaveScumLocation = info.SaveScumLocation;
            StevenMet = info.StevenMet;
            StevenStrikes = info.StevenStrikes;
            CurrentDay = info.CurrentDay;
            OnAdventure = info.OnAdventure;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Read error: {ex.Message}";
        }
    }

    private void RefreshBackups()
    {
        if (!HasSaveFile) return;

        Backups.Clear();
        foreach (var b in _backupService.GetBackups(SaveFilePath))
            Backups.Add(b);
    }

    private void SaveSettings()
    {
        _settingsService.Save(new AppSettings
        {
            SaveFilePath = SaveFilePath,
            MaxBackups = MaxBackups,
            ClearStevenHistory = ClearStevenHistory,
            AutoRelaunchGame = AutoRelaunchGame,
        });
    }
}
```

**Step 2: Verify build**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet build
```
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/ViewModels/MainViewModel.cs
git commit -m "feat: implement MainViewModel with MVVM commands and polling"
```

---

### Task 9: Implement WPF UI - Styles

**Files:**
- Create: `src/Resources/Styles.xaml`

**Step 1: Create Styles.xaml resource dictionary**

Create `src/Resources/Styles.xaml`:
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Color Palette - Dark theme -->
    <Color x:Key="BgDark">#1E1E2E</Color>
    <Color x:Key="BgMedium">#2B2B3D</Color>
    <Color x:Key="BgLight">#363649</Color>
    <Color x:Key="AccentBlue">#7AA2F7</Color>
    <Color x:Key="AccentGreen">#9ECE6A</Color>
    <Color x:Key="AccentRed">#F7768E</Color>
    <Color x:Key="AccentYellow">#E0AF68</Color>
    <Color x:Key="TextPrimary">#C0CAF5</Color>
    <Color x:Key="TextSecondary">#A9B1D6</Color>
    <Color x:Key="TextMuted">#565F89</Color>
    <Color x:Key="Border">#414868</Color>

    <SolidColorBrush x:Key="BgDarkBrush" Color="{StaticResource BgDark}"/>
    <SolidColorBrush x:Key="BgMediumBrush" Color="{StaticResource BgMedium}"/>
    <SolidColorBrush x:Key="BgLightBrush" Color="{StaticResource BgLight}"/>
    <SolidColorBrush x:Key="AccentBlueBrush" Color="{StaticResource AccentBlue}"/>
    <SolidColorBrush x:Key="AccentGreenBrush" Color="{StaticResource AccentGreen}"/>
    <SolidColorBrush x:Key="AccentRedBrush" Color="{StaticResource AccentRed}"/>
    <SolidColorBrush x:Key="AccentYellowBrush" Color="{StaticResource AccentYellow}"/>
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimary}"/>
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondary}"/>
    <SolidColorBrush x:Key="TextMutedBrush" Color="{StaticResource TextMuted}"/>
    <SolidColorBrush x:Key="BorderBrush" Color="{StaticResource Border}"/>

    <!-- Button Style -->
    <Style x:Key="PrimaryButton" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource AccentBlueBrush}"/>
        <Setter Property="Foreground" Value="#FFFFFF"/>
        <Setter Property="FontSize" Value="13"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="border"
                            Background="{TemplateBinding Background}"
                            CornerRadius="6"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="border" Property="Opacity" Value="0.85"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="border" Property="Opacity" Value="0.7"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter TargetName="border" Property="Opacity" Value="0.4"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style x:Key="DangerButton" TargetType="Button" BasedOn="{StaticResource PrimaryButton}">
        <Setter Property="Background" Value="{StaticResource AccentRedBrush}"/>
    </Style>

    <Style x:Key="SecondaryButton" TargetType="Button" BasedOn="{StaticResource PrimaryButton}">
        <Setter Property="Background" Value="{StaticResource BgLightBrush}"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
    </Style>

    <!-- TextBox Style -->
    <Style x:Key="DarkTextBox" TargetType="TextBox">
        <Setter Property="Background" Value="{StaticResource BgDarkBrush}"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Padding" Value="8,6"/>
        <Setter Property="FontSize" Value="12"/>
        <Setter Property="CaretBrush" Value="{StaticResource TextPrimaryBrush}"/>
    </Style>

    <!-- Section Header -->
    <Style x:Key="SectionHeader" TargetType="TextBlock">
        <Setter Property="Foreground" Value="{StaticResource TextMutedBrush}"/>
        <Setter Property="FontSize" Value="11"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="TextTransform" Value="Uppercase" />
        <Setter Property="Margin" Value="0,16,0,8"/>
    </Style>

    <!-- Status Label -->
    <Style x:Key="StatusLabel" TargetType="TextBlock">
        <Setter Property="Foreground" Value="{StaticResource TextSecondaryBrush}"/>
        <Setter Property="FontSize" Value="13"/>
        <Setter Property="VerticalAlignment" Value="Center"/>
    </Style>

    <Style x:Key="StatusValue" TargetType="TextBlock">
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="FontSize" Value="13"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="VerticalAlignment" Value="Center"/>
    </Style>

    <!-- CheckBox Style -->
    <Style x:Key="DarkCheckBox" TargetType="CheckBox">
        <Setter Property="Foreground" Value="{StaticResource TextSecondaryBrush}"/>
        <Setter Property="FontSize" Value="12"/>
        <Setter Property="Margin" Value="0,4"/>
    </Style>

    <!-- DataGrid Style -->
    <Style x:Key="DarkDataGrid" TargetType="DataGrid">
        <Setter Property="Background" Value="{StaticResource BgDarkBrush}"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
        <Setter Property="RowBackground" Value="{StaticResource BgDarkBrush}"/>
        <Setter Property="AlternatingRowBackground" Value="{StaticResource BgMediumBrush}"/>
        <Setter Property="GridLinesVisibility" Value="None"/>
        <Setter Property="HeadersVisibility" Value="Column"/>
        <Setter Property="FontSize" Value="12"/>
        <Setter Property="RowHeight" Value="32"/>
    </Style>

</ResourceDictionary>
```

**Step 2: Verify build**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet build
```
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/Resources/Styles.xaml
git commit -m "feat: add dark theme WPF styles"
```

---

### Task 10: Implement MainWindow.xaml (UI Layout)

**Files:**
- Modify: `src/MainWindow.xaml` (replace default content)
- Modify: `src/MainWindow.xaml.cs` (add browse dialog)

**Step 1: Replace MainWindow.xaml with full UI**

Overwrite `src/MainWindow.xaml` with:
```xml
<Window x:Class="MewgenicsSaveGuardian.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:MewgenicsSaveGuardian.ViewModels"
        xmlns:conv="clr-namespace:MewgenicsSaveGuardian.Converters"
        Title="Mewgenics Save Guardian"
        Width="560" Height="720"
        MinWidth="480" MinHeight="600"
        Background="{StaticResource BgMediumBrush}"
        WindowStartupLocation="CenterScreen">

    <Window.DataContext>
        <vm:MainViewModel/>
    </Window.DataContext>

    <Window.Resources>
        <conv:StatusToColorConverter x:Key="StatusColorConverter"/>

        <BooleanToVisibilityConverter x:Key="BoolToVis"/>
    </Window.Resources>

    <ScrollViewer VerticalScrollBarVisibility="Auto" Padding="20">
        <StackPanel>

            <!-- Title Bar -->
            <TextBlock Text="Mewgenics Save Guardian"
                       FontSize="20" FontWeight="Bold"
                       Foreground="{StaticResource AccentBlueBrush}"
                       Margin="0,0,0,4"/>
            <TextBlock Text="Bypass Steven's save-scum penalty with one click"
                       Foreground="{StaticResource TextMutedBrush}"
                       FontSize="12" Margin="0,0,0,16"/>

            <!-- Save File Path Section -->
            <TextBlock Text="SAVE FILE" Style="{StaticResource SectionHeader}"/>
            <DockPanel Margin="0,0,0,4">
                <Button Content="Browse" DockPanel.Dock="Right"
                        Style="{StaticResource SecondaryButton}"
                        Click="OnBrowseClick" Margin="8,0,0,0"/>
                <TextBox Text="{Binding SaveFilePath, UpdateSourceTrigger=PropertyChanged}"
                         Style="{StaticResource DarkTextBox}"
                         IsReadOnly="True"/>
            </DockPanel>
            <Button Content="Auto Find" Command="{Binding AutoFindPathCommand}"
                    Style="{StaticResource SecondaryButton}"
                    HorizontalAlignment="Right" Margin="0,4,0,0"/>

            <!-- Status Section -->
            <TextBlock Text="STATUS" Style="{StaticResource SectionHeader}"/>
            <Border Background="{StaticResource BgDarkBrush}" CornerRadius="8"
                    Padding="16" Margin="0,0,0,8">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>

                    <TextBlock Grid.Row="0" Grid.Column="0" Text="Game Process:"
                               Style="{StaticResource StatusLabel}" Margin="0,0,16,6"/>
                    <TextBlock Grid.Row="0" Grid.Column="1"
                               Style="{StaticResource StatusValue}"
                               Foreground="{Binding IsGameRunning, Converter={StaticResource StatusColorConverter}}"
                               Margin="0,0,0,6">
                        <TextBlock.Text>
                            <Binding Path="IsGameRunning"
                                     StringFormat="{}{0}">
                                <Binding.Converter>
                                    <conv:StatusToColorConverter/>
                                </Binding.Converter>
                            </Binding>
                        </TextBlock.Text>
                    </TextBlock>

                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Save Scum Flag:"
                               Style="{StaticResource StatusLabel}" Margin="0,0,16,6"/>
                    <TextBlock Grid.Row="1" Grid.Column="1"
                               Style="{StaticResource StatusValue}"
                               Foreground="{Binding SaveScumLocation, Converter={StaticResource StatusColorConverter}, ConverterParameter=penalty}"
                               Margin="0,0,0,6">
                        <TextBlock.Text>
                            <MultiBinding StringFormat="{}{0} {1}">
                                <Binding Path="SaveScumLocation"/>
                                <Binding Path="IsPenaltyActive"
                                         Converter="{StaticResource BoolToVis}"
                                         ConverterParameter="penalty"/>
                            </MultiBinding>
                        </TextBlock.Text>
                    </TextBlock>

                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Steven Met:"
                               Style="{StaticResource StatusLabel}" Margin="0,0,16,6"/>
                    <TextBlock Grid.Row="2" Grid.Column="1"
                               Text="{Binding StevenMet, StringFormat='{}{0}'}"
                               Style="{StaticResource StatusValue}" Margin="0,0,0,6"/>

                    <TextBlock Grid.Row="3" Grid.Column="0" Text="Steven Strikes:"
                               Style="{StaticResource StatusLabel}" Margin="0,0,16,6"/>
                    <TextBlock Grid.Row="3" Grid.Column="1"
                               Text="{Binding StevenStrikes}"
                               Style="{StaticResource StatusValue}" Margin="0,0,0,6"/>

                    <TextBlock Grid.Row="4" Grid.Column="0" Text="Current Day:"
                               Style="{StaticResource StatusLabel}" Margin="0,0,16,6"/>
                    <TextBlock Grid.Row="4" Grid.Column="1"
                               Text="{Binding CurrentDay}"
                               Style="{StaticResource StatusValue}" Margin="0,0,0,6"/>

                    <TextBlock Grid.Row="5" Grid.Column="0" Text="On Adventure:"
                               Style="{StaticResource StatusLabel}" Margin="0,0,16,0"/>
                    <TextBlock Grid.Row="5" Grid.Column="1"
                               Text="{Binding OnAdventure}"
                               Style="{StaticResource StatusValue}"/>
                </Grid>
            </Border>

            <!-- Actions Section -->
            <TextBlock Text="ACTIONS" Style="{StaticResource SectionHeader}"/>
            <WrapPanel Margin="0,0,0,8">
                <Button Content="Remove Penalty" Command="{Binding RemovePenaltyCommand}"
                        Style="{StaticResource PrimaryButton}" Margin="0,0,8,0"
                        FontSize="15" Padding="24,10"/>
                <Button Content="Relaunch Game" Command="{Binding LaunchGameCommand}"
                        Style="{StaticResource SecondaryButton}" Margin="0,0,8,0"/>
            </WrapPanel>

            <CheckBox Content="Also clear Steven encounter history"
                      IsChecked="{Binding ClearStevenHistory}"
                      Style="{StaticResource DarkCheckBox}"/>
            <CheckBox Content="Auto-relaunch game after fix"
                      IsChecked="{Binding AutoRelaunchGame}"
                      Style="{StaticResource DarkCheckBox}"/>

            <!-- Status Bar -->
            <Border Background="{StaticResource BgDarkBrush}" CornerRadius="6"
                    Padding="12,8" Margin="0,12,0,0">
                <TextBlock Text="{Binding StatusMessage}"
                           Foreground="{StaticResource AccentYellowBrush}"
                           FontSize="12"/>
            </Border>

            <!-- Backups Section -->
            <TextBlock Text="BACKUPS" Style="{StaticResource SectionHeader}"/>
            <DataGrid ItemsSource="{Binding Backups}"
                      SelectedItem="{Binding SelectedBackup}"
                      Style="{StaticResource DarkDataGrid}"
                      AutoGenerateColumns="False"
                      IsReadOnly="True"
                      SelectionMode="Single"
                      Height="160" Margin="0,0,0,8">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="Timestamp" Width="*"
                                        Binding="{Binding Timestamp, StringFormat='yyyy-MM-dd HH:mm:ss'}"/>
                    <DataGridTextColumn Header="Day" Width="50"
                                        Binding="{Binding CurrentDay}"/>
                    <DataGridTextColumn Header="Flag" Width="50"
                                        Binding="{Binding SaveScumFlag}"/>
                    <DataGridTextColumn Header="Size" Width="70"
                                        Binding="{Binding FileSizeBytes, StringFormat='{}{0:N0} B'}"/>
                </DataGrid.Columns>
            </DataGrid>

            <WrapPanel Margin="0,0,0,8">
                <Button Content="Restore Selected" Command="{Binding RestoreBackupCommand}"
                        Style="{StaticResource SecondaryButton}" Margin="0,0,8,0"/>
                <Button Content="Create Backup Now" Command="{Binding CreateBackupCommand}"
                        Style="{StaticResource SecondaryButton}"/>
            </WrapPanel>

            <!-- Settings -->
            <TextBlock Text="SETTINGS" Style="{StaticResource SectionHeader}"/>
            <StackPanel Orientation="Horizontal" Margin="0,0,0,16">
                <TextBlock Text="Max backups:" Style="{StaticResource StatusLabel}" Margin="0,0,8,0"/>
                <ComboBox SelectedItem="{Binding MaxBackups}" Width="60"
                          Background="{StaticResource BgDarkBrush}"
                          Foreground="{StaticResource TextPrimaryBrush}">
                    <ComboBox.Items>
                        <sys:Int32 xmlns:sys="clr-namespace:System;assembly=mscorlib">3</sys:Int32>
                        <sys:Int32 xmlns:sys="clr-namespace:System;assembly=mscorlib">5</sys:Int32>
                        <sys:Int32 xmlns:sys="clr-namespace:System;assembly=mscorlib">10</sys:Int32>
                        <sys:Int32 xmlns:sys="clr-namespace:System;assembly=mscorlib">20</sys:Int32>
                    </ComboBox.Items>
                </ComboBox>
            </StackPanel>

        </StackPanel>
    </ScrollViewer>
</Window>
```

**Step 2: Replace MainWindow.xaml.cs**

Overwrite `src/MainWindow.xaml.cs` with:
```csharp
using System.Windows;
using Microsoft.Win32;
using MewgenicsSaveGuardian.ViewModels;

namespace MewgenicsSaveGuardian;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Mewgenics Save File",
            Filter = "Save files (*.sav)|*.sav|All files (*.*)|*.*",
            InitialDirectory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Glaiel Games", "Mewgenics"),
        };

        if (dialog.ShowDialog() == true && DataContext is MainViewModel vm)
        {
            vm.SaveFilePath = dialog.FileName;
        }
    }
}
```

**Step 3: Verify build**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet build
```
Expected: Build succeeded.

**Step 4: Commit**

```bash
git add src/MainWindow.xaml src/MainWindow.xaml.cs
git commit -m "feat: implement MainWindow UI with dark theme"
```

---

### Task 11: Wire Up App.xaml Resources

**Files:**
- Modify: `src/App.xaml` (add resource dictionary)

**Step 1: Update App.xaml to include Styles**

Overwrite `src/App.xaml` with:
```xml
<Application x:Class="MewgenicsSaveGuardian.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Styles.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

**Step 2: Verify build**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet build
```
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/App.xaml
git commit -m "chore: wire up Styles.xaml in App.xaml resources"
```

---

### Task 12: Final Build Verification and Publish Config

**Files:**
- Modify: `src/MewgenicsSaveGuardian.csproj` (add publish settings)

**Step 1: Update .csproj with publish configuration**

Add inside the `<PropertyGroup>` of `src/MewgenicsSaveGuardian.csproj`:
```xml
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<SelfContained>true</SelfContained>
<PublishSingleFile>true</PublishSingleFile>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<ApplicationIcon>Resources\icon.ico</ApplicationIcon>
```

Note: `ApplicationIcon` references `Resources\icon.ico` which can be added later. If the icon file doesn't exist, remove that line or create a placeholder.

**Step 2: Run full build**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet build
```
Expected: Build succeeded.

**Step 3: Run all tests**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet test --verbosity normal
```
Expected: All tests pass.

**Step 4: Test publish**

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet publish src/MewgenicsSaveGuardian.csproj -c Release
```
Expected: Publish succeeded. Single exe in `src/bin/Release/net9.0-windows/win-x64/publish/`

**Step 5: Commit**

```bash
git add src/MewgenicsSaveGuardian.csproj
git commit -m "chore: add publish configuration for single-file exe"
```

---

### Task 13: Final Polish - Fix any XAML/Build Issues

This is a cleanup task. After all prior tasks, run:

Run:
```bash
cd "C:/Users/Alex/AppData/Roaming/Glaiel Games/Mewgenics/MewgenicsSaveGuardian"
dotnet build 2>&1
dotnet test --verbosity normal 2>&1
```

Fix any remaining build errors or warnings. Common issues to watch for:
- `TextTransform` is not a valid WPF property (remove it from `SectionHeader` style in `Styles.xaml`)
- `sys:Int32` namespace might need `assembly=System.Runtime` instead of `mscorlib` on .NET 9
- Any XAML binding errors visible in build output

After fixing all issues:
```bash
git add -A
git commit -m "fix: resolve build warnings and XAML compatibility issues"
```

---

## Summary

| Task | Component | Files | Tests |
|------|-----------|-------|-------|
| 1 | Solution setup | 4 | 0 |
| 2 | Models | 3 | 0 |
| 3 | SaveFileService | 1 + 1 test | 7 |
| 4 | BackupService | 1 + 1 test | 6 |
| 5 | ProcessService | 1 + 1 test | 3 |
| 6 | SettingsService | 1 | 0 |
| 7 | Converter | 1 | 0 |
| 8 | MainViewModel | 1 | 0 |
| 9 | Styles.xaml | 1 | 0 |
| 10 | MainWindow UI | 2 | 0 |
| 11 | App.xaml wiring | 1 | 0 |
| 12 | Publish config | 1 | 0 |
| 13 | Final polish | varies | 0 |
| **Total** | | **~18 files** | **16 tests** |

**Dependencies:** Tasks 1 -> 2 -> (3,4,5,6 parallel) -> 7 -> 8 -> (9,10 parallel) -> 11 -> 12 -> 13
