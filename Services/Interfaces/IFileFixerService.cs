using UtilityAppV2.Models;

namespace UtilityAppV2.Services.Interfaces;

public interface IFileFixerService
{
    Task<FileFixResult> FixFilesAsync(IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}