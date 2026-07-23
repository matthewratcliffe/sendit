using SendIt.Core.Configuration;

namespace SendIt.Core.Ai;

public static class AiProviderFactory
{
    public static IAiProvider Create(AiSettings settings) => settings.Provider switch
    {
        AiProviderKind.OpenAiCompatible => new OpenAiCompatibleProvider(settings),
        AiProviderKind.Ollama => new OllamaProvider(settings),
        AiProviderKind.ClaudeCli => new CliShellProvider(settings),
        AiProviderKind.CodexCli => new CliShellProvider(settings),
        AiProviderKind.KiroCli => new CliShellProvider(settings),
        AiProviderKind.OpenCode => new CliShellProvider(settings),
        AiProviderKind.CustomCommand => new CliShellProvider(settings),
        AiProviderKind.LmStudio => new OpenAiCompatibleProvider(settings),
        AiProviderKind.LlamaCpp => new OpenAiCompatibleProvider(settings),
        _ => throw new NotSupportedException($"Unsupported AI provider: {settings.Provider}")
    };
}
