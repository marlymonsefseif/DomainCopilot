using DomainCopilot.Application.Documents.DTOs;
using DomainCopilot.Application.Documents.Interfaces;
using DomainDocumentFormat = DomainCopilot.Domain.Documents.DocumentFormat;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace DomainCopilot.Infrastructure.Documents.Extraction
{
    public class PdfDocumentExtractor : IDocumentExtractor
    {
        public DomainDocumentFormat SupportedFormat => DomainDocumentFormat.Pdf;
        public async Task<ExtractedDocument> ExtractAsync(
            Stream content,
            CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream();

            await content.CopyToAsync(memoryStream, cancellationToken);

            memoryStream.Position = 0;

            using var pdf = PdfDocument.Open(memoryStream);

            var pages = new List<ExtractedPage>();

            foreach (var page in pdf.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var text = ContentOrderTextExtractor.GetText(page);

                pages.Add(new ExtractedPage
                {
                    PageNumber = page.Number,
                    Text = text
                });
            }

            return new ExtractedDocument
            {
                Text = string.Join(
                    Environment.NewLine,
                    pages.Select(x => x.Text)),
                PageCount = pages.Count,
                Pages = pages
            };
        }
    }
}
