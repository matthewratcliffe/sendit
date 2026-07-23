using SendIt.Core.Configuration;
using SendIt.Core.Validation;
using Xunit;

namespace SendIt.Core.Tests.Validation;

public class LargeCommitDetectorTests
{
    private readonly LargeCommitDetector _detector = new(new GitSettings
    {
        LargeCommitFileThreshold = 50,
        LargeCommitLineThreshold = 2000,
        LargeCommitMaxFileSizeBytes = 100L * 1024 * 1024,
        LargeCommitMaxBinaryCount = 20
    });

    [Fact]
    public void Evaluate_NoWarningsUnderThresholds()
    {
        var warnings = _detector.Evaluate(new CommitStats(10, 100, 0, 1024));
        Assert.Empty(warnings);
    }

    [Fact]
    public void Evaluate_WarnsOnEachExceededThreshold()
    {
        var warnings = _detector.Evaluate(new CommitStats(
            FilesChanged: 100, LinesChanged: 5000, BinaryFileCount: 25, MaxFileSizeBytes: 200L * 1024 * 1024));

        Assert.Equal(4, warnings.Count);
    }
}
