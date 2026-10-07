using DomainCopilot.Application.Rag.DTOs;
using DomainCopilot.Domain.Documents;
using DomainDocumentFormat = DomainCopilot.Domain.Documents.DocumentFormat;
using DomainCopilot.Infrastructure.Data;
using DomainCopilot.Infrastructure.Providers;
using DomainCopilot.Infrastructure.Rag;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DomainCopilot.IntegrationTests;

public class HybridRetrievalIntegrationTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=domaincopilot;Username=postgres;Password=123456";

    [Fact]
    public async Task PostgresHybridRetrieval_ReturnsFusedResultsWithBothRanks()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);

        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient();
        var embeddingProvider = new LocalEmbeddingProvider(httpClient, config, NullLogger<LocalEmbeddingProvider>.Instance);

        // Seed document and chunk with specific domain keywords
        var doc = new Document("Gearbox_Overhaul.pdf", "Gearbox_Overhaul.pdf", DomainDocumentFormat.Pdf, "2.0", Guid.NewGuid().ToString("N"));
        doc.MarkProcessed();
        await dbContext.Documents.AddAsync(doc);

        var chunkText = "Planetary gearbox oil change requires synthetic polyglycol ISO VG 320 lubricant and magnetic plug inspection.";
        var chunk = new DocumentChunk(doc.Id, 0, chunkText, "Gearbox_Overhaul.pdf", "Lubrication Specs", 8, "Spec 4.1", "2.0");

        var vector = await embeddingProvider.GenerateEmbeddingAsync(chunkText);
        chunk.SetEmbedding(vector);
        await dbContext.DocumentChunks.AddAsync(chunk);

        await dbContext.SaveChangesAsync();

        // Perform hybrid search
        var vectorSearch = new PostgresVectorSearchService(dbContext, embeddingProvider);
        var hybridService = new PostgresHybridRetrievalService(dbContext, vectorSearch);

        var hybridRequest = new HybridSearchRequest
        {
            Query = "planetary gearbox synthetic lubricant oil change",
            TopK = 3,
            DenseWeight = 0.5f,
            KeywordWeight = 0.5f,
            DocumentIdFilter = doc.Id
        };

        var results = await hybridService.SearchAsync(hybridRequest);

        Assert.NotEmpty(results);
        var top = results[0];
        Assert.Equal(chunk.Id, top.ChunkId);
        Assert.True(top.FusedScore > 0, "Fused score must be positive");
        Assert.NotNull(top.KeywordRank);

        var citation = top.ToCitation();
        Assert.Equal("Gearbox_Overhaul.pdf", citation.Source);
        Assert.Equal("Lubrication Specs", citation.Section);
        Assert.Contains("polyglycol", citation.Snippet);
    }
}
