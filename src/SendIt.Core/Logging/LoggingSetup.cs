using Serilog;
using Serilog.Events;
using SendIt.Core.Configuration;

namespace SendIt.Core.Logging;

public static class LoggingSetup
{
    public static ILogger CreateLogger(AdvancedSettings settings, bool verbose)
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SendIt", "logs");
        Directory.CreateDirectory(logDirectory);

        var minimumLevel = ParseLevel(verbose ? "Verbose" : settings.LogLevel);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .WriteTo.File(
                Path.Combine(logDirectory, "sendit-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: Math.Max(1, settings.LogRetainedFileCount))
            .CreateLogger();

        return Log.Logger;
    }

    private static LogEventLevel ParseLevel(string level) => level.ToLowerInvariant() switch
    {
        "error" => LogEventLevel.Error,
        "warning" => LogEventLevel.Warning,
        "info" => LogEventLevel.Information,
        "verbose" => LogEventLevel.Verbose,
        "debug" => LogEventLevel.Debug,
        _ => LogEventLevel.Information
    };
}
