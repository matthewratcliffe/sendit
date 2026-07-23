using Moq;
using SendIt.Core.Ai;
using SendIt.Core.Commits;
using SendIt.Core.Configuration;
using Xunit;

namespace SendIt.Core.Tests.Commits;

public class CommitMessageGeneratorTests
{
    private static CommitContext SampleContext() => new(
        "feature/HJI-123-thing", "HJI-123", "M file.cs", "diff --git a/file.cs", "", "1 file changed", "abc1234 previous",
        "repo", "user", "git@example.com:org/repo.git");

    [Fact]
    public async Task GenerateAsync_SanitizesMarkdownFencesAndWhitespace()
    {
        var providerMock = new Mock<IAiProvider>();
        providerMock
            .Setup(p => p.GenerateAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, "```\nfeat: add the thing\n```", null));

        var generator = new CommitMessageGenerator(providerMock.Object, new AiSettings());
        var result = await generator.GenerateAsync(SampleContext());

        Assert.True(result.Success);
        Assert.Equal("feat: add the thing", result.Message);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsFailureWhenProviderFails()
    {
        var providerMock = new Mock<IAiProvider>();
        providerMock
            .Setup(p => p.GenerateAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(false, "", "connection refused"));

        var generator = new CommitMessageGenerator(providerMock.Object, new AiSettings());
        var result = await generator.GenerateAsync(SampleContext());

        Assert.False(result.Success);
        Assert.Equal("connection refused", result.Error);
    }

    [Theory]
    [InlineData("feat: add thing", true)]
    [InlineData("fix(auth): correct token refresh", true)]
    [InlineData("Added a new thing", false)]
    [InlineData("WIP", false)]
    public void IsConventionalCommit_ValidatesFormat(string message, bool expected)
        => Assert.Equal(expected, CommitMessageGenerator.IsConventionalCommit(message));
}
