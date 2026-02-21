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
        SafeDelete(_testDbPath);
    }

    private static void SafeDelete(string filePath)
    {
        if (File.Exists(filePath))
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // File might be locked by SQLite, ignore
            }
        }
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
            SafeDelete(penaltyDb);
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
            Assert.Equal(1, info.StevenStrikes);
        }
        finally
        {
            SafeDelete(penaltyDb);
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
            SafeDelete(penaltyDb);
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
            conn.Close();

            var info = _service.ReadStatus(emptyDb);
            Assert.Equal(0, info.SaveScumLocation);
            Assert.Equal(0, info.CurrentDay);
            Assert.False(info.OnAdventure);
        }
        finally
        {
            SafeDelete(emptyDb);
        }
    }

    [Fact]
    public void ResetPenalty_should_cap_steven_strikes_to_one()
    {
        var penaltyDb = Path.Combine(Path.GetTempPath(), $"test_cap_{Guid.NewGuid()}.sav");
        try
        {
            CreateTestDatabase(penaltyDb, savescumLocation: 1, stevenStrikes: 5);

            _service.ResetPenalty(penaltyDb, clearHistory: false);

            var info = _service.ReadStatus(penaltyDb);
            Assert.Equal(0, info.SaveScumLocation);
            Assert.Equal(1, info.StevenStrikes);
        }
        finally
        {
            SafeDelete(penaltyDb);
        }
    }

    [Fact]
    public void ResetPenalty_should_not_change_single_strike()
    {
        var penaltyDb = Path.Combine(Path.GetTempPath(), $"test_single_{Guid.NewGuid()}.sav");
        try
        {
            CreateTestDatabase(penaltyDb, savescumLocation: 1, stevenStrikes: 1);

            _service.ResetPenalty(penaltyDb, clearHistory: false);

            var info = _service.ReadStatus(penaltyDb);
            Assert.Equal(0, info.SaveScumLocation);
            Assert.Equal(1, info.StevenStrikes);
        }
        finally
        {
            SafeDelete(penaltyDb);
        }
    }
}
