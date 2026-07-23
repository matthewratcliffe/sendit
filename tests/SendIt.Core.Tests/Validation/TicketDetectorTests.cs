using SendIt.Core.Validation;
using Xunit;

namespace SendIt.Core.Tests.Validation;

public class TicketDetectorTests
{
    private readonly TicketDetector _detector = new(new[] { "HJI-\\d+", "ABC-\\d+", "JIRA-\\d+" });

    [Theory]
    [InlineData("feature/HJI-1606-add-thing", "HJI-1606")]
    [InlineData("bugfix/ABC-123", "ABC-123")]
    [InlineData("feature/no-ticket-here", null)]
    public void Detect_FindsFirstMatchingPattern(string text, string? expected)
        => Assert.Equal(expected, _detector.Detect(text));
}
