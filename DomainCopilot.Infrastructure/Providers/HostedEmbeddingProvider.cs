using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DomainCopilot.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DomainCopilot.Infrastructure.Providers;

public class HostedEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HostedEmbeddingProvider> _logger;
    private readonly string _model;
    private readonly string _endpoint;
    private readonly string _apiKey;

    public string ProviderName => "Hosted";

    public HostedEmbeddingProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<HostedEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _endpoint = configuration["Embeddings:Hosted:Endpoint"] ?? "https://api.openai.com/v1/embeddings";
        _model = configuration["Embeddings:Hosted:Model"] ?? "text-embedding-3-small";
        _apiKey = configuration["Embeddings:Hosted:ApiKey"] ?? string.Empty;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var results = await GenerateEmbeddingsAsync(new[] { text }, cancellationToken);
        return results[0];
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Hosted embedding API key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var payload = new
        {
            model = _model,
            input = texts
        };

        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>(cancellationToken: cancellationToken);
        if (result?.Data == null || result.Data.Count == 0)
        {
            throw new InvalidOperationException("Empty response from hosted embedding provider.");
        }

        return result.Data.OrderBy(d => d.Index).Select(d => d.Embedding).ToList();
    }

    private class OpenAiEmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<OpenAiEmbeddingData> Data { get; set; } = new();
    }

    private class OpenAiEmbeddingData
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
