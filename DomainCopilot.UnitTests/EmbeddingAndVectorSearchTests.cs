using DomainCopilot.Application.Rag.DTOs;
using DomainCopilot.Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DomainCopilot.UnitTests;

public class EmbeddingAndVectorSearchTests
{
    [Fact]
    public async Task LocalEmbeddingProvider_GeneratesNormalizedVectors()
    {
        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient();
        var provider = new LocalEmbeddingProvider(httpClient, config, NullLogger<LocalEmbeddingProvider>.Instance);

        var text = "Centrifugal pump bearing overheating due to insufficient lubrication";
        var embedding = await provider.GenerateEmbeddingAsync(text);

        Assert.NotNull(embedding);
        Assert.Equal(384, embedding.Length);

        // Verify unit norm (length approximately 1.0)
        var norm = Math.Sqrt(embedding.Sum(x => x * x));
        Assert.True(Math.Abs(norm - 1.0) < 0.01, $"Expected norm ~1.0, got {norm}");
    }

    [Fact]
    public async Task ResilientEmbeddingProvider_FallsBackToLocal_WhenHostedFails()
    {
        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient();

        var hosted = new HostedEmbeddingProvider(httpClient, config, NullLogger<HostedEmbeddingProvider>.Instance);
        var local = new LocalEmbeddingProvider(httpClient, config, NullLogger<LocalEmbeddingProvider>.Instance);
        var resilient = new ResilientEmbeddingProvider(hosted, local, NullLogger<ResilientEmbeddingProvider>.Instance);

        var text = "Emergency shutdown procedure for hydraulic press";
        var embedding = await resilient.GenerateEmbeddingAsync(text);

        Assert.NotNull(embedding);
        Assert.Equal(384, embedding.Length);
        Assert.True(resilient.IsDegraded);
        Assert.Equal("Local/Degraded", resilient.ProviderName);
    }

    [Fact]
    public void VectorSearchResult_ToCitation_FormatsTraceableCitation()
    {
        var result = new VectorSearchResult
        {
            ChunkId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            ChunkIndex = 2,
            Source = "Centrifugal_Pump_Manual_Rev3.pdf",
            Section = "Section 4: Safety & Lubrication",
            PageNumber = 14,
            Clause = "Clause 4.2",
            Version = "Rev 3.0",
            Text = "Verify oil level in bearing housing before operating pump.",
            Score = 0.89f
        };

        var citation = result.ToCitation();

        Assert.Equal(result.ChunkId, citation.ChunkId);
        Assert.Equal(result.DocumentId, citation.DocumentId);
        Assert.Equal("Centrifugal_Pump_Manual_Rev3.pdf", citation.Source);
        Assert.Equal("Section 4: Safety & Lubrication", citation.Section);
        Assert.Equal(14, citation.PageNumber);
        Assert.Equal("Clause 4.2", citation.Clause);
        Assert.Equal("Rev 3.0", citation.Version);
        Assert.Equal(0.89f, citation.RelevanceScore);
        Assert.Contains("Verify oil level", citation.Snippet);
    }
}
