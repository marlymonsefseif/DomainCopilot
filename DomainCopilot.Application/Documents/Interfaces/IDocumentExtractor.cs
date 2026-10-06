using DomainCopilot.Application.Documents.DTOs;
using DomainCopilot.Domain.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCopilot.Application.Documents.Interfaces
{
    public interface IDocumentExtractor
    {
        DocumentFormat SupportedFormat { get; }

        Task<ExtractedDocument> ExtractAsync(
            Stream content,
            CancellationToken cancellationToken = default);
    }
}
