using System.Data;
using DomainCopilot.Application.Rag.DTOs;
using DomainCopilot.Application.Rag.Interfaces;
using DomainCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DomainCopilot.Infrastructure.Rag;

public class PostgresHybridRetrievalService : IHybridRetrievalService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IVectorSearchService _vectorSearchService;

    public PostgresHybridRetrievalService(
        ApplicationDbContext dbContext,
        IVectorSearchService vectorSearchService)
    {
        _dbContext = dbContext;
        _vectorSearchService = vectorSearchService;
    }

    public async Task<IReadOnlyList<HybridSearchResult>> SearchAsync(
        HybridSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Array.Empty<HybridSearchResult>();
        }

        var candidateLimit = Math.Max(request.TopK * 3, 20);

        // 1. Run Dense Vector Search
        var denseRequest = new VectorSearchRequest
        {
            Query = request.Query,
            TopK = candidateLimit,
            MinSimilarity = 0.05f,
            DocumentIdFilter = request.DocumentIdFilter,
            VersionFilter = request.VersionFilter
        };

        var denseResults = await _vectorSearchService.SearchAsync(denseRequest, cancellationToken);

        // 2. Run Keyword / Full-Text Search in PostgreSQL
        var keywordResults = await ExecuteKeywordSearchAsync(request.Query, candidateLimit, request.DocumentIdFilter, request.VersionFilter, cancellationToken);

        // 3. Reciprocal Rank Fusion (RRF)
        var k = request.RrfConstantK > 0 ? request.RrfConstantK : 60;
        var denseWeight = request.DenseWeight > 0 ? request.DenseWeight : 0.5f;
        var keywordWeight = request.KeywordWeight > 0 ? request.KeywordWeight : 0.5f;

        var mergedChunks = new Dictionary<Guid, (
            Guid ChunkId,
            Guid DocumentId,
            int ChunkIndex,
            string Text,
            string Source,
            string Section,
            int? PageNumber,
            string? Clause,
            string Version,
            int? DenseRank,
            int? KeywordRank,
            float? DenseScore,
            float? KeywordScore
        )>();

        // Process Dense Ranks
        for (var i = 0; i < denseResults.Count; i++)
        {
            var item = denseResults[i];
            var rank = i + 1; // 1-based rank
            mergedChunks[item.ChunkId] = (
                item.ChunkId,
                item.DocumentId,
                item.ChunkIndex,
                item.Text,
                item.Source,
                item.Section,
                item.PageNumber,
                item.Clause,
                item.Version,
                rank,
                null,
                item.Score,
                null
            );
        }

        // Process Keyword Ranks
        for (var i = 0; i < keywordResults.Count; i++)
        {
            var item = keywordResults[i];
            var rank = i + 1; // 1-based rank

            if (mergedChunks.TryGetValue(item.ChunkId, out var existing))
            {
                mergedChunks[item.ChunkId] = existing with
                {
                    KeywordRank = rank,
                    KeywordScore = item.Score
                };
            }
            else
            {
                mergedChunks[item.ChunkId] = (
                    item.ChunkId,
                    item.DocumentId,
                    item.ChunkIndex,
                    item.Text,
                    item.Source,
                    item.Section,
                    item.PageNumber,
                    item.Clause,
                    item.Version,
                    null,
                    rank,
                    null,
                    item.Score
                );
            }
        }

        // Compute Fused RRF Score for each candidate
        var fusedResults = new List<HybridSearchResult>();
        foreach (var candidate in mergedChunks.Values)
        {
            float rrfDense = candidate.DenseRank.HasValue
                ? denseWeight / (k + candidate.DenseRank.Value)
                : 0.0f;

            float rrfKeyword = candidate.KeywordRank.HasValue
                ? keywordWeight / (k + candidate.KeywordRank.Value)
                : 0.0f;

            var fusedScore = rrfDense + rrfKeyword;

            if (fusedScore >= request.MinScore)
            {
                fusedResults.Add(new HybridSearchResult
                {
                    ChunkId = candidate.ChunkId,
                    DocumentId = candidate.DocumentId,
                    ChunkIndex = candidate.ChunkIndex,
                    Text = candidate.Text,
                    Source = candidate.Source,
                    Section = candidate.Section,
                    PageNumber = candidate.PageNumber,
                    Clause = candidate.Clause,
                    Version = candidate.Version,
                    DenseRank = candidate.DenseRank,
                    KeywordRank = candidate.KeywordRank,
                    DenseScore = candidate.DenseScore,
                    KeywordScore = candidate.KeywordScore,
                    FusedScore = fusedScore
                });
            }
        }

        // Order by FusedScore descending and return top K
        return fusedResults
            .OrderByDescending(r => r.FusedScore)
            .Take(request.TopK)
            .ToList();
    }

    private async Task<List<(Guid ChunkId, Guid DocumentId, int ChunkIndex, string Text, string Source, string Section, int? PageNumber, string? Clause, string Version, float Score)>>
        ExecuteKeywordSearchAsync(
            string query,
            int topK,
            Guid? documentId,
            string? version,
            CancellationToken cancellationToken)
    {
        var results = new List<(Guid, Guid, int, string, string, string, int?, string?, string, float)>();

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        const string sql = @"
            SELECT ""Id"", ""DocumentId"", ""ChunkIndex"", ""Text"", ""Source"", ""Section"", ""PageNumber"", ""Clause"", ""Version"",
                   ts_rank_cd(to_tsvector('english', ""Text""), plainto_tsquery('english', @query)) AS ""Score""
            FROM ""DocumentChunks""
            WHERE to_tsvector('english', ""Text"") @@ plainto_tsquery('english', @query)
              AND (@documentId IS NULL OR ""DocumentId"" = @documentId)
              AND (@version IS NULL OR ""Version"" = @version)
            ORDER BY ""Score"" DESC
            LIMIT @topK;
        ";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        command.Parameters.Add(new NpgsqlParameter("@query", NpgsqlTypes.NpgsqlDbType.Text) { Value = query });
        command.Parameters.Add(new NpgsqlParameter("@documentId", NpgsqlTypes.NpgsqlDbType.Uuid)
        {
            Value = documentId.HasValue ? documentId.Value : DBNull.Value
        });
        command.Parameters.Add(new NpgsqlParameter("@version", NpgsqlTypes.NpgsqlDbType.Varchar)
        {
            Value = string.IsNullOrWhiteSpace(version) ? DBNull.Value : version
        });
        command.Parameters.Add(new NpgsqlParameter("@topK", NpgsqlTypes.NpgsqlDbType.Integer) { Value = topK });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add((
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetInt32(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.GetString(8),
                reader.GetFloat(9)
            ));
        }

        return results;
    }
}
