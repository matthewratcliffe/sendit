using SendIt.Core.Git;

namespace SendIt.Core.Validation;

public record ValidationIssue(string Code, string Message);

public class RepositoryValidator
{
    private readonly GitService _git;

    public RepositoryValidator(GitService git) => _git = git;

    public IReadOnlyList<ValidationIssue> Validate()
    {
        var issues = new List<ValidationIssue>();

        if (!_git.IsRepository())
        {
            issues.Add(new ValidationIssue("NoRepository", "Current directory is not a git repository."));
            return issues;
        }

        if (string.IsNullOrEmpty(_git.GetCurrentBranch()))
            issues.Add(new ValidationIssue("NoBranch", "Could not determine the current branch."));

        if (_git.HasMergeConflict())
            issues.Add(new ValidationIssue("MergeConflict", "Unresolved merge conflicts detected."));

        try
        {
            _ = _git.Status();
        }
        catch (Exception ex)
        {
            issues.Add(new ValidationIssue("WorkingTreeInaccessible", $"Working tree is not accessible: {ex.Message}"));
        }

        var version = _git.GetVersion();
        if (string.IsNullOrEmpty(version))
            issues.Add(new ValidationIssue("GitNotSupported", "Unable to determine git version."));

        return issues;
    }
}
