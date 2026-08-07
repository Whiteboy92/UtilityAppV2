using UtilityAppV2.Models;
using Xunit;

namespace UtilityAppV2.Tests.Models;

public class ResultModelTests
{
    [Fact]
    public void FileMoveResult_NotAbortedWhenNoError()
    {
        var result = new FileMoveResult { TotalFiles = 3, MovedFiles = 3 };

        Assert.False(result.Aborted);
        Assert.Empty(result.MoveErrors);
        Assert.Empty(result.TagErrors);
    }

    [Fact]
    public void FileMoveResult_AbortedWhenErrorSet()
    {
        var result = new FileMoveResult { ErrorMessage = "boom" };

        Assert.True(result.Aborted);
    }

    [Fact]
    public void ConversionResult_DefaultsToEmptyFailures()
    {
        var result = new ConversionResult { TotalFiles = 5, ConvertedFiles = 5 };

        Assert.NotNull(result.FailedFiles);
        Assert.Empty(result.FailedFiles);
    }
}
