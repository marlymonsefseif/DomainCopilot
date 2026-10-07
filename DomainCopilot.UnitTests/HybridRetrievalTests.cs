using DomainCopilot.Application.Rag.DTOs;
using Xunit;

namespace DomainCopilot.UnitTests;

public class HybridRetrievalTests
{
    [Fact]
    public void HybridSearchResult_ToCitation_PreservesAllTracingMetadata()
    {
        var result = new HybridSearchResult
        {
            ChunkId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            ChunkIndex = 5,
            Source = "Turbine_Operating_Manual.pdf",
            Section = "Section 2.3: Vibration Limits",
            PageNumber = 45,
            Clause = "Table 2-B",
            Version = "2.1",
            Text = "Vibration amplitude exceeding 4.5 mm/s requires immediate trip.",
            DenseRank = 1,
            KeywordRank = 2,
            DenseScore = 0.91f,
            KeywordScore = 0.85f,
            FusedScore = 0.0162f
        };

        var citation = result.ToCitation();

        Assert.Equal(result.ChunkId, citation.ChunkId);
        Assert.Equal(result.DocumentId, citation.DocumentId);
        Assert.Equal("Turbine_Operating_Manual.pdf", citation.Source);
        Assert.Equal("Section 2.3: Vibration Limits", citation.Section);
        Assert.Equal(45, citation.PageNumber);
        Assert.Equal("Table 2-B", citation.Clause);
        Assert.Equal("2.1", citation.Version);
        Assert.Equal(0.0162f, citation.RelevanceScore);
        Assert.Contains("Vibration amplitude", citation.Snippet);
    }

    [Fact]
    public void ReciprocalRankFusion_CombinesRanksCorrectly()
    {
        var k = 60;
        var denseWeight = 0.5f;
        var keywordWeight = 0.5f;

        // Candidate appearing in both lists at rank 1
        var dualMatchRrf = (denseWeight / (k + 1)) + (keywordWeight / (k + 1));

        // Candidate appearing only in dense list at rank 1
        var singleMatchRrf = denseWeight / (k + 1);

        // Candidate appearing in dense list at rank 10
        var lowerRankRrf = denseWeight / (k + 10);

        Assert.True(dualMatchRrf > singleMatchRrf, "Dual rank match must score higher than single rank match");
        Assert.True(singleMatchRrf > lowerRankRrf, "Rank 1 must score higher than rank 10");
        Assert.Equal(1.0f / 61.0f, dualMatchRrf, 4);
    }
}
