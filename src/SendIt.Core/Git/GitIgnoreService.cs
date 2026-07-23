using SendIt.Core.Ai;

namespace SendIt.Core.Git;

/// <summary>
/// If a repository has no .gitignore, offers to generate one using the configured AI provider,
/// based on project type(s) detected from marker files in the repository root.
/// </summary>
public class GitIgnoreService
{
    private readonly IAiProvider _provider;
    private readonly string _repositoryRoot;

    public GitIgnoreService(IAiProvider provider, string repositoryRoot)
    {
        _provider = provider;
        _repositoryRoot = repositoryRoot;
    }

    public bool Exists() => File.Exists(Path.Combine(_repositoryRoot, ".gitignore"));

    public async Task<string?> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var types = ProjectTypeDetector.Detect(_repositoryRoot);
        var typeDescription = types.Count > 0 ? string.Join(", ", types) : "unknown/generic";

        var response = await _provider.GenerateAsync(new AiRequest(
            SystemPrompt: "You generate .gitignore files. Reply with only the raw .gitignore file contents. " +
                          "No markdown fences, no explanation.",
            UserPrompt: $"Generate a .gitignore file for a project using: {typeDescription}. " +
                        "Include common OS, editor, and build-output entries.",
            Temperature: 0.1,
            MaxTokens: 800), cancellationToken);

        if (!response.Success) return null;

        return response.Text
            .Replace("```gitignore", "")
            .Replace("```", "")
            .Trim() + Environment.NewLine;
    }

    public void Write(string contents)
        => File.WriteAllText(Path.Combine(_repositoryRoot, ".gitignore"), contents);
}
