using System.IO;
using UtilityAppV2.Core;
using UtilityAppV2.Helpers;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.Services.Implementation;

public class ArtistRepository : IArtistRepository
{
    private readonly Lock syncRoot = new();
    private IReadOnlyList<string>? cachedArtists;

    public async Task<IReadOnlyList<string>> LoadArtistsAsync(CancellationToken cancellationToken = default)
    {
        var cached = Volatile.Read(ref cachedArtists);
        if (cached is not null)
            return cached;

        // Scan the disk off the lock so concurrent callers never block on I/O.
        var scanned = await Task.Run(() => ScanArtists(cancellationToken), cancellationToken);

        lock (syncRoot)
        {
            cachedArtists ??= scanned;
            return cachedArtists;
        }
    }

    public bool ArtistExists(string name, IReadOnlyList<string> artists)
    {
        return artists.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public void InvalidateCache()
    {
        lock (syncRoot)
        {
            cachedArtists = null;
        }
    }

    private static IReadOnlyList<string> ScanArtists(CancellationToken cancellationToken)
    {
        string dbFolder = FilePathConstants.MusicDbFolder;
        if (!Directory.Exists(dbFolder))
            return [];

        var artists = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var f in Directory.GetFiles(dbFolder))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string name = Path.GetFileNameWithoutExtension(f);

            // Files are named "Song - Artist"; take the artist segment.
            var (_, artistPart) = FileNameParser.SplitSongArtist(name);
            if (!string.IsNullOrWhiteSpace(artistPart))
                artists.Add(artistPart);
        }

        return artists.OrderBy(a => a).ToArray();
    }
}
