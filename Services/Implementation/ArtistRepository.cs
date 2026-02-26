using System.IO;
using UtilityAppV2.Core;
using UtilityAppV2.Services.Interfaces;

namespace UtilityAppV2.Services.Implementation;

public class ArtistRepository : IArtistRepository
{
    private List<string>? cachedArtists;
    private readonly Lock syncRoot = new();

    public Task<List<string>> LoadArtistsAsync(CancellationToken cancellationToken = default)
    {
        lock (syncRoot)
        {
            if (cachedArtists != null)
            {
                return Task.FromResult(cachedArtists);
            }

            string dbFolder = FilePathConstants.MusicDbFolder;
            if (!Directory.Exists(dbFolder))
            {
                cachedArtists = [];
                return Task.FromResult(cachedArtists);
            }

            var files = Directory.GetFiles(dbFolder);
            var artists = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var f in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string name = Path.GetFileNameWithoutExtension(f).Trim();
                int idx = name.LastIndexOf('-');
                if (idx > 0)
                {
                    string artistPart = name[(idx + 1)..].Trim();
                    if (!string.IsNullOrWhiteSpace(artistPart))
                    {
                        artists.Add(artistPart);
                    }
                }
            }

            cachedArtists = artists.OrderBy(a => a).ToList();
            return Task.FromResult(cachedArtists);
        }
    }

    public bool ArtistExists(string name, List<string> artists)
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
}