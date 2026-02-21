namespace MewgenicsSaveGuardian.Models;

public class AppSettings
{
    public string SaveFilePath { get; set; } = string.Empty;
    public int MaxBackups { get; set; } = 5;
    public bool AutoRelaunchGame { get; set; }
    public bool ClearStevenHistory { get; set; }
}
