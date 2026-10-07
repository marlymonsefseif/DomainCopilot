using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using DomainCopilot.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DomainCopilot.Infrastructure.Providers;

public class LocalEmbeddingProvider : IEmbeddingProvider
{
    private const int EmbeddingDimensions = 384;
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocalEmbeddingProvider> _logger;
    private readonly string _endpoint;
    private readonly string _model;

    public string ProviderName => "Local/Degraded";

    public LocalEmbeddingProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LocalEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _endpoint = configuration["Embeddings:Local:Endpoint"] ?? "http://localhost:11434/api/embeddings";
        _model = configuration["Embeddings:Local:Model"] ?? "nomic-embed-text";
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var results = await GenerateEmbeddingsAsync(new[] { text }, cancellationToken);
        return results[0];
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>();

        foreach (var text in texts)
        {
            float[]? embedding = null;

            try
            {
                var payload = new
                {
                    model = _model,
                    prompt = text
                };

                using var response = await _httpClient.PostAsJsonAsync(_endpoint, payload, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: cancellationToken);
                    if (ollamaResponse?.Embedding != null && ollamaResponse.Embedding.Length > 0)
                    {
                        embedding = NormalizeVector(ollamaResponse.Embedding);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Ollama local embedding endpoint unavailable. Falling back to offline deterministic embedding.");
            }

            // Fallback to deterministic offline embedding
            embedding ??= GenerateOfflineDeterministicEmbedding(text);
            results.Add(embedding);
        }

        return results;
    }

    private static float[] GenerateOfflineDeterministicEmbedding(string text)
    {
        var vector = new float[EmbeddingDimensions];
        if (string.IsNullOrWhiteSpace(text))
        {
            return vector;
        }

        var words = text.ToLowerInvariant().Split(new[] { ' ', '\r', '\n', '\t', ',', '.', ';', ':', '-', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(word));
            for (var i = 0; i < hashBytes.Length; i += 4)
            {
                var index = Math.Abs(BitConverter.ToInt32(hashBytes, i)) % EmbeddingDimensions;
                vector[index] += 1.0f;
            }
        }

        return NormalizeVector(vector);
    }

    private static float[] NormalizeVector(float[] vector)
    {
        var sumSquares = 0.0;
        for (var i = 0; i < vector.Length; i++)
        {
            sumSquares += vector[i] * vector[i];
        }

        var norm = (float)Math.Sqrt(sumSquares);
        if (norm > 0)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }
        }

        return vector;
    }

    private class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
