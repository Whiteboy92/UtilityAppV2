namespace UtilityAppV2.Models;

public class FileMoveResult
{
    public int TotalFiles { get; init; }
    public int MovedFiles { get; init; }

    /// <summary>Files the user chose to keep in the playlist (cover/duplicate-title collisions).</summary>
    public int SkippedFiles { get; init; }

    public TimeSpan Duration { get; init; }

    /// <summary>Set when the operation was aborted by validation; null on success.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Non-fatal per-file move failures; the rest of the batch still moves.</summary>
    public IReadOnlyList<string> MoveErrors { get; init; } = [];

    /// <summary>Non-fatal tagging failures encountered after moving.</summary>
    public IReadOnlyList<string> TagErrors { get; init; } = [];

    public bool Aborted => ErrorMessage is not null;
}
