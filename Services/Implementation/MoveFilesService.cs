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
            var movedFiles = await Task.Run(
                () => MoveFilesCore(files, destinationFolder, progress, cancellationToken),
                cancellationToken);

            // 6. Re-tag the whole destination so track numbers stay unique and
            //    sequential across the entire library (not just this batch).
            var destinationFiles = FileSystemHelper.GetAllFilesFromFolder(destinationFolder);
            var tagErrors = await TagProcessingHelper.ProcessTagsAsync(destinationFiles, cancellationToken);

            return new FileMoveResult
            {
                TotalFiles = totalFiles,
                MovedFiles = movedFiles.Count,
                Duration = DateTime.UtcNow - startTime,
                TagErrors = tagErrors,
            };
        }

        private static List<string> MoveFilesCore(
            string[] files,
            string destinationFolder,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            int totalFiles = files.Length;
            var moved = new List<string>(totalFiles);

            for (int i = 0; i < totalFiles; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string sourceFile = files[i];
                string destinationFile = Path.Combine(destinationFolder, Path.GetFileName(sourceFile));

                File.Move(sourceFile, destinationFile, overwrite: false);
                moved.Add(destinationFile);

                progress?.Report((moved.Count * 100.0) / totalFiles);
            }

            return moved;
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
