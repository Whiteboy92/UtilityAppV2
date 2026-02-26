namespace UtilityAppV2.Models;

public class ConversionResult
{
    public int TotalFiles { get; set; }
    public int ConvertedFiles { get; set; }
    public TimeSpan Duration { get; set; }
}