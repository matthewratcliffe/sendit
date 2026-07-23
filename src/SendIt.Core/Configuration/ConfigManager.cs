using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SendIt.Core.Configuration;

/// <summary>
/// Loads and merges user (%USERPROFILE%\.sendit.json) and repository (&lt;repo&gt;\.sendit.json)
/// configuration. Repository configuration always overrides user configuration.
/// API keys are encrypted at rest with Windows DPAPI when running on Windows; on other
/// platforms they are stored in plain text with a warning, since no equivalent OS keystore
/// is universally available without extra dependencies.
/// </summary>
public class ConfigManager
{
    private const string ApiKeyPrefix = "dpapi:";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string UserConfigPath { get; }
    public string RepoConfigPath { get; }

    public ConfigManager(string repositoryRoot, string? userConfigPathOverride = null)
    {
        UserConfigPath = userConfigPathOverride ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".sendit.json");
        RepoConfigPath = Path.Combine(repositoryRoot, ".sendit.json");
    }

    public SenditConfig Load()
    {
        var config = new SenditConfig();
        ApplyFile(config, UserConfigPath);
        ApplyFile(config, RepoConfigPath);
        DecryptApiKey(config);
        return config;
    }

    private static void ApplyFile(SenditConfig target, string path)
    {
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path);
        var overrideConfig = JsonSerializer.Deserialize<SenditConfig>(json, JsonOptions);
        if (overrideConfig is null) return;
        Merge(target, overrideConfig);
    }

    private static void Merge(SenditConfig target, SenditConfig source)
    {
        target.General = source.General ?? target.General;
        target.Ai = source.Ai ?? target.Ai;
        target.Git = source.Git ?? target.Git;
        target.Tests = source.Tests ?? target.Tests;
        target.Advanced = source.Advanced ?? target.Advanced;
    }

    public void SaveUser(SenditConfig config) => Save(config, UserConfigPath);

    public void SaveRepo(SenditConfig config)
    {
        // Repository configuration is committed to source control and must never contain secrets.
        var clone = JsonSerializer.Deserialize<SenditConfig>(
            JsonSerializer.Serialize(config, JsonOptions), JsonOptions)!;
        clone.Ai.ApiKey = string.Empty;
        Save(clone, RepoConfigPath);
    }

    private static void Save(SenditConfig config, string path)
    {
        var clone = JsonSerializer.Deserialize<SenditConfig>(
            JsonSerializer.Serialize(config, JsonOptions), JsonOptions)!;
        EncryptApiKey(clone);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(clone, JsonOptions));
    }

    public static void Reset(string repositoryRoot)
    {
        var manager = new ConfigManager(repositoryRoot);
        if (File.Exists(manager.UserConfigPath)) File.Delete(manager.UserConfigPath);
    }

    private static void EncryptApiKey(SenditConfig config)
    {
        if (string.IsNullOrEmpty(config.Ai.ApiKey) || config.Ai.ApiKey.StartsWith(ApiKeyPrefix))
            return;

        if (OperatingSystem.IsWindows())
        {
            var protectedBytes = ProtectApiKey(config.Ai.ApiKey);
            config.Ai.ApiKey = ApiKeyPrefix + Convert.ToBase64String(protectedBytes);
        }
    }

    private static void DecryptApiKey(SenditConfig config)
    {
        if (string.IsNullOrEmpty(config.Ai.ApiKey) || !config.Ai.ApiKey.StartsWith(ApiKeyPrefix))
            return;

        if (!OperatingSystem.IsWindows())
        {
            config.Ai.ApiKey = string.Empty;
            return;
        }

        var payload = config.Ai.ApiKey[ApiKeyPrefix.Length..];
        config.Ai.ApiKey = UnprotectApiKey(Convert.FromBase64String(payload));
    }

    [SupportedOSPlatform("windows")]
    private static byte[] ProtectApiKey(string apiKey)
    {
        var plain = Encoding.UTF8.GetBytes(apiKey);
        return ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
    }

    [SupportedOSPlatform("windows")]
    private static string UnprotectApiKey(byte[] cipher)
    {
        var plain = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plain);
    }
}
