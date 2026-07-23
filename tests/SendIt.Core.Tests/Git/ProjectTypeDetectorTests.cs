using SendIt.Core.Git;
using Xunit;

namespace SendIt.Core.Tests.Git;

public class ProjectTypeDetectorTests : IDisposable
{
    private readonly string _dir;

    public ProjectTypeDetectorTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "sendit-detect-" + Guid.NewGuid());
        Directory.CreateDirectory(_dir);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Detect_FindsDotnetProject()
    {
        File.WriteAllText(Path.Combine(_dir, "app.csproj"), "<Project />");
        Assert.Contains("dotnet", ProjectTypeDetector.Detect(_dir));
    }

    [Fact]
    public void Detect_FindsNodeProject()
    {
        File.WriteAllText(Path.Combine(_dir, "package.json"), "{}");
        Assert.Contains("node", ProjectTypeDetector.Detect(_dir));
    }

    [Fact]
    public void Detect_ReturnsEmptyForUnknownProject()
        => Assert.Empty(ProjectTypeDetector.Detect(_dir));
}
