using SendIt.Core.Ai;
using SendIt.Core.Configuration;
using SendIt.Core.Git;

namespace SendIt.Core.Diagnostics;

public record DiagnosticCheck(string Name, bool Passed, string Detail);

public class DoctorService
{
    private readonly GitService _git;
    private readonly SenditConfig _config;

    public DoctorService(GitService git, SenditConfig config)
    {
        _git = git;
        _config = config;
    }

    public async Task<IReadOnlyList<DiagnosticCheck>> RunAsync(CancellationToken cancellationToken = default)
    {
        var checks = new List<DiagnosticCheck>
        {
            CheckGit(),
            CheckCli("GitHub CLI", "gh"),
            CheckCli("GitLab CLI", "glab"),
            CheckRepository(),
            CheckTicketRegex(),
            CheckConfiguration(),
            CheckSdks(),
        };

        checks.Add(await CheckAiProviderAsync(cancellationToken));
        checks.Add(await CheckNetworkAsync(cancellationToken));

        return checks;
    }

    private DiagnosticCheck CheckGit()
    {
        var version = _git.GetVersion();
        return new DiagnosticCheck("Git", !string.IsNullOrEmpty(version), string.IsNullOrEmpty(version) ? "git not found on PATH" : version);
    }

    private static DiagnosticCheck CheckCli(string name, string executable)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(executable, "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null) return new DiagnosticCheck(name, false, "not found");
            process.WaitForExit(3000);
            return new DiagnosticCheck(name, process.ExitCode == 0, process.ExitCode == 0 ? "available" : "not found");
        }
        catch
        {
            return new DiagnosticCheck(name, false, "not found");
        }
    }

    private DiagnosticCheck CheckRepository()
        => new("Repository", _git.IsRepository(), _git.IsRepository() ? "valid git repository" : "not a git repository");

    private DiagnosticCheck CheckTicketRegex()
    {
        var invalid = _config.Git.TicketPatterns
            .Where(p => !IsValidRegex(p))
            .ToList();
        return new DiagnosticCheck("Ticket regex", invalid.Count == 0,
            invalid.Count == 0 ? $"{_config.Git.TicketPatterns.Count} pattern(s) valid" : $"Invalid pattern(s): {string.Join(", ", invalid)}");
    }

    private static bool IsValidRegex(string pattern)
    {
        try { _ = new System.Text.RegularExpressions.Regex(pattern); return true; }
        catch { return false; }
    }

    private DiagnosticCheck CheckConfiguration()
        => new("Configuration", true, "loaded successfully");

    private static DiagnosticCheck CheckSdks()
    {
        var found = new List<string>();
        foreach (var (name, exe, args) in new[] { ("dotnet", "dotnet", "--version"), ("node", "node", "--version"), ("python", "python", "--version") })
        {
            if (CheckCli(name, exe).Passed) found.Add(name);
        }
        return new DiagnosticCheck("Installed SDKs", found.Count > 0, found.Count > 0 ? string.Join(", ", found) : "none detected");
    }

    private async Task<DiagnosticCheck> CheckAiProviderAsync(CancellationToken cancellationToken)
    {
        try
        {
            var provider = AiProviderFactory.Create(_config.Ai);
            var ok = await provider.TestConnectionAsync(cancellationToken);
            return new DiagnosticCheck("AI provider", ok, ok ? $"{provider.Name} reachable" : $"{provider.Name} not reachable");
        }
        catch (Exception ex)
        {
            return new DiagnosticCheck("AI provider", false, ex.Message);
        }
    }

    private async Task<DiagnosticCheck> CheckNetworkAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await client.GetAsync("https://github.com", cancellationToken);
            return new DiagnosticCheck("Network", response.IsSuccessStatusCode, response.IsSuccessStatusCode ? "internet reachable" : $"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return new DiagnosticCheck("Network", false, $"offline or unreachable ({ex.Message})");
        }
    }
}
