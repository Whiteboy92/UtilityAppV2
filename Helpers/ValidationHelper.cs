using System.IO;
using UtilityAppV2.Models;

namespace UtilityAppV2.Helpers
{
    /// <summary>
    /// Pure validation helpers. They never touch the UI — callers decide how to surface
    /// the returned message, which keeps the move pipeline testable and thread-safe.
    /// </summary>
    public static class ValidationHelper
    {
        public static bool ValidateSourceFolder(string sourceFolder, out string? error)
        {
            if (Directory.Exists(sourceFolder))
            {
                error = null;
                return true;
            }

            error = $"Source folder does not exist:\n{sourceFolder}";
            return false;
        }

        public static bool EnsureNoUnknownArtist(string[] files, out string? error)
        {
            var filesWithUnknownArtist = files
                .Where(f => Path.GetFileNameWithoutExtension(f)
                    .Contains("Unknown Artist", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filesWithUnknownArtist.Count == 0)
            {
                error = null;
                return true;
            }

            error = "Some files contain 'Unknown Artist' in their names.\n" +
                    "Please fix the filenames before moving.\n\n" +
                    "Examples:\n" +
                    string.Join(Environment.NewLine,
                        filesWithUnknownArtist.Take(5).Select(Path.GetFileName));

            return false;
        }

        public static bool EnsureNoDuplicates(
            string[] files,
            string destinationFolder,
            out string? error)
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
                .Where(existingDestFileNames.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (duplicateNewNames.Count == 0 && collidingExistingNames.Count == 0)
            {
                error = null;
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

            error = message;
            return false;
        }

        /// <summary>
        /// Finds incoming files whose song title already exists in the destination
        /// under a different file name (a different artist — typically a cover).
        /// Exact-name duplicates are handled by <see cref="EnsureNoDuplicates"/> and
        /// are never reported here. The caller decides per file whether to move.
        /// </summary>
        public static IReadOnlyList<CoverCollision> FindCoverCollisions(
            string[] files,
            string destinationFolder)
        {
            // First existing file name per title is enough to show the user.
            var existingByTitle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in Directory.GetFiles(destinationFolder, "*.*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(path);
                string title = FileNameParser.SplitSongArtist(Path.GetFileNameWithoutExtension(name)).Song;
                if (!string.IsNullOrWhiteSpace(title))
                    existingByTitle.TryAdd(title, name);
            }

            var collisions = new List<CoverCollision>();
            foreach (var incoming in files)
            {
                string incomingName = Path.GetFileName(incoming);
                string title = FileNameParser.SplitSongArtist(Path.GetFileNameWithoutExtension(incomingName)).Song;
                if (string.IsNullOrWhiteSpace(title))
                    continue;

                if (existingByTitle.TryGetValue(title, out var existingName)
                    && !string.Equals(existingName, incomingName, StringComparison.OrdinalIgnoreCase))
                {
                    collisions.Add(new CoverCollision(incoming, existingName));
                }
            }

            return collisions;
        }
    }
}
