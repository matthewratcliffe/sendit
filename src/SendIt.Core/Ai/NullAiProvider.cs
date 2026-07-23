namespace SendIt.Core.Ai;

/// <summary>No-op provider used when no AI provider could be constructed; always reports unavailable.</summary>
public class NullAiProvider : IAiProvider
{
    public string Name => "None";

    public Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new AiResponse(false, "", "No AI provider configured."));

    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
