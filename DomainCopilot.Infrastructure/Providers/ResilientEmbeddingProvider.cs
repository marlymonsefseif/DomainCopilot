using DomainCopilot.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace DomainCopilot.Infrastructure.Providers;

public class ResilientEmbeddingProvider : IEmbeddingProvider
{
    private readonly HostedEmbeddingProvider _hostedProvider;
    private readonly LocalEmbeddingProvider _localProvider;
    private readonly ILogger<ResilientEmbeddingProvider> _logger;

    public string ProviderName => IsDegraded ? "Local/Degraded" : "Hosted";
    public bool IsDegraded { get; private set; }

    public ResilientEmbeddingProvider(
        HostedEmbeddingProvider hostedProvider,
        LocalEmbeddingProvider localProvider,
        ILogger<ResilientEmbeddingProvider> logger)
    {
        _hostedProvider = hostedProvider;
        _localProvider = localProvider;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var results = await GenerateEmbeddingsAsync(new[] { text }, cancellationToken);
        return results[0];
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        try
        {
            var hostedResults = await _hostedProvider.GenerateEmbeddingsAsync(texts, cancellationToken);
            IsDegraded = false;
            return hostedResults;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Hosted embedding provider failed or not configured. Automatically switching to degraded/offline embedding provider (Twist T2).");
            IsDegraded = true;
            return await _localProvider.GenerateEmbeddingsAsync(texts, cancellationToken);
        }
    }
}
