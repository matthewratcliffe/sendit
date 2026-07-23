using SendIt.Core.Commits;
using Xunit;

namespace SendIt.Core.Tests.Commits;

public class CommitFormatterTests
{
    [Fact]
    public void NormalizeManual_PrependsTicketWhenPresent()
        => Assert.Equal("[HJI-123] - fix the thing", CommitFormatter.NormalizeManual("fix the thing", "HJI-123"));

    [Fact]
    public void NormalizeManual_OmitsPrefixWhenNoTicket()
        => Assert.Equal("fix the thing", CommitFormatter.NormalizeManual("fix the thing", null));

    [Fact]
    public void NormalizeManual_TrimsWhitespace()
        => Assert.Equal("[ABC-1] - fix", CommitFormatter.NormalizeManual("  fix  ", "ABC-1"));
}
