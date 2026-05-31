namespace UtilityAppV2.Models;

public class FileMoveResult
{
    public int TotalFiles { get; init; }
    public int MovedFiles { get; init; }
    public TimeSpan Duration { get; init; }

    /// <summary>Set when the operation was aborted by validation; null on success.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Non-fatal tagging failures encountered after moving.</summary>
    public IReadOnlyList<string> TagErrors { get; init; } = [];

    public bool Aborted => ErrorMessage is not null;
}
