using System.IO;
using UtilityAppV2.Core;
using UtilityAppV2.Helpers;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.Services.Implementation
{
    public class MoveFilesService(IArtistRepository artistRepository) : IMoveFilesService
    {
        private readonly IArtistRepository artistRepository =
            artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));

        public async Task<FileMoveResult> MoveFilesAsync(
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            var sourceFolder = FilePathConstants.PlaylistFolder;
            var destinationFolder = FilePathConstants.MusicDbFolder;

            var startTime = DateTime.UtcNow;

            // 1. Validate source folder
            if (!ValidationHelper.ValidateSourceFolder(sourceFolder, out string? sourceError))
                return Aborted(0, startTime, sourceError);

            Directory.CreateDirectory(destinationFolder);

            // 2. Get source files
            string[] files = FileSystemHelper
                .GetAllFilesFromFolder(sourceFolder)
                .ToArray();

            int totalFiles = files.Length;

            if (totalFiles == 0)
            {
                return new FileMoveResult
                {
                    TotalFiles = 0,
                    MovedFiles = 0,
                    Duration = DateTime.UtcNow - startTime,
                };
            }

            // 3. Validate names: Unknown Artist
            if (!ValidationHelper.EnsureNoUnknownArtist(files, out string? unknownError))
                return Aborted(totalFiles, startTime, unknownError);

            // 4. Validate duplicates (in source and against destination)
            if (!ValidationHelper.EnsureNoDuplicates(files, destinationFolder, out string? duplicateError))
                return Aborted(totalFiles, startTime, duplicateError);

            // 5. Move files (off the UI thread)
            var (movedFiles, moveErrors) = await Task.Run(
                () => MoveFilesCore(files, destinationFolder, progress, cancellationToken),
                cancellationToken);

            // New files landed in the DB folder, so the cached artist list is now
            // stale — drop it so the next scan picks up the newly added artists.
            artistRepository.InvalidateCache();

            // 6. Re-tag the whole destination so track numbers stay unique and
            //    sequential across the entire library (not just this batch).
            var destinationFiles = FileSystemHelper.GetAllFilesFromFolder(destinationFolder);
            var tagErrors = await TagProcessingHelper.ProcessTagsAsync(destinationFiles, cancellationToken);

            return new FileMoveResult
            {
                TotalFiles = totalFiles,
                MovedFiles = movedFiles.Count,
                Duration = DateTime.UtcNow - startTime,
                MoveErrors = moveErrors,
                TagErrors = tagErrors,
            };
        }

        private static (List<string> Moved, List<string> Errors) MoveFilesCore(
            string[] files,
            string destinationFolder,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            int totalFiles = files.Length;
            var moved = new List<string>(totalFiles);
            var errors = new List<string>();

            for (int i = 0; i < totalFiles; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string sourceFile = files[i];
                string destinationFile = Path.Combine(destinationFolder, Path.GetFileName(sourceFile));

                try
                {
                    File.Move(sourceFile, destinationFile, overwrite: false);
                    moved.Add(destinationFile);
                }
                catch (Exception ex)
                {
                    // A single unmovable file (locked, permission, race) must not
                    // abort the batch and skip the downstream tagging step.
                    errors.Add($"{Path.GetFileName(sourceFile)}: {ex.Message}");
                }

                progress?.Report(((i + 1) * 100.0) / totalFiles);
            }

            return (moved, errors);
        }

        private static FileMoveResult Aborted(int totalFiles, DateTime startTime, string? error) =>
            new()
            {
                TotalFiles = totalFiles,
                MovedFiles = 0,
                Duration = DateTime.UtcNow - startTime,
                ErrorMessage = error,
            };
    }
}
