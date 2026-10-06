using DomainCopilot.Domain.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCopilot.Application.Documents.DTOs
{
    public class IngestDocumentResponse
    {
        public Guid DocumentId { get; init; }

        public string FileName { get; init; } = string.Empty;

        public DocumentFormat Format { get; init; }

        public DocumentStatus Status { get; init; }

        public int PageCount { get; init; }

        public int CharacterCount { get; init; }

        public int ChunkCount { get; init; }

        public IReadOnlyList<ChunkResponse> Chunks { get; init; }
             = Array.Empty<ChunkResponse>();

        public string? ErrorMessage { get; init; }

    }
}
