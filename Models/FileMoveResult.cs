namespace UtilityAppV2.Models;

public class FileMoveResult
{
    public int TotalFiles { get; init; }
    public int MovedFiles { get; init; }
    public TimeSpan Duration { get; init; }
}