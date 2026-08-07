using UtilityAppV2.Services.Implementation;
using Xunit;

namespace UtilityAppV2.Tests.Services;

public class RegexFileNameCleanerTests
{
    private readonly RegexFileNameCleaner cleaner = new();

    [Theory]
    [InlineData("Song (Official Music Video)", "Song")]
    [InlineData("Song [Official Music Video]", "Song")]
    [InlineData("Song (Official Audio)", "Song")]
    [InlineData("Song [Lyrics]", "Song")]
    [InlineData("Song (HD)", "Song")]
    [InlineData("Song (Explicit)", "Song")]
    [InlineData("Song (feat. Someone)", "Song")]
    public void StripsUnwantedTags(string input, string expected)
    {
        Assert.Equal(expected, cleaner.CleanFileName(input));
    }

    [Fact]
    public void CollapsesWhitespaceLeftByRemoval()
    {
        var result = cleaner.CleanFileName("Title (Official Video) - Artist");

        Assert.Equal("Title - Artist", result);
    }

    [Fact]
    public void RemovesQuoteCharacters()
    {
        Assert.Equal("Dont", cleaner.CleanFileName("Don't"));
        Assert.Equal("Say Yes", cleaner.CleanFileName("Say \"Yes\""));
    }

    [Fact]
    public void FrenchcoreIsWordBounded_StrippedWhenStandalone()
    {
        Assert.Equal("Anthem", cleaner.CleanFileName("Frenchcore Anthem"));
    }

    [Fact]
    public void FrenchcoreIsWordBounded_KeptInsideLongerWord()
    {
        // "Frenchcorner" must not be mangled by the frenchcore rule.
        Assert.Equal("Frenchcorner", cleaner.CleanFileName("Frenchcorner"));
    }

    [Fact]
    public void TrimsLeadingAndTrailingDashesAndUnderscores()
    {
        Assert.Equal("Clean Name", cleaner.CleanFileName("_- Clean Name -_"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankInputReturnsEmpty(string input)
    {
        Assert.Equal(string.Empty, cleaner.CleanFileName(input));
    }

    [Fact]
    public void ParsePartsDelegatesToFileNameParser()
    {
        var (song, artist) = cleaner.ParseParts("Song - Artist");

        Assert.Equal("Song", song);
        Assert.Equal("Artist", artist);
    }
}
