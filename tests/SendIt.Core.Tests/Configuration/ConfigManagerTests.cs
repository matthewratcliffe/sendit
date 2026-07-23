using SendIt.Core.Configuration;
using Xunit;

namespace SendIt.Core.Tests.Configuration;

public class ConfigManagerTests : IDisposable
{
    private readonly string _tempDir;

    public ConfigManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sendit-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public void Load_ReturnsDefaultsWhenNoFilesExist()
    {
        var manager = new ConfigManager(_tempDir, Path.Combine(_tempDir, "user.sendit.json"));
        var config = manager.Load();

        Assert.Equal("feature/", config.General.DefaultBranchPrefix);
        Assert.Contains("feature/", config.Git.AllowedBranchPrefixes);
    }

    [Fact]
    public void SaveRepo_NeverPersistsApiKey()
    {
        var manager = new ConfigManager(_tempDir, Path.Combine(_tempDir, "user.sendit.json"));
        var config = manager.Load();
        config.Ai.ApiKey = "super-secret-key";

        manager.SaveRepo(config);

        var raw = File.ReadAllText(manager.RepoConfigPath);
        Assert.DoesNotContain("super-secret-key", raw);
    }

    [Fact]
    public void RepoConfig_OverridesUserConfig()
    {
        var manager = new ConfigManager(_tempDir, Path.Combine(_tempDir, "user.sendit.json"));
        var userConfig = manager.Load();
        userConfig.General.DefaultBranchPrefix = "from-user/";
        manager.SaveUser(userConfig);

        var repoConfig = manager.Load();
        repoConfig.General.DefaultBranchPrefix = "from-repo/";
        manager.SaveRepo(repoConfig);

        var merged = manager.Load();
        Assert.Equal("from-repo/", merged.General.DefaultBranchPrefix);
    }
}
