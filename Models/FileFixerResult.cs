namespace UtilityAppV2.Models;

public class FileFixResult
{
    public int TotalFiles { get; init; }
    public int ProcessedFiles { get; init; }
    public int RenamedFiles { get; init; }
    public TimeSpan Duration { get; init; } = TimeSpan.Zero;
}