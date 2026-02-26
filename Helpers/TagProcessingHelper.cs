using System.IO;
using System.Windows;

namespace UtilityAppV2.Helpers
{
    public static class TagProcessingHelper
    {
        public static async Task ProcessTagsAsync(
            string destinationFolder,
            CancellationToken cancellationToken)
        {
            var allFiles = GetAudioFiles(destinationFolder);

            if (allFiles.Count == 0)
            {
                return;
            }

            var parsed = allFiles
                .Select(ParseSongAndArtist)
                .OrderBy(x => x.ArtistName)
                .ThenBy(x => x.SongName)
                .ToList();

            int index = 0;

            foreach (var item in parsed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                uint trackNumber = (uint)Math.Min(index + 1, 9999);

                try
                {
                    var tfile = TagLib.File.Create(item.Path);

                    if (!string.IsNullOrWhiteSpace(item.SongName))
                    {
                        tfile.Tag.Title = item.SongName;
                    }

                    if (!string.IsNullOrWhiteSpace(item.ArtistName))
                    {
                        tfile.Tag.Performers = [item.ArtistName];
                    }

                    tfile.Tag.Track = trackNumber;

                    tfile.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to update tags for:\n{item.Path}\n\nError:\n{ex.Message}",
                        "Tag Update Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                index++;
                await Task.Yield();
            }
        }

        private static List<string> GetAudioFiles(string destinationFolder)
        {
            return Directory.GetFiles(destinationFolder, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f =>
                    f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".flac", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static (string Path, string SongName, string ArtistName) ParseSongAndArtist(string path)
        {
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);

            string[] parts = fileNameWithoutExt.Split([" - "], StringSplitOptions.None);

            string songName;
            string artistName;

            if (parts.Length >= 2)
            {
                artistName = parts[^1];
                songName = string.Join(" - ", parts.Take(parts.Length - 1));
            }
            else
            {
                songName = fileNameWithoutExt;
                artistName = string.Empty;
            }

            return (path, songName, artistName);
        }
    }
}
