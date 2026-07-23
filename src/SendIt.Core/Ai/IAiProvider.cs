namespace SendIt.Core.Ai;

public record AiRequest(string SystemPrompt, string UserPrompt, double Temperature, int MaxTokens);

public record AiResponse(bool Success, string Text, string? Error);

public interface IAiProvider
{
    string Name { get; }

    Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default);

    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
