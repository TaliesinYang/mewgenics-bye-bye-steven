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
