namespace DomainCopilot.Application.Rag.DTOs;

public class HybridSearchRequest
{
    public string Query { get; init; } = string.Empty;

    public int TopK { get; init; } = 5;

    public float DenseWeight { get; init; } = 0.5f;

    public float KeywordWeight { get; init; } = 0.5f;

    public int RrfConstantK { get; init; } = 60;

    public float MinScore { get; init; } = 0.005f;

    public Guid? DocumentIdFilter { get; init; }

    public string? VersionFilter { get; init; }
}
