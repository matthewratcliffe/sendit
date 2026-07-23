using SendIt.Core.Configuration;

namespace SendIt.Core.Validation;

public record CommitStats(int FilesChanged, int LinesChanged, int BinaryFileCount, long MaxFileSizeBytes);

public class LargeCommitDetector
{
    private readonly GitSettings _settings;

    public LargeCommitDetector(GitSettings settings) => _settings = settings;

    public IReadOnlyList<string> Evaluate(CommitStats stats)
    {
        var warnings = new List<string>();

        if (stats.FilesChanged > _settings.LargeCommitFileThreshold)
            warnings.Add($"{stats.FilesChanged} files changed exceeds threshold of {_settings.LargeCommitFileThreshold}.");

        if (stats.LinesChanged > _settings.LargeCommitLineThreshold)
            warnings.Add($"{stats.LinesChanged} changed lines exceeds threshold of {_settings.LargeCommitLineThreshold}.");

        if (stats.MaxFileSizeBytes > _settings.LargeCommitMaxFileSizeBytes)
            warnings.Add($"A file of {stats.MaxFileSizeBytes / (1024 * 1024)}MB exceeds the {_settings.LargeCommitMaxFileSizeBytes / (1024 * 1024)}MB limit.");

        if (stats.BinaryFileCount > _settings.LargeCommitMaxBinaryCount)
            warnings.Add($"{stats.BinaryFileCount} binary files exceeds threshold of {_settings.LargeCommitMaxBinaryCount}.");

        return warnings;
    }
}
