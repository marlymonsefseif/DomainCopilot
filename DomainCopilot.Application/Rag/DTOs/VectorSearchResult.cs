namespace DomainCopilot.Application.Rag.DTOs;

public class VectorSearchResult
{
    public Guid ChunkId { get; init; }

    public Guid DocumentId { get; init; }

    public int ChunkIndex { get; init; }

    public string Text { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string Section { get; init; } = string.Empty;

    public int? PageNumber { get; init; }

    public string? Clause { get; init; }

    public string Version { get; init; } = string.Empty;

    public float Score { get; init; }

    public Citation ToCitation()
    {
        return new Citation
        {
            ChunkId = ChunkId,
            DocumentId = DocumentId,
            Source = Source,
            Section = Section,
            PageNumber = PageNumber,
            Clause = Clause,
            Version = Version,
            Snippet = Text.Length > 300 ? Text[..300] + "..." : Text,
            RelevanceScore = Score
        };
    }
}
