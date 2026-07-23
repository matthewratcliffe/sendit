namespace SendIt.Core.Configuration;

public class SenditConfig
{
    public GeneralSettings General { get; set; } = new();
    public AiSettings Ai { get; set; } = new();
    public GitSettings Git { get; set; } = new();
    public TestSettings Tests { get; set; } = new();
    public AdvancedSettings Advanced { get; set; } = new();
}

public class GeneralSettings
{
    public string DefaultBranchPrefix { get; set; } = "feature/";
    public string DefaultTicketType { get; set; } = "";
    public bool AutoStageFiles { get; set; } = true;
    public bool AutoPush { get; set; } = false;
    public string ColourTheme { get; set; } = "Auto";
    public bool VerboseLogging { get; set; } = false;
}

public enum AiProviderKind
{
    OpenAiCompatible,
    Ollama,
    ClaudeCli,
    CodexCli,
    KiroCli,
    OpenCode,
    LmStudio,
    LlamaCpp,
    CustomCommand
}

public class AiSettings
{
    public AiProviderKind Provider { get; set; } = AiProviderKind.ClaudeCli;
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "";
    /// <summary>Encrypted (DPAPI) at rest; see ConfigManager.</summary>
    public string ApiKey { get; set; } = "";
    public double Temperature { get; set; } = 0.2;
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxTokens { get; set; } = 512;
    public int RetryCount { get; set; } = 2;
    public string SystemPrompt { get; set; } =
        "You are a commit message generator. Reply with a single Conventional Commit message only. " +
        "No markdown, no explanation, no ticket references.";
    public string PromptTemplate { get; set; } =
        "Branch: {branch}\nTicket: {ticket}\nStatus:\n{status}\nDiff:\n{diff}\nRecent commits:\n{log}\n";
    /// <summary>Executable used by ClaudeCli/CodexCli/OpenCode/CustomCommand providers.</summary>
    public string CommandExecutable { get; set; } = "claude";
    public string CommandArguments { get; set; } = "-p";
}

public class GitSettings
{
    public List<string> AllowedBranchPrefixes { get; set; } = new()
    {
        "feature/", "bugfix/", "fix/", "hotfix/", "release/", "chore/",
        "docs/", "ci/", "refactor/", "test/", "infrastructure/"
    };
    public List<string> TicketPatterns { get; set; } = new()
    {
        "[A-Za-z][A-Za-z0-9]*-\\d+"
    };
    public int LargeCommitFileThreshold { get; set; } = 50;
    public int LargeCommitLineThreshold { get; set; } = 2000;
    public long LargeCommitMaxFileSizeBytes { get; set; } = 100L * 1024 * 1024;
    public int LargeCommitMaxBinaryCount { get; set; } = 20;
}

public class TestSettings
{
    public List<string> Commands { get; set; } = new();
    public bool StopOnFailure { get; set; } = true;
}

public class AdvancedSettings
{
    public string LogLevel { get; set; } = "Info";
    public int LogRetainedFileCount { get; set; } = 10;
    public bool RequireTicket { get; set; } = false;
}
