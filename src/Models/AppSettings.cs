namespace MewgenicsSaveGuardian.Models;

public class AppSettings
{
    public string SaveFilePath { get; set; } = string.Empty;
    public int MaxBackups { get; set; } = 5;
    public bool AutoRelaunchGame { get; set; }
    public string GameExePath { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
}
