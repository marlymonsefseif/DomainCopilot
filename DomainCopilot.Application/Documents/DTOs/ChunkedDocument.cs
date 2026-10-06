using DomainCopilot.Domain.Documents;

namespace DomainCopilot.Application.Documents.DTOs
{
    public class ChunkedDocument
    {
        public Guid DocumentId { get; init; }

        public IReadOnlyList<DocumentChunk> Chunks { get; init; }
            = Array.Empty<DocumentChunk>();
    }
}

