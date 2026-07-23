namespace SendIt.Core.Validation;

public enum BranchAction
{
    RenameAutomatically,
    RenameManually,
    Continue,
    Cancel
}

public class BranchValidator
{
    private readonly IReadOnlyList<string> _allowedPrefixes;

    public BranchValidator(IReadOnlyList<string> allowedPrefixes) => _allowedPrefixes = allowedPrefixes;

    public bool IsValid(string branchName)
        => _allowedPrefixes.Any(prefix => branchName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>Suggests a corrected branch name by applying the closest allowed prefix.</summary>
    public string SuggestRename(string branchName, string defaultPrefix)
    {
        var name = branchName.Contains('/') ? branchName[(branchName.IndexOf('/') + 1)..] : branchName;
        return defaultPrefix + name;
    }
}
