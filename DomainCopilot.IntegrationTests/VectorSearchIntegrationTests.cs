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

public class VectorSearchIntegrationTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=domaincopilot;Username=postgres;Password=123456";

    [Fact]
    public async Task PostgresVectorSearch_FindsRelevantChunk_AndReturnsCitation()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);

        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient();
        var embeddingProvider = new LocalEmbeddingProvider(httpClient, config, NullLogger<LocalEmbeddingProvider>.Instance);

        // Seed a test document and chunk
        var doc = new Document("CNC_Maintenance_Guide.pdf", "CNC_Maintenance_Guide.pdf", DomainDocumentFormat.Pdf, "1.0", Guid.NewGuid().ToString("N"));
        doc.MarkProcessed();
        await dbContext.Documents.AddAsync(doc);

        var chunkText = "To service spindle bearings on the CNC mill, first engage lockout-tagout on the main power isolator.";
        var chunk = new DocumentChunk(doc.Id, 0, chunkText, "CNC_Maintenance_Guide.pdf", "Spindle Maintenance", 12, "Item 3.1", "1.0");

        var vector = await embeddingProvider.GenerateEmbeddingAsync(chunkText);
        chunk.SetEmbedding(vector);
        await dbContext.DocumentChunks.AddAsync(chunk);

        await dbContext.SaveChangesAsync();

        // Search with a semantic query
        var searchService = new PostgresVectorSearchService(dbContext, embeddingProvider);
        var searchRequest = new VectorSearchRequest
        {
            Query = "How to service CNC spindle bearings and safety lock",
            TopK = 3,
            MinSimilarity = 0.1f,
            DocumentIdFilter = doc.Id
        };

        var results = await searchService.SearchAsync(searchRequest);

        Assert.NotEmpty(results);
        var topResult = results[0];
        Assert.Equal(chunk.Id, topResult.ChunkId);
        Assert.True(topResult.Score > 0.3f, $"Expected score > 0.3, got {topResult.Score}");

        var citation = topResult.ToCitation();
        Assert.Equal("CNC_Maintenance_Guide.pdf", citation.Source);
        Assert.Equal("Spindle Maintenance", citation.Section);
        Assert.Equal(12, citation.PageNumber);
        Assert.Contains("lockout-tagout", citation.Snippet);
    }
}
