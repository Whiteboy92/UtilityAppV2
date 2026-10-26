using UtilityAppV2.Helpers;
using UtilityAppV2.Tests.TestSupport;
using Xunit;

namespace UtilityAppV2.Tests.Helpers;

public class ValidationHelperTests
{
    [Fact]
    public void ValidateSourceFolder_TrueWhenExists()
    {
        using var dir = new TempDir();

        Assert.True(ValidationHelper.ValidateSourceFolder(dir.Path, out string? error));
        Assert.Null(error);
    }

    [Fact]
    public void ValidateSourceFolder_FalseWhenMissing()
    {
        bool ok = ValidationHelper.ValidateSourceFolder(@"Z:\nope_" + Guid.NewGuid(), out string? error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void EnsureNoUnknownArtist_FalseWhenPresent()
    {
        string[] files = [@"C:\p\Song - Unknown Artist.mp3", @"C:\p\Good - Artist.mp3"];

        bool ok = ValidationHelper.EnsureNoUnknownArtist(files, out string? error);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("Unknown Artist", error);
    }

    [Fact]
    public void EnsureNoUnknownArtist_TrueWhenClean()
    {
        string[] files = [@"C:\p\Song - Artist.mp3"];

        Assert.True(ValidationHelper.EnsureNoUnknownArtist(files, out string? error));
        Assert.Null(error);
    }

    [Fact]
    public void EnsureNoDuplicates_FalseOnDuplicatesWithinSource()
    {
        using var dest = new TempDir();
        // Same file name from two different source folders => collision on move.
        string[] files = [@"C:\one\track.mp3", @"C:\two\track.mp3"];

        bool ok = ValidationHelper.EnsureNoDuplicates(files, dest.Path, out string? error);

        Assert.False(ok);
        Assert.Contains("Duplicate", error);
    }

    [Fact]
    public void EnsureNoDuplicates_FalseWhenCollidesWithDestination()
    {
        using var dest = new TempDir();
        dest.Touch("track.mp3");
        string[] files = [@"C:\src\track.mp3"];

        bool ok = ValidationHelper.EnsureNoDuplicates(files, dest.Path, out string? error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void EnsureNoDuplicates_TrueWhenAllUnique()
    {
        using var dest = new TempDir();
        string[] files = [@"C:\src\a.mp3", @"C:\src\b.mp3"];

        Assert.True(ValidationHelper.EnsureNoDuplicates(files, dest.Path, out string? error));
        Assert.Null(error);
    }

    [Fact]
    public void EnsureNoDuplicates_IsCaseInsensitive()
    {
        using var dest = new TempDir();
        string[] files = [@"C:\src\Track.mp3", @"C:\src\track.mp3"];

        Assert.False(ValidationHelper.EnsureNoDuplicates(files, dest.Path, out _));
    }

    [Fact]
    public void FindCoverCollisions_FlagsSameTitleDifferentArtist()
    {
        using var dest = new TempDir();
        dest.Touch("Hurt - Johnny Cash.mp3");
        string incoming = @"C:\src\Hurt - Nine Inch Nails.mp3";

        var collisions = ValidationHelper.FindCoverCollisions([incoming], dest.Path);

        Assert.Single(collisions);
        Assert.Equal(incoming, collisions[0].IncomingFile);
        Assert.Equal("Hurt - Johnny Cash.mp3", collisions[0].ExistingFileName);
    }

    [Fact]
    public void FindCoverCollisions_IgnoresExactSameNameAndArtist()
    {
        using var dest = new TempDir();
        dest.Touch("Hurt - Johnny Cash.mp3");
        // Same full name => an exact duplicate, handled by EnsureNoDuplicates, not here.
        string incoming = @"C:\src\Hurt - Johnny Cash.mp3";

        Assert.Empty(ValidationHelper.FindCoverCollisions([incoming], dest.Path));
    }

    [Fact]
    public void FindCoverCollisions_TitleMatchIsCaseInsensitive()
    {
        using var dest = new TempDir();
        dest.Touch("Hurt - Johnny Cash.mp3");
        string incoming = @"C:\src\hurt - Nine Inch Nails.mp3";

        Assert.Single(ValidationHelper.FindCoverCollisions([incoming], dest.Path));
    }

    [Fact]
    public void FindCoverCollisions_EmptyWhenNoTitleMatch()
    {
        using var dest = new TempDir();
        dest.Touch("Hurt - Johnny Cash.mp3");
        string incoming = @"C:\src\Numb - Linkin Park.mp3";

        Assert.Empty(ValidationHelper.FindCoverCollisions([incoming], dest.Path));
    }
}
