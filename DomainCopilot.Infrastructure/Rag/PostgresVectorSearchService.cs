using System.Data;
using DomainCopilot.Application.Common.Interfaces;
using DomainCopilot.Application.Rag.DTOs;
using DomainCopilot.Application.Rag.Interfaces;
using DomainCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DomainCopilot.Infrastructure.Rag;

public class PostgresVectorSearchService : IVectorSearchService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmbeddingProvider _embeddingProvider;

    public PostgresVectorSearchService(
        ApplicationDbContext dbContext,
        IEmbeddingProvider embeddingProvider)
    {
        _dbContext = dbContext;
        _embeddingProvider = embeddingProvider;
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        VectorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Array.Empty<VectorSearchResult>();
        }

        var queryEmbedding = await _embeddingProvider.GenerateEmbeddingAsync(request.Query, cancellationToken);
        var results = new List<VectorSearchResult>();

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        const string sql = @"
            SELECT ""Id"", ""DocumentId"", ""ChunkIndex"", ""Text"", ""Source"", ""Section"", ""PageNumber"", ""Clause"", ""Version"",
                   cosine_similarity(""Embedding"", @queryVector) AS ""Score""
            FROM ""DocumentChunks""
            WHERE ""Embedding"" IS NOT NULL
              AND (@documentId IS NULL OR ""DocumentId"" = @documentId)
              AND (@version IS NULL OR ""Version"" = @version)
              AND cosine_similarity(""Embedding"", @queryVector) >= @minSimilarity
            ORDER BY ""Score"" DESC
            LIMIT @topK;
        ";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var queryVectorParam = new NpgsqlParameter("@queryVector", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Real)
        {
            Value = queryEmbedding
        };
        command.Parameters.Add(queryVectorParam);

        var docIdParam = new NpgsqlParameter("@documentId", NpgsqlTypes.NpgsqlDbType.Uuid)
        {
            Value = request.DocumentIdFilter.HasValue ? request.DocumentIdFilter.Value : DBNull.Value
        };
        command.Parameters.Add(docIdParam);

        var versionParam = new NpgsqlParameter("@version", NpgsqlTypes.NpgsqlDbType.Varchar)
        {
            Value = string.IsNullOrWhiteSpace(request.VersionFilter) ? DBNull.Value : request.VersionFilter
        };
        command.Parameters.Add(versionParam);

        var minSimParam = new NpgsqlParameter("@minSimilarity", NpgsqlTypes.NpgsqlDbType.Real)
        {
            Value = request.MinSimilarity
        };
        command.Parameters.Add(minSimParam);

        var topKParam = new NpgsqlParameter("@topK", NpgsqlTypes.NpgsqlDbType.Integer)
        {
            Value = request.TopK
        };
        command.Parameters.Add(topKParam);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new VectorSearchResult
            {
                ChunkId = reader.GetGuid(0),
                DocumentId = reader.GetGuid(1),
                ChunkIndex = reader.GetInt32(2),
                Text = reader.GetString(3),
                Source = reader.GetString(4),
                Section = reader.GetString(5),
                PageNumber = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                Clause = reader.IsDBNull(7) ? null : reader.GetString(7),
                Version = reader.GetString(8),
                Score = reader.GetFloat(9)
            });
        }

        return results;
    }
}
