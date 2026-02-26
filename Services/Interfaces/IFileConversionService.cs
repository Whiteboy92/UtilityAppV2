using UtilityAppV2.Models;

namespace UtilityAppV2.Services.Interfaces;

public interface IFileConversionService
{
    Task<ConversionResult> ConvertFilesToPngAsync(
        string sourceFolder,
        IProgress<double> progress,
        CancellationToken cancellationToken = default);
}