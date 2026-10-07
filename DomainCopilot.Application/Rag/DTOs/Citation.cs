namespace DomainCopilot.Application.Rag.DTOs;

public class Citation
{
    public Guid ChunkId { get; init; }

    public Guid DocumentId { get; init; }

    public string Source { get; init; } = string.Empty;

    public string Section { get; init; } = string.Empty;

    public int? PageNumber { get; init; }

    public string? Clause { get; init; }

    public string Version { get; init; } = string.Empty;

    public string Snippet { get; init; } = string.Empty;

    public float RelevanceScore { get; init; }
}
