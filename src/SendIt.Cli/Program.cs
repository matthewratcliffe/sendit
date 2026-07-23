using System.Reflection;
using SendIt.Cli;
using SendIt.Core;
using SendIt.Core.Configuration;
using SendIt.Core.Diagnostics;
using SendIt.Core.Git;
using SendIt.Core.Logging;
using Serilog;
using Spectre.Console;

string Version = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    ?? "1.0.0";

if (args.Contains("--version"))
{
    Console.WriteLine($"sendit {Version}");
    return (int)ExitCode.Success;
}

if (args.Contains("--help"))
{
    PrintHelp();
    return (int)ExitCode.Success;
}

var repoRoot = Directory.GetCurrentDirectory();

if (args.Contains("-reset"))
{
    ConfigManager.Reset(repoRoot);
    AnsiConsole.MarkupLine("[green]User configuration removed.[/]");
    return (int)ExitCode.Success;
}

if (!GitService.TryLocateGit(out _))
{
    AnsiConsole.MarkupLine("[red]git executable was not found on PATH.[/]");
    return (int)ExitCode.GitFailure;
}

var availableUpdate = await new UpdateChecker().GetAvailableUpdateAsync(Version, CancellationToken.None);
if (availableUpdate is not null)
{
    AnsiConsole.MarkupLineInterpolated(
        $"[yellow]An update is available: {Version} -> {availableUpdate}. Run the install script again to update.[/]");
}

var git = new GitService(repoRoot);
var configManager = new ConfigManager(repoRoot);
SenditConfig config;
try
{
    config = configManager.Load();
}
catch (Exception ex)
{
    AnsiConsole.MarkupLineInterpolated($"[red]Configuration error: {ex.Message}[/]");
    return (int)ExitCode.ConfigurationError;
}

if (args.Contains("-configure"))
{
    ConfigureUi.Run(configManager, config);
    return (int)ExitCode.Success;
}

var logger = LoggingSetup.CreateLogger(config.Advanced, config.General.VerboseLogging);

if (args.Contains("-doctor"))
{
    var doctor = new DoctorService(git, config);
    var checks = await doctor.RunAsync();
    var table = new Table().AddColumn("Check").AddColumn("Result").AddColumn("Detail");
    foreach (var check in checks)
        table.AddRow(check.Name, check.Passed ? "[green]PASS[/]" : "[red]FAIL[/]", check.Detail.EscapeMarkup());
    AnsiConsole.Write(table);
    return checks.All(c => c.Passed) ? (int)ExitCode.Success : (int)ExitCode.ValidationFailed;
}

var options = new WorkflowOptions(
    SkipTests: args.Contains("-skiptests"),
    NoPr: args.Contains("-nopr"),
    Force: args.Contains("-force"));

var runner = new WorkflowRunner(git, config, new SpectreUserInteraction(), logger, options);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    var exitCode = await runner.RunAsync(cts.Token);
    return (int)exitCode;
}
catch (OperationCanceledException)
{
    AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
    return (int)ExitCode.UserCancelled;
}
finally
{
    Log.CloseAndFlush();
}

static void PrintHelp()
{
    Console.WriteLine("""
        sendit - AI-assisted Git workflow automation

        Usage:
          sendit                Run the complete workflow
          sendit -configure      Launch the configuration interface
          sendit -doctor         Run diagnostics
          sendit -reset          Delete user configuration
          sendit -skiptests      Skip project validation
          sendit -nopr           Disable Pull Request generation (default in this build)
          sendit -force          Override warnings
          sendit --version       Display version
          sendit --help          Display this help
        """);
}
