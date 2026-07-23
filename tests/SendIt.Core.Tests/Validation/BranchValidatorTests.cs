using SendIt.Core.Validation;
using Xunit;

namespace SendIt.Core.Tests.Validation;

public class BranchValidatorTests
{
    private readonly BranchValidator _validator = new(new[] { "feature/", "bugfix/", "hotfix/" });

    [Theory]
    [InlineData("feature/my-thing", true)]
    [InlineData("bugfix/HJI-123", true)]
    [InlineData("random-branch", false)]
    [InlineData("main", false)]
    public void IsValid_MatchesAllowedPrefixes(string branch, bool expected)
        => Assert.Equal(expected, _validator.IsValid(branch));

    [Fact]
    public void SuggestRename_AppliesDefaultPrefixToLeafName()
    {
        var result = _validator.SuggestRename("wip/my-thing", "feature/");
        Assert.Equal("feature/my-thing", result);
    }

    [Fact]
    public void SuggestRename_HandlesNameWithoutSlash()
    {
        var result = _validator.SuggestRename("mything", "feature/");
        Assert.Equal("feature/mything", result);
    }
}
