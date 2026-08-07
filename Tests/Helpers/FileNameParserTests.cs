using UtilityAppV2.Helpers;
using Xunit;

namespace UtilityAppV2.Tests.Helpers;

public class FileNameParserTests
{
    [Fact]
    public void SplitsOnSpacedSeparator()
    {
        var (song, artist) = FileNameParser.SplitSongArtist("Enter Sandman - Metallica");

        Assert.Equal("Enter Sandman", song);
        Assert.Equal("Metallica", artist);
    }

    [Fact]
    public void KeepsHyphenatedArtistIntact()
    {
        // The spaced " - " is the real boundary; the hyphen in "Jay-Z" must survive.
        var (song, artist) = FileNameParser.SplitSongArtist("99 Problems - Jay-Z");

        Assert.Equal("99 Problems", song);
        Assert.Equal("Jay-Z", artist);
    }

    [Fact]
    public void SplitsOnLastSeparatorWhenMultiplePresent()
    {
        var (song, artist) = FileNameParser.SplitSongArtist("A - B - C");

        Assert.Equal("A - B", song);
        Assert.Equal("C", artist);
    }

    [Fact]
    public void FallsBackToBareDashWhenNoSpacedSeparator()
    {
        var (song, artist) = FileNameParser.SplitSongArtist("Digitoll -Coldrain");

        Assert.Equal("Digitoll", song);
        Assert.Equal("Coldrain", artist);
    }

    [Fact]
    public void NoSeparatorTreatsWholeNameAsSong()
    {
        var (song, artist) = FileNameParser.SplitSongArtist("JustATitle");

        Assert.Equal("JustATitle", song);
        Assert.Equal(string.Empty, artist);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EmptyOrWhitespaceReturnsEmptyPair(string? input)
    {
        var (song, artist) = FileNameParser.SplitSongArtist(input!);

        Assert.Equal(string.Empty, song);
        Assert.Equal(string.Empty, artist);
    }

    [Fact]
    public void TrimsSurroundingWhitespace()
    {
        var (song, artist) = FileNameParser.SplitSongArtist("  Song   -   Artist  ");

        Assert.Equal("Song", song);
        Assert.Equal("Artist", artist);
    }
}
