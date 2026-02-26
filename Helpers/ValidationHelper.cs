using System.IO;
using System.Windows;
using UtilityAppV2.Models;

namespace UtilityAppV2.Helpers
{
    public static class ValidationHelper
    {
        public static bool ValidateSourceFolder(string sourceFolder)
        {
            if (Directory.Exists(sourceFolder))
            {
                return true;
            }

            MessageBox.Show(
                $"Source folder does not exist:\n{sourceFolder}",
                "Move Files Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return false;
        }

        public static bool EnsureNoUnknownArtist(
            string[] files,
            int totalFiles,
            DateTime startTime,
            out FileMoveResult? result)
        {
            var filesWithUnknownArtist = files
                .Where(f =>
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    return name.IndexOf("Unknown Artist", StringComparison.OrdinalIgnoreCase) >= 0;
                })
                .ToList();

            if (filesWithUnknownArtist.Count == 0)
            {
                result = null;
                return true;
            }

            string message = "Some files contain 'Unknown Artist' in their names.\n" +
                             "Please fix the filenames before moving.\n\n" +
                             "Examples:\n" +
                             string.Join(Environment.NewLine,
                                 filesWithUnknownArtist
                                     .Take(5)
                                     .Select(Path.GetFileName));

            MessageBox.Show(
                message,
                "Invalid File Names",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            result = new FileMoveResult
            {
                TotalFiles = totalFiles,
                MovedFiles = 0,
                Duration = DateTime.UtcNow - startTime,
            };

            return false;
        }

        public static bool EnsureNoDuplicates(
            string[] files,
            string destinationFolder,
            int totalFiles,
            DateTime startTime,
            out FileMoveResult? result)
        {
            var existingDestFileNames = Directory
                .GetFiles(destinationFolder, "*.*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => name != null)
                .Cast<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newDestFileNames = files
                .Select(Path.GetFileName)
                .Where(name => name != null)
                .Cast<string>()
                .ToList();

            var duplicateNewNames = newDestFileNames
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            var collidingExistingNames = newDestFileNames
                .Where(name => existingDestFileNames.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (duplicateNewNames.Count == 0 && collidingExistingNames.Count == 0)
            {
                result = null;
                return true;
            }

            string message = "Duplicate files detected. Move operation aborted.\n\n";

            if (duplicateNewNames.Count > 0)
            {
                message += "Duplicates within Playlist folder:\n" +
                           string.Join(Environment.NewLine, duplicateNewNames.Take(10)) +
                           "\n\n";
            }

            if (collidingExistingNames.Count > 0)
            {
                message += "Files that already exist in destination folder:\n" +
                           string.Join(Environment.NewLine, collidingExistingNames.Take(10)) +
                           "\n\n";
            }

            message += "Please resolve duplicates (rename or remove) before moving.";

            MessageBox.Show(
                message,
                "Duplicate Files Detected",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            result = new FileMoveResult
            {
                TotalFiles = totalFiles,
                MovedFiles = 0,
                Duration = DateTime.UtcNow - startTime,
            };

            return false;
        }
    }
}
