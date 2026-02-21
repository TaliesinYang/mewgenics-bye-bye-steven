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
