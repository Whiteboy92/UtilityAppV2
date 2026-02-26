using System.IO;
using UtilityAppV2.Core;
using UtilityAppV2.Helpers;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.Services.Implementation
{
    public class MoveFilesService : IMoveFilesService
    {
        public async Task<FileMoveResult> MoveFilesAsync(
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            var sourceFolder = FilePathConstants.PlaylistFolder;
            var destinationFolder = FilePathConstants.MusicDbFolder;

            var startTime = DateTime.UtcNow;

            // 1. Validate source folder
            if (!ValidationHelper.ValidateSourceFolder(sourceFolder))
            {
                return new FileMoveResult
                {
                    TotalFiles = 0,
                    MovedFiles = 0,
                    Duration = TimeSpan.Zero,
                };
            }

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
            if (!ValidationHelper.EnsureNoUnknownArtist(files, totalFiles, startTime, out FileMoveResult? unknownArtistResult))
            {
                return unknownArtistResult ?? throw new InvalidOperationException();
            }

            // 4. Validate duplicates (in source and against destination)
            if (!ValidationHelper.EnsureNoDuplicates(files, destinationFolder, totalFiles, startTime, out var duplicatesResult))
            {
                return duplicatesResult ?? throw new InvalidOperationException();
            }

            // 5. Move files
            int movedFiles = await MoveFilesCoreAsync(files, destinationFolder, progress, cancellationToken);

            // 6. Tag all audio files in destination
            await TagProcessingHelper.ProcessTagsAsync(destinationFolder, cancellationToken);

            var duration = DateTime.UtcNow - startTime;

            return new FileMoveResult
            {
                TotalFiles = totalFiles,
                MovedFiles = movedFiles,
                Duration = duration,
            };
        }

        private async Task<int> MoveFilesCoreAsync(
            string[] files,
            string destinationFolder,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            int totalFiles = files.Length;
            int movedFiles = 0;

            for (int i = 0; i < totalFiles; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string sourceFile = files[i];
                string fileName = Path.GetFileName(sourceFile);
                string destinationFile = Path.Combine(destinationFolder, fileName);

                File.Move(sourceFile, destinationFile, overwrite: false);
                movedFiles++;

                progress?.Report((movedFiles * 100.0) / totalFiles);

                await Task.Yield();
            }

            return movedFiles;
        }
    }
}
