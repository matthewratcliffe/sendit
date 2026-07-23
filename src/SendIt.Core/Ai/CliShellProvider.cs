using System.Diagnostics;
using SendIt.Core.Configuration;

namespace SendIt.Core.Ai;

/// <summary>
/// Shells out to a local CLI tool (Claude Code, Codex CLI, OpenCode, or any custom command),
/// piping the combined prompt via stdin and reading the generated text from stdout.
/// This is what makes SendIt work fully offline against tools already on the developer's PATH.
/// </summary>
public class CliShellProvider : IAiProvider
{
    private readonly AiSettings _settings;

    public string Name => _settings.Provider switch
    {
        AiProviderKind.ClaudeCli => "Claude CLI",
        AiProviderKind.CodexCli => "Codex CLI",
        AiProviderKind.KiroCli => "Kiro CLI",
        AiProviderKind.OpenCode => "OpenCode",
        _ => "Custom Command"
    };

    public CliShellProvider(AiSettings settings) => _settings = settings;

    public async Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var prompt = $"{request.SystemPrompt}\n\n{request.UserPrompt}";
        var attempts = Math.Max(1, _settings.RetryCount + 1);
        string? lastError = null;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                var (success, stdOut, stdErr) = await RunAsync(prompt, cancellationToken);
                if (success && !string.IsNullOrWhiteSpace(stdOut))
                    return new AiResponse(true, stdOut.Trim(), null);
                lastError = string.IsNullOrWhiteSpace(stdErr) ? "Command produced no output." : stdErr;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }
        }

        return new AiResponse(false, "", lastError ?? "Unknown error");
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = new ProcessStartInfo(_settings.CommandExecutable, "--version")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi);
            if (process is null) return false;
            process.StandardInput.Close();

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, _settings.TimeoutSeconds)));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best-effort; the process may have already exited.
        }
    }

    private async Task<(bool Success, string StdOut, string StdErr)> RunAsync(string prompt, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(_settings.CommandExecutable, _settings.CommandArguments)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start '{_settings.CommandExecutable}'.");

        await process.StandardInput.WriteAsync(prompt);
        process.StandardInput.Close();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, _settings.TimeoutSeconds)));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var stdOutTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
        var stdErrTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        var stdOut = await stdOutTask;
        var stdErr = await stdErrTask;

        return (process.ExitCode == 0, stdOut, stdErr);
    }
}
