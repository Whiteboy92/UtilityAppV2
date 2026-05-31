namespace UtilityAppV2.Models;

public class AudioOptimizeResult
{
    public int TotalFiles { get; init; }
    public int OptimizedFiles { get; init; }
    public long BytesSaved { get; init; }
    public TimeSpan Duration { get; init; }
}
