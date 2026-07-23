using System.Diagnostics;

namespace SendIt.Core.Git;

public record GitResult(bool Success, string StdOut, string StdErr, int ExitCode);

/// <summary>Thin wrapper around the git CLI executable.</summary>
public class GitService
{
    private readonly string _workingDirectory;
    private readonly string _gitExecutable;

    public GitService(string workingDirectory, string gitExecutable = "git")
    {
        _workingDirectory = workingDirectory;
        _gitExecutable = gitExecutable;
    }

    public static bool TryLocateGit(out string path)
    {
        path = "git";
        try
        {
            var result = RunStatic("git", "--version", Directory.GetCurrentDirectory());
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    public GitResult Run(string arguments, CancellationToken cancellationToken = default)
        => RunStatic(_gitExecutable, arguments, _workingDirectory, cancellationToken);

    private static GitResult RunStatic(string executable, string arguments, string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(executable, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start '{executable}'.");

        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        process.WaitForExit();

        var stdOut = stdOutTask.GetAwaiter().GetResult();
        var stdErr = stdErrTask.GetAwaiter().GetResult();
        return new GitResult(process.ExitCode == 0, stdOut.Trim(), stdErr.Trim(), process.ExitCode);
    }

    public bool IsRepository() => Run("rev-parse --is-inside-work-tree").Success;

    public string GetVersion() => Run("--version").StdOut;

    public string GetCurrentBranch() => Run("rev-parse --abbrev-ref HEAD").StdOut;

    public bool HasMergeConflict()
    {
        var result = Run("diff --name-only --diff-filter=U");
        return result.Success && result.StdOut.Length > 0;
    }

    public string Status(bool porcelain = true) => Run(porcelain ? "status --porcelain" : "status").StdOut;

    public string Diff() => Run("diff").StdOut;

    public string DiffCached() => Run("diff --cached").StdOut;

    public string DiffStat() => Run("diff --stat").StdOut;

    public string Log(int count = 10) => Run($"log -{count} --oneline").StdOut;

    public string RemoteUrl() => Run("config --get remote.origin.url").StdOut;

    public string RepositoryName()
    {
        var toplevel = Run("rev-parse --show-toplevel").StdOut;
        return string.IsNullOrEmpty(toplevel) ? "" : Path.GetFileName(toplevel.TrimEnd('/', '\\'));
    }

    public string CurrentUser() => Run("config --get user.name").StdOut;

    public GitResult StageAll() => Run("add -A");

    public GitResult Commit(string message)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, message);
        try
        {
            return Run($"commit -F \"{tempFile}\"");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    public string? GetUpstreamBranch()
    {
        var result = Run("rev-parse --abbrev-ref --symbolic-full-name @{u}");
        return result.Success ? result.StdOut : null;
    }

    public bool HasUnpushedCommits()
    {
        var upstream = GetUpstreamBranch();
        if (upstream is null) return false;
        var result = Run($"log {upstream}..HEAD --oneline");
        return result.Success && result.StdOut.Length > 0;
    }

    public GitResult Push()
    {
        var upstream = GetUpstreamBranch();
        return upstream is null ? Run($"push -u origin {GetCurrentBranch()}") : Run("push");
    }
}
