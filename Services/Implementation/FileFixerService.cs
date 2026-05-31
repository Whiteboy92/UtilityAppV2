using System.Globalization;
using System.IO;
using UtilityAppV2.Core;
using UtilityAppV2.Helpers;
using UtilityAppV2.Models;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.Services.Implementation;

public class FileFixerService(
    IRegexFileNameCleaner regexFileNameCleaner,
    IArtistRepository artistRepository)
    : IFileFixerService
{
    private readonly IRegexFileNameCleaner regexFileNameCleaner = regexFileNameCleaner ?? throw new ArgumentNullException(nameof(regexFileNameCleaner));
    private readonly IArtistRepository artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
    private readonly TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

    public async Task<FileFixResult> FixFilesAsync(
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        string playlistFolder = FilePathConstants.PlaylistFolder;
        string dbFolder = FilePathConstants.MusicDbFolder;

        FileSystemHelper.EnsureFoldersExist(playlistFolder, dbFolder);

        var artistList = await artistRepository.LoadArtistsAsync(cancellationToken);

        var playlistFiles = FileSystemHelper
            .GetAllFilesFromFolder(playlistFolder)
            .ToList();

        int total = playlistFiles.Count;

        // Run file I/O off the UI thread to keep the app responsive.
        var (processed, renamed) = await Task.Run(
            () => FixCore(playlistFiles, artistList, progress, cancellationToken),
            cancellationToken);

        stopwatch.Stop();

        return new FileFixResult
        {
            TotalFiles = total,
            ProcessedFiles = processed,
            RenamedFiles = renamed,
            Duration = stopwatch.Elapsed,
        };
    }

    private (int Processed, int Renamed) FixCore(
        List<string> playlistFiles,
        IReadOnlyList<string> artistList,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        int total = playlistFiles.Count;
        int processed = 0;
        int renamed = 0;

        foreach (var file in playlistFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string newPath = GetRenamedFilePath(file, artistList);

                if (!string.Equals(file, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    newPath = FileSystemHelper.GetUniqueFilePath(newPath);
                    File.Move(file, newPath);
                    renamed++;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // skip problematic files silently
            }
            finally
            {
                processed++;
                progress?.Report(total > 0 ? (processed * 100.0) / total : 100.0);
            }
        }

        return (processed, renamed);
    }

    private string GetRenamedFilePath(string filePath, IReadOnlyList<string> artistList)
    {
        string ext = Path.GetExtension(filePath);
        string baseName = Path.GetFileNameWithoutExtension(filePath);

        string cleaned = regexFileNameCleaner.CleanFileName(baseName);

        (string song, string artist) = SetCorrectFileNamePattern(cleaned, artistList);

        song = textInfo.ToTitleCase(song.ToLower());
        artist = textInfo.ToTitleCase(artist.ToLower());

        string newBaseName = FileSystemHelper.RemoveInvalidFileNameChars($"{song} - {artist}");
        string directory = Path.GetDirectoryName(filePath)!;

        return Path.Combine(directory, newBaseName + ext);
    }

    private (string Song, string Artist) SetCorrectFileNamePattern(string cleaned, IReadOnlyList<string> artistList)
    {
        var dashParts = cleaned
            .Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToArray();

        if (dashParts.Length >= 2)
        {
            if (artistRepository.ArtistExists(dashParts[0], artistList))
                return (dashParts[1], dashParts[0]);

            return artistRepository.ArtistExists(dashParts[1], artistList)
                ? (dashParts[0], dashParts[1])
                : (dashParts[1], dashParts[0]);
        }

        var parts = regexFileNameCleaner.ParseParts(cleaned);

        bool artistNonEmpty = !string.IsNullOrWhiteSpace(parts.Artist);
        bool artistKnown = artistNonEmpty && artistRepository.ArtistExists(parts.Artist, artistList);

        if (artistKnown || artistNonEmpty)
            return (parts.Song, parts.Artist);

        return (cleaned, "Unknown Artist");
    }
}
