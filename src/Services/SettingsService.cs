using System.IO;
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
            // Skip backup copies and non-SteamID directories
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
