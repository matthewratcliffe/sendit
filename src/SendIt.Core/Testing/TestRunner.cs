using System.Diagnostics;
using SendIt.Core.Configuration;

namespace SendIt.Core.Testing;

public record TestCommandResult(string Command, bool Success, string Output);

public class TestRunner
{
    private readonly TestSettings _settings;
    private readonly string _workingDirectory;

    public TestRunner(TestSettings settings, string workingDirectory)
    {
        _settings = settings;
        _workingDirectory = workingDirectory;
    }

    public async Task<IReadOnlyList<TestCommandResult>> RunAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<TestCommandResult>();

        foreach (var command in _settings.Commands)
        {
            var result = await RunCommandAsync(command, cancellationToken);
            results.Add(result);
            if (!result.Success && _settings.StopOnFailure) break;
        }

        return results;
    }

    private async Task<TestCommandResult> RunCommandAsync(string command, CancellationToken cancellationToken)
    {
        var isWindows = OperatingSystem.IsWindows();
        var psi = new ProcessStartInfo(isWindows ? "cmd.exe" : "/bin/sh", isWindows ? $"/c {command}" : $"-c \"{command}\"")
        {
            WorkingDirectory = _workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi);
        if (process is null) return new TestCommandResult(command, false, "Failed to start process.");

        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var output = (await stdOutTask) + (await stdErrTask);
        return new TestCommandResult(command, process.ExitCode == 0, output);
    }
}
