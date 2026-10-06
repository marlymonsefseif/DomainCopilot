using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCopilot.Application.Documents.DTOs
{
    public class ExtractedDocument
    {
        public string Text { get; init; } = string.Empty;

        public int PageCount { get; init; }

        public IReadOnlyList<ExtractedPage> Pages { get; init; }
            = Array.Empty<ExtractedPage>();
    }
}
