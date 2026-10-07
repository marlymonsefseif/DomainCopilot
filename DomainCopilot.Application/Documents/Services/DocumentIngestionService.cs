using DomainCopilot.Application.Common.Interfaces;
using DomainCopilot.Application.Documents.DTOs;
using DomainCopilot.Application.Documents.Interfaces;
using DomainCopilot.Domain.Documents;

namespace DomainCopilot.Application.Documents.Services
{
    public class DocumentIngestionService
    {
        private readonly IEnumerable<IDocumentExtractor> _extractors;
        private readonly ITextCleaner _textCleaner;
        private readonly IDocumentRepository _repository;
        private readonly IDocumentHashCalculator _hashCalculator;
        private readonly IDocumentChunker _chunker;
        private readonly IEmbeddingProvider _embeddingProvider;

        public DocumentIngestionService(
            IEnumerable<IDocumentExtractor> extractors,
            ITextCleaner textCleaner,
            IDocumentRepository repository,
            IDocumentHashCalculator hashCalculator,
            IDocumentChunker chunker,
            IEmbeddingProvider embeddingProvider)
        {
            _extractors = extractors;
            _textCleaner = textCleaner;
            _repository = repository;
            _hashCalculator = hashCalculator;
            _chunker = chunker;
            _embeddingProvider = embeddingProvider;
        }

        public async Task<IngestDocumentResponse> IngestAsync(
            IngestDocumentRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.Content.Length == 0)
            {
                throw new ArgumentException(
                    "Document content is empty.");
            }

            var extension = Path.GetExtension(
                request.FileName)
                .ToLowerInvariant();

            var format = extension switch
            {
                ".pdf" => DocumentFormat.Pdf,
                ".docx" => DocumentFormat.Docx,
                _ => throw new ArgumentException(
                    "Only PDF and DOCX files are supported.")
            };

            var contentHash =
                _hashCalculator.Calculate(request.Content);

            var existingDocument =
                await _repository.GetByContentHashAsync(
                    contentHash,
                    cancellationToken);

            if (existingDocument != null)
            {
                return new IngestDocumentResponse
                {
                    DocumentId = existingDocument.Id,
                    FileName = existingDocument.FileName,
                    Format = existingDocument.Format,
                    Status = existingDocument.Status,
                    ErrorMessage = existingDocument.ErrorMessage
                };
            }

            var document = new Document(
                request.FileName,
                request.FileName,
                format,
                request.Version,
                contentHash);

            await _repository.AddAsync(
                document,
                cancellationToken);

            document.MarkProcessing();

            await _repository.UpdateAsync(
                document,
                cancellationToken);

            try
            {
                var extractor = _extractors
                    .FirstOrDefault(x =>
                        x.SupportedFormat == format);

                if (extractor == null)
                {
                    throw new InvalidOperationException(
                        $"No extractor registered for {format}.");
                }

                await using var stream =
                    new MemoryStream(request.Content);

                var extracted = await extractor.ExtractAsync(
                    stream,
                    cancellationToken);

                var cleanedPages = extracted.Pages
                    .Select(page => new ExtractedPage
                    {
                        PageNumber = page.PageNumber,
                        Text = _textCleaner.Clean(page.Text)
                    })
                    .Where(page =>
                        !string.IsNullOrWhiteSpace(page.Text))
                    .ToList();

                var cleanedText =
                    !string.IsNullOrWhiteSpace(extracted.Text)
                        ? _textCleaner.Clean(extracted.Text)
                        : string.Join(
                            Environment.NewLine,
                            cleanedPages.Select(x => x.Text));

                if (string.IsNullOrWhiteSpace(cleanedText))
                {
                    throw new InvalidOperationException(
                        "No text could be extracted from the document.");
                }

                var chunks = _chunker.Chunk(
                    document.Id,
                    document.Source,
                    document.Version,
                    cleanedPages);

                if (chunks.Count > 0)
                {
                    var chunkTexts = chunks.Select(c => c.Text).ToList();
                    var embeddings = await _embeddingProvider.GenerateEmbeddingsAsync(
                        chunkTexts,
                        cancellationToken);

                    for (var i = 0; i < chunks.Count; i++)
                    {
                        if (i < embeddings.Count)
                        {
                            chunks[i].SetEmbedding(embeddings[i]);
                        }
                    }

                    await _repository.AddChunksAsync(
                        chunks,
                        cancellationToken);
                }

                var chunkResponses = chunks
                    .Select(chunk => new ChunkResponse
                    {
                        ChunkIndex = chunk.ChunkIndex,
                        Text = chunk.Text,
                        Source = chunk.Source,
                        Section = chunk.Section,
                        PageNumber = chunk.PageNumber,
                        Clause = chunk.Clause,
                        Version = chunk.Version
                    })
                    .ToList();

                document.MarkProcessed();

                await _repository.UpdateAsync(
                    document,
                    cancellationToken);

                return new IngestDocumentResponse
                {
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    Format = document.Format,
                    Status = document.Status,
                    PageCount = extracted.PageCount,
                    CharacterCount = cleanedText.Length,
                    ChunkCount = chunks.Count,
                    Chunks = chunkResponses
                };
            }
            catch (Exception ex)
            {
                document.MarkFailed(ex.Message);

                await _repository.UpdateAsync(
                    document,
                    cancellationToken);

                return new IngestDocumentResponse
                {
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    Format = document.Format,
                    Status = document.Status,
                    ErrorMessage = document.ErrorMessage
                };
            }
        }
    }
}

