namespace DomainCopilot.Application.Rag.DTOs;

public class VectorSearchRequest
{
    public string Query { get; init; } = string.Empty;

    public int TopK { get; init; } = 5;

    public float MinSimilarity { get; init; } = 0.25f;

    public Guid? DocumentIdFilter { get; init; }

    public string? VersionFilter { get; init; }
}
