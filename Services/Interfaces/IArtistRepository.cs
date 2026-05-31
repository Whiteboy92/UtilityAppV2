namespace UtilityAppV2.Services.Interfaces;

public interface IArtistRepository
{
    Task<IReadOnlyList<string>> LoadArtistsAsync(CancellationToken cancellationToken = default);
    bool ArtistExists(string name, IReadOnlyList<string> artists);
    void InvalidateCache();
}
