namespace SendIt.Core.Commits;

public record CommitContext(
    string Branch,
    string? Ticket,
    string Status,
    string Diff,
    string CachedDiff,
    string DiffStat,
    string RecentLog,
    string RepositoryName,
    string CurrentUser,
    string RemoteUrl);
