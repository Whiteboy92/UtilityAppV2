namespace UtilityAppV2.Services.Interfaces;

public interface IArtistRepository
{
    Task<List<string>> LoadArtistsAsync(CancellationToken cancellationToken = default);
    bool ArtistExists(string name, List<string> artists);
}