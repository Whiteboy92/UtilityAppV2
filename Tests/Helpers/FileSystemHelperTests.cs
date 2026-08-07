using System.IO;
using UtilityAppV2.Helpers;
using UtilityAppV2.Tests.TestSupport;
using Xunit;

namespace UtilityAppV2.Tests.Helpers;

public class FileSystemHelperTests
{
    [Fact]
    public void GetAllFilesFromFolder_ReturnsEmptyForMissingFolder()
    {
        var result = FileSystemHelper.GetAllFilesFromFolder(@"Z:\definitely\not\here_" + Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public void GetAllFilesFromFolder_ReturnsTopLevelFiles()
    {
        using var dir = new TempDir();
        dir.Touch("a.mp3");
        dir.Touch("b.mp3");

        var result = FileSystemHelper.GetAllFilesFromFolder(dir.Path);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void GetUniqueFilePath_ReturnsSamePathWhenFree()
    {
        using var dir = new TempDir();
        string target = dir.Combine("song.png");

        Assert.Equal(target, FileSystemHelper.GetUniqueFilePath(target));
    }

    [Fact]
    public void GetUniqueFilePath_IncrementsUntilFree()
    {
        using var dir = new TempDir();
        dir.Touch("song.png");
        dir.Touch("song (1).png");

        string unique = FileSystemHelper.GetUniqueFilePath(dir.Combine("song.png"));

        Assert.Equal(dir.Combine("song (2).png"), unique);
    }

    [Fact]
    public void RemoveInvalidFileNameChars_StripsInvalidChars()
    {
        // ':' and '/' are invalid file name chars on Windows.
        string cleaned = FileSystemHelper.RemoveInvalidFileNameChars("a:b/c");

        Assert.Equal("abc", cleaned);
    }

    [Fact]
    public void RemoveInvalidFileNameChars_CollapsesWhitespaceAndTrims()
    {
        string cleaned = FileSystemHelper.RemoveInvalidFileNameChars("  Song    Name  ");

        Assert.Equal("Song Name", cleaned);
    }

    [Fact]
    public void EnsureFoldersExist_CreatesMissingFolders()
    {
        using var dir = new TempDir();
        string nested = dir.Combine("a");
        string nested2 = dir.Combine("b");

        FileSystemHelper.EnsureFoldersExist(nested, nested2);

        Assert.True(Directory.Exists(nested));
        Assert.True(Directory.Exists(nested2));
    }
}
