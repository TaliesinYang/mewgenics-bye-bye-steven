using Microsoft.Data.Sqlite;
using MewgenicsSaveGuardian.Services;
using System.Threading;

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
        // SQLite may hold file locks briefly, so wait for them to be released
        System.Threading.Thread.Sleep(100);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        System.Threading.Thread.Sleep(100);

        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    private static void CreateTestSave(string path, int day = 5, int flag = 0)
    {
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
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
            conn.Close();
        }
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
            conn.Close();
        }

        // Wait to ensure the file is not locked
        System.Threading.Thread.Sleep(200);
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        System.Threading.Thread.Sleep(200);

        _service.RestoreBackup(_testSavePath, backup);

        // Verify the save now matches the backup
        using (var checkConn = new SqliteConnection($"Data Source={_testSavePath};Mode=ReadOnly"))
        {
            checkConn.Open();
            using var checkCmd = checkConn.CreateCommand();
            checkCmd.CommandText = "SELECT data FROM properties WHERE key = 'current_day'";
            var day = Convert.ToInt32(checkCmd.ExecuteScalar());
            Assert.Equal(5, day);
            checkConn.Close();
        }

        SqliteConnection.ClearAllPools();
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
