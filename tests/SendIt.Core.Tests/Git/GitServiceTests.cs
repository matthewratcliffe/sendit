using SendIt.Core.Git;
using Xunit;

namespace SendIt.Core.Tests.Git;

public class GitServiceTests : IDisposable
{
    private readonly string _repoDir;
    private readonly GitService _git;

    public GitServiceTests()
    {
        _repoDir = Path.Combine(Path.GetTempPath(), "sendit-git-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_repoDir);
        _git = new GitService(_repoDir);
        _git.Run("init");
        _git.Run("config user.email test@example.com");
        _git.Run("config user.name Test User");
    }

    public void Dispose()
    {
        try { Directory.Delete(_repoDir, recursive: true); } catch { /* best effort on Windows file locks */ }
    }

    [Fact]
    public void IsRepository_TrueAfterInit() => Assert.True(_git.IsRepository());

    [Fact]
    public void StageAll_And_Commit_Succeeds()
    {
        File.WriteAllText(Path.Combine(_repoDir, "file.txt"), "hello");
        Assert.True(_git.StageAll().Success);
        Assert.True(_git.Commit("feat: initial commit").Success);
        Assert.Contains("feat: initial commit", _git.Log(1));
    }

    [Fact]
    public void HasMergeConflict_FalseOnCleanRepo() => Assert.False(_git.HasMergeConflict());

    [Fact]
    public void GetUpstreamBranch_NullWhenNoRemote() => Assert.Null(_git.GetUpstreamBranch());
}
