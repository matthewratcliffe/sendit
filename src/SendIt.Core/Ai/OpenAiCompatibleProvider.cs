using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SendIt.Core.Configuration;

namespace SendIt.Core.Ai;

/// <summary>Targets any OpenAI-compatible /chat/completions endpoint (OpenAI, LM Studio, llama.cpp server, etc).</summary>
public class OpenAiCompatibleProvider : IAiProvider
{
    private readonly AiSettings _settings;
    private readonly HttpClient _httpClient;

    public string Name => "OpenAI Compatible";

    public OpenAiCompatibleProvider(AiSettings settings, HttpClient? httpClient = null)
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
                    temperature = request.Temperature,
                    max_tokens = request.MaxTokens,
                    messages = new object[]
                    {
                        new { role = "system", content = request.SystemPrompt },
                        new { role = "user", content = request.UserPrompt }
                    }
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                    $"{_settings.Endpoint.TrimEnd('/')}/v1/chat/completions")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrEmpty(_settings.ApiKey))
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    lastError = $"HTTP {(int)response.StatusCode}: {body}";
                    continue;
                }

                using var doc = JsonDocument.Parse(body);
                var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
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
            using var response = await _httpClient.GetAsync($"{_settings.Endpoint.TrimEnd('/')}/v1/models", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
