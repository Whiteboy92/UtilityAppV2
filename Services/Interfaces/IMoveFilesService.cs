using UtilityAppV2.Models;

namespace UtilityAppV2.Services.Interfaces
{
    public interface IMoveFilesService
    {
        /// <param name="confirmCoverCollision">
        /// Called once per incoming file whose title already exists in the library
        /// under a different name (likely a cover). Return true to move it, false to
        /// leave it in the playlist. When null, all such files are moved.
        /// </param>
        Task<FileMoveResult> MoveFilesAsync(
            IProgress<double>? progress,
            CancellationToken cancellationToken,
            Func<CoverCollision, bool>? confirmCoverCollision = null);
    }
}