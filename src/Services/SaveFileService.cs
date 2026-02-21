using System.IO;
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

        SaveFileInfo result;
        using (var conn = new SqliteConnection($"Data Source={savePath};Mode=ReadOnly"))
        {
            conn.Open();

            result = new SaveFileInfo
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
            conn.Close();
        }

        return result;
    }

    public void ResetPenalty(string savePath, bool clearHistory, bool capStrikes = true)
    {
        if (!File.Exists(savePath))
            throw new FileNotFoundException("Save file not found.", savePath);

        using (var conn = new SqliteConnection($"Data Source={savePath}"))
        {
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
                else if (capStrikes)
                {
                    // Cap steven strikes to 1 (keep only the first record)
                    using var capCmd = conn.CreateCommand();
                    capCmd.CommandText = @"
                        DELETE FROM properties
                        WHERE key LIKE 'NPCRSTRACKER_steven_savescum_%'
                        AND key NOT IN (
                            SELECT key FROM properties
                            WHERE key LIKE 'NPCRSTRACKER_steven_savescum_%'
                            ORDER BY key ASC
                            LIMIT 1
                        )";
                    capCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                conn.Close();
            }
        }
    }

    public bool VerifyIntegrity(string savePath)
    {
        if (!File.Exists(savePath))
            return false;

        using (var conn = new SqliteConnection($"Data Source={savePath};Mode=ReadOnly"))
        {
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check";
            var result = cmd.ExecuteScalar()?.ToString();
            conn.Close();
            return result == "ok";
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

    private static int CountStevenStrikes(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM properties WHERE key LIKE 'NPCRSTRACKER_steven_savescum_%'";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
