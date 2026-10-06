namespace DomainCopilot.Application.Documents.DTOs
{
    public class ChunkResponse
    {
        public int ChunkIndex { get; init; }

        public string Text { get; init; } = string.Empty;

        public string Source { get; init; } = string.Empty;

        public string Section { get; init; } = string.Empty;

        public int? PageNumber { get; init; }

        public string? Clause { get; init; }

        public string Version { get; init; } = string.Empty;
    }
}


