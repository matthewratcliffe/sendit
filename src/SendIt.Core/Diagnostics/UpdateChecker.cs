using System.Text.Json;

namespace SendIt.Core.Diagnostics;

/// <summary>
/// Checks GitHub Releases for a newer sendit build. Results are cached to disk for 24 hours
/// so normal usage never waits on a network round-trip.
/// </summary>
public class UpdateChecker
{
    private const string ReleasesUrl = "https://api.github.com/repos/matthewratcliffe/sendit/releases/latest";
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);

    private readonly string _cachePath;

    public UpdateChecker(string? cachePathOverride = null)
    {
        _cachePath = cachePathOverride ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SendIt", "update-check.json");
    }

    /// <summary>Returns the latest available version if it is newer than <paramref name="currentVersion"/>, otherwise null.</summary>
    public async Task<string?> GetAvailableUpdateAsync(string currentVersion, CancellationToken cancellationToken)
    {
        string? latest;
        try
        {
            latest = await GetLatestVersionAsync(cancellationToken);
        }
        catch
        {
            return null;
        }

        if (latest is null) return null;
        return IsNewer(latest, currentVersion) ? latest : null;
    }

    private async Task<string?> GetLatestVersionAsync(CancellationToken cancellationToken)
    {
        var cache = ReadCache();
        if (cache is not null && DateTimeOffset.UtcNow - cache.CheckedAt < CacheLifetime)
            return cache.LatestVersion;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(RequestTimeout);

        using var client = new HttpClient { Timeout = RequestTimeout };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("sendit-cli");

        var response = await client.GetAsync(ReleasesUrl, cts.Token);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(cts.Token);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("tag_name", out var tagProp)) return null;

        var latestVersion = tagProp.GetString()?.TrimStart('v', 'V');
        WriteCache(latestVersion);
        return latestVersion;
    }

    private static bool IsNewer(string latest, string current)
    {
        if (!Version.TryParse(latest, out var latestVersion)) return false;
        if (!Version.TryParse(current, out var currentVersion)) return false;
        return latestVersion > currentVersion;
    }

    private CachedResult? ReadCache()
    {
        try
        {
            if (!File.Exists(_cachePath)) return null;
            var json = File.ReadAllText(_cachePath);
            return JsonSerializer.Deserialize<CachedResult>(json);
        }
        catch
        {
            return null;
        }
    }

    private void WriteCache(string? latestVersion)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_cachePath)!);
            var payload = new CachedResult(DateTimeOffset.UtcNow, latestVersion);
            File.WriteAllText(_cachePath, JsonSerializer.Serialize(payload));
        }
        catch
        {
            // Best-effort cache; a failed write just means we check again next launch.
        }
    }

    private record CachedResult(DateTimeOffset CheckedAt, string? LatestVersion);
}
