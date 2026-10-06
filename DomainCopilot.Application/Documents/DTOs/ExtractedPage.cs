using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCopilot.Application.Documents.DTOs
{
    public class ExtractedPage
    {
        public int PageNumber { get; init; }

        public string Text { get; init; } = string.Empty;
    }
}
