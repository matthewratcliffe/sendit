using System.Text;
using System.Text.Json;
using SendIt.Core.Configuration;

namespace SendIt.Core.Ai;

public class OllamaProvider : IAiProvider
{
    private readonly AiSettings _settings;
    private readonly HttpClient _httpClient;

    public string Name => "Ollama";

    public OllamaProvider(AiSettings settings, HttpClient? httpClient = null)
    {
        _settings = settings;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds));
    }

    public async Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var attempts = Math.Max(1, _settings.RetryCount + 1);
        string? lastError = null;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                var payload = new
                {
                    model = _settings.Model,
                    stream = false,
                    options = new { temperature = request.Temperature, num_predict = request.MaxTokens },
                    messages = new object[]
                    {
                        new { role = "system", content = request.SystemPrompt },
                        new { role = "user", content = request.UserPrompt }
                    }
                };

                using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(
                    $"{_settings.Endpoint.TrimEnd('/')}/api/chat", content, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    lastError = $"HTTP {(int)response.StatusCode}: {body}";
                    continue;
                }

                using var doc = JsonDocument.Parse(body);
                var text = doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
                return new AiResponse(true, text.Trim(), null);
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }
        }

        return new AiResponse(false, "", lastError ?? "Unknown error");
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{_settings.Endpoint.TrimEnd('/')}/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
