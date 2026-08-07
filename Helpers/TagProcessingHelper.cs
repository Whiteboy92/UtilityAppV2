using System.IO;

namespace UtilityAppV2.Helpers
{
    public static class TagProcessingHelper
    {
        private const string AlbumName = "MyMusic";

        private static readonly string[] AudioExtensions = [".mp3", ".flac", ".m4a", ".wav"];

        /// <summary>
        /// Writes Title/Artist tags (and a sequential track number) for the supplied audio files.
        /// Returns any non-fatal failures so the caller can surface them on the UI thread.
        /// </summary>
        public static async Task<IReadOnlyList<string>> ProcessTagsAsync(
            IEnumerable<string> files,
            CancellationToken cancellationToken)
        {
            var audioFiles = files
                .Where(IsAudioFile)
                .Select(ParseSongAndArtist)
                .OrderBy(x => x.ArtistName)
                .ThenBy(x => x.SongName)
                .ToList();

            if (audioFiles.Count == 0)
                return [];

            var errors = new List<string>();

            await Task.Run(() =>
            {
                int index = 0;
                foreach (var item in audioFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    uint trackNumber = (uint)Math.Min(index + 1, 9999);

                    try
                    {
                        using var tfile = TagLib.File.Create(item.Path);
                        var tag = tfile.Tag;

                        bool changed = false;

                        if (!string.IsNullOrWhiteSpace(item.SongName) && tag.Title != item.SongName)
                        {
                            tag.Title = item.SongName;
                            changed = true;
                        }

                        if (!string.IsNullOrWhiteSpace(item.ArtistName) &&
                            (tag.Performers.Length != 1 || tag.Performers[0] != item.ArtistName))
                        {
                            tag.Performers = [item.ArtistName];
                            changed = true;
                        }

                        if (tag.Album != AlbumName)
                        {
                            tag.Album = AlbumName;
                            changed = true;
                        }

                        if (tag.Track != trackNumber)
                        {
                            tag.Track = trackNumber;
                            changed = true;
                        }

                        // Only touch the disk when something actually differs — a
                        // library-wide re-tag on every move would otherwise rewrite
                        // thousands of already-correct files.
                        if (changed)
                            tfile.Save();
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{item.Path}: {ex.Message}");
                    }

                    index++;
                }
            }, cancellationToken);

            return errors;
        }

        private static bool IsAudioFile(string path) =>
            AudioExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

        private static (string Path, string SongName, string ArtistName) ParseSongAndArtist(string path)
        {
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
            var (song, artist) = FileNameParser.SplitSongArtist(fileNameWithoutExt);
            return (path, song, artist);
        }
    }
}
