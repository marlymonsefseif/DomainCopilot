using DocumentFormat.OpenXml.Packaging;
using DomainCopilot.Application.Documents.DTOs;
using DomainCopilot.Application.Documents.Interfaces;
using DomainDocumentFormat = DomainCopilot.Domain.Documents.DocumentFormat;
using DomainCopilot.Domain.Documents;

namespace DomainCopilot.Infrastructure.Documents.Extraction;

public class DocxDocumentExtractor : IDocumentExtractor
{
    public DomainDocumentFormat SupportedFormat => DomainDocumentFormat.Docx;

    public async Task<ExtractedDocument> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();

        await content.CopyToAsync(memoryStream, cancellationToken);

        memoryStream.Position = 0;

        using var document = WordprocessingDocument.Open(
            memoryStream,
            false);

        var body = document.MainDocumentPart?
            .Document?
            .Body;

        if (body == null)
        {
            return new ExtractedDocument();
        }

        var paragraphs = body
            .Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
            .Select(x => x.Text)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var text = string.Join(
            Environment.NewLine,
            paragraphs);

        return new ExtractedDocument
        {
            Text = text,
            PageCount = 0,
            Pages = Array.Empty<ExtractedPage>()
        };
    }
}