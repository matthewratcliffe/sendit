using System.Text.RegularExpressions;
using SendIt.Core.Ai;
using SendIt.Core.Configuration;

namespace SendIt.Core.Commits;

public record CommitGenerationResult(bool Success, string Message, string? Error);

/// <summary>
/// Builds the AI prompt from git context, requests a Conventional Commit message,
/// and enforces the "single line, no ticket, no markdown, no explanation" output contract.
/// </summary>
public class CommitMessageGenerator
{
    private static readonly Regex ConventionalCommitPattern = new(
        @"^(feat|fix|chore|docs|style|refactor|perf|test|build|ci|revert)(\([\w\-\/]+\))?!?: .+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IAiProvider _provider;
    private readonly AiSettings _settings;

    public CommitMessageGenerator(IAiProvider provider, AiSettings settings)
    {
        _provider = provider;
        _settings = settings;
    }

    public async Task<CommitGenerationResult> GenerateAsync(CommitContext context, CancellationToken cancellationToken = default)
    {
        var prompt = _settings.PromptTemplate
            .Replace("{branch}", context.Branch)
            .Replace("{ticket}", context.Ticket ?? "None")
            .Replace("{status}", context.Status)
            .Replace("{diff}", Truncate(context.Diff))
            .Replace("{log}", context.RecentLog);

        var response = await _provider.GenerateAsync(
            new AiRequest(_settings.SystemPrompt, prompt, _settings.Temperature, _settings.MaxTokens),
            cancellationToken);

        if (!response.Success)
            return new CommitGenerationResult(false, "", response.Error);

        var cleaned = Sanitize(response.Text);
        return new CommitGenerationResult(true, cleaned, null);
    }

    /// <summary>Strips markdown fences, collapses to a single line, and validates Conventional Commit form.</summary>
    private static string Sanitize(string text)
    {
        var firstLine = text
            .Replace("```", "")
            .Trim()
            .Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.Length > 0) ?? "";

        return firstLine.Trim('`', '"');
    }

    public static bool IsConventionalCommit(string message) => ConventionalCommitPattern.IsMatch(message);

    private static string Truncate(string diff, int maxChars = 8000)
        => diff.Length <= maxChars ? diff : diff[..maxChars] + "\n... (truncated)";
}
