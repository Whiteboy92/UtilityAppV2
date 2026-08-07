using UtilityAppV2.Services.Implementation;
using Xunit;

namespace UtilityAppV2.Tests.Services;

// Note: LoadArtistsAsync/ScanArtists read the hardcoded MusicDbFolder from
// FilePathConstants, so they are not unit-testable without that machine-specific
// path. ArtistExists is the pure matching logic and is fully covered here.
public class ArtistRepositoryTests
{
    private readonly ArtistRepository repo = new();

    [Fact]
    public void ArtistExists_TrueForExactMatch()
    {
        Assert.True(repo.ArtistExists("Metallica", ["Metallica", "Slayer"]));
    }

    [Fact]
    public void ArtistExists_IsCaseInsensitive()
    {
        Assert.True(repo.ArtistExists("metallica", ["Metallica"]));
    }

    [Fact]
    public void ArtistExists_FalseWhenAbsent()
    {
        Assert.False(repo.ArtistExists("Nightwish", ["Metallica", "Slayer"]));
    }

    [Fact]
    public void ArtistExists_FalseForEmptyList()
    {
        Assert.False(repo.ArtistExists("Anyone", []));
    }
}
