namespace UtilityAppV2.Models;

public class ConversionResult
{
    public int TotalFiles { get; set; }
    public int ConvertedFiles { get; set; }
    public TimeSpan Duration { get; set; }

    /// <summary>Per-file conversion failures (name + reason); empty on full success.</summary>
    public IReadOnlyList<string> FailedFiles { get; set; } = [];
}