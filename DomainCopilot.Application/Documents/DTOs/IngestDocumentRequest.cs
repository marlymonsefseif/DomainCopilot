using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCopilot.Application.Documents.DTOs
{
    public class IngestDocumentRequest
    {
        public string FileName { get; init; } = string.Empty;

        public string ContentType { get; init; } = string.Empty;

        public byte[] Content { get; init; } = Array.Empty<byte>();

        public string Version { get; init; } = "1.0";
    }
}
