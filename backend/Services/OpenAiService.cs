using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TraceAI.Api.Services;

public class OpenAiService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiService> logger) : IAiService
{
    private readonly string? _apiKey = configuration["OpenAI:ApiKey"];
    private readonly string _baseUrl = configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";

    public async Task<(string Output, string RawResponse)> CompleteAsync(string prompt, string model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is missing. Set OpenAI:ApiKey.");
        }

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = "You are an expert software engineering assistant." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("OpenAI error: {StatusCode} - {Raw}", response.StatusCode, raw);
            throw new InvalidOperationException($"OpenAI call failed: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(raw);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
                      ?? string.Empty;

        return (content.Trim(), raw);
    }
}
