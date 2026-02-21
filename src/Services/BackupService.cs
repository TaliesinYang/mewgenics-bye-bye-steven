using System.IO;
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

        SqliteConnection.ClearAllPools();

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

        SqliteConnection.ClearAllPools();
    }

    public List<BackupEntry> GetBackups(string savePath)
    {
        var backupDir = GetBackupDir(savePath);
        if (!Directory.Exists(backupDir))
            return [];

        var backups = Directory.GetFiles(backupDir, $"{BackupPrefix}*.sav")
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

        SqliteConnection.ClearAllPools();
        return backups;
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
            using (var conn = new SqliteConnection($"Data Source={path};Mode=ReadOnly"))
            {
                conn.Open();
                var day = ReadInt(conn, "current_day");
                var flag = ReadInt(conn, "savescumlocation");
                conn.Close();
                return (day, flag);
            }
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
