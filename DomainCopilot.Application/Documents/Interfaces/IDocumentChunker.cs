using DomainCopilot.Application.Documents.DTOs;
using DomainCopilot.Domain.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCopilot.Application.Documents.Interfaces
{
    public interface IDocumentChunker
    {
        IReadOnlyList<DocumentChunk> Chunk(
            Guid documentId,
            string source,
            string version,
            IReadOnlyList<ExtractedPage> pages);
    }
}
