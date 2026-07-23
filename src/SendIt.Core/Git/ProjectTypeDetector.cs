namespace SendIt.Core.Git;

/// <summary>Detects likely project type(s) from marker files, used to generate a suitable .gitignore.</summary>
public static class ProjectTypeDetector
{
    private static readonly (string Pattern, string Type)[] Markers =
    {
        ("*.csproj", "dotnet"),
        ("*.sln", "dotnet"),
        ("*.slnx", "dotnet"),
        ("package.json", "node"),
        ("pyproject.toml", "python"),
        ("requirements.txt", "python"),
        ("Cargo.toml", "rust"),
        ("go.mod", "go"),
        ("pom.xml", "java-maven"),
        ("build.gradle", "java-gradle"),
        ("build.gradle.kts", "java-gradle"),
        ("Gemfile", "ruby"),
        ("composer.json", "php"),
        ("*.xcodeproj", "swift"),
        ("CMakeLists.txt", "cpp"),
    };

    public static IReadOnlyList<string> Detect(string repositoryRoot)
    {
        var found = new List<string>();
        foreach (var (pattern, type) in Markers)
        {
            if (found.Contains(type)) continue;
            if (Directory.EnumerateFiles(repositoryRoot, pattern, SearchOption.TopDirectoryOnly).Any())
                found.Add(type);
        }
        return found;
    }
}
