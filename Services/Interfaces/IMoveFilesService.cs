using UtilityAppV2.Models;

namespace UtilityAppV2.Services.Interfaces
{
    public interface IMoveFilesService
    {
        Task<FileMoveResult> MoveFilesAsync(
            IProgress<double>? progress,
            CancellationToken cancellationToken);
    }
}