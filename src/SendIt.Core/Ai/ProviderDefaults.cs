using SendIt.Core.Configuration;

namespace SendIt.Core.Ai;

/// <summary>Sensible default executable/arguments per CLI-shell provider, used by the configure UI.</summary>
public static class ProviderDefaults
{
    public static (string Executable, string Arguments) CliDefaults(AiProviderKind kind) => kind switch
    {
        AiProviderKind.ClaudeCli => ("claude", "-p"),
        AiProviderKind.CodexCli => ("codex", "exec"),
        AiProviderKind.KiroCli => ("kiro", "-p"),
        AiProviderKind.OpenCode => ("opencode", "run"),
        _ => ("", "")
    };
}
