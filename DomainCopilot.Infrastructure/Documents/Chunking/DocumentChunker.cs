using DomainCopilot.Application.Documents.DTOs;
using DomainCopilot.Application.Documents.Interfaces;
using DomainCopilot.Domain.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCopilot.Infrastructure.Documents.Chunking
{
    public class DocumentChunker : IDocumentChunker
    {
        private const int MaxChunkCharacters = 1200;
        private const int OverlapCharacters = 150;

        public IReadOnlyList<DocumentChunk> Chunk(
            Guid documentId,
            string source,
            string version,
            IReadOnlyList<ExtractedPage> pages)
        {
            var chunks = new List<DocumentChunk>();

            var chunkIndex = 0;

            foreach (var page in pages)
            {
                if (string.IsNullOrWhiteSpace(page.Text))
                {
                    continue;
                }

                var section = "General";

                var paragraphs = page.Text
                    .Split(
                        new[] { "\n\n" },
                        StringSplitOptions.RemoveEmptyEntries);

                var currentText = new List<string>();

                foreach (var paragraph in paragraphs)
                {
                    var cleanedParagraph = paragraph.Trim();

                    if (string.IsNullOrWhiteSpace(cleanedParagraph))
                    {
                        continue;
                    }

                    if (LooksLikeHeading(cleanedParagraph))
                    {
                        if (currentText.Count > 0)
                        {
                            AddChunk(
                                chunks,
                                documentId,
                                source,
                                version,
                                section,
                                page.PageNumber,
                                string.Join(
                                    Environment.NewLine,
                                    currentText),
                                ref chunkIndex);

                            currentText.Clear();
                        }

                        section = cleanedParagraph;

                        continue;
                    }

                    currentText.Add(cleanedParagraph);

                    var currentLength = string.Join(
                        Environment.NewLine,
                        currentText).Length;

                    if (currentLength >= MaxChunkCharacters)
                    {
                        AddChunk(
                            chunks,
                            documentId,
                            source,
                            version,
                            section,
                            page.PageNumber,
                            string.Join(
                                Environment.NewLine,
                                currentText),
                            ref chunkIndex);

                        var overlapText = GetOverlapText(
                            currentText,
                            OverlapCharacters);

                        currentText.Clear();

                        if (!string.IsNullOrWhiteSpace(overlapText))
                        {
                            currentText.Add(overlapText);
                        }
                    }
                }

                if (currentText.Count > 0)
                {
                    AddChunk(
                        chunks,
                        documentId,
                        source,
                        version,
                        section,
                        page.PageNumber,
                        string.Join(
                            Environment.NewLine,
                            currentText),
                        ref chunkIndex);
                }
            }

            return chunks;
        }

        private static void AddChunk(
            List<DocumentChunk> chunks,
            Guid documentId,
            string source,
            string version,
            string section,
            int pageNumber,
            string text,
            ref int chunkIndex)
        {
            var cleanedText = text.Trim();

            if (string.IsNullOrWhiteSpace(cleanedText))
            {
                return;
            }

            chunks.Add(
                new DocumentChunk(
                    documentId,
                    chunkIndex,
                    cleanedText,
                    source,
                    section,
                    pageNumber,
                    null,
                    version));

            chunkIndex++;
        }

        private static bool LooksLikeHeading(string text)
        {
            if (text.Length > 100)
            {
                return false;
            }

            if (text.EndsWith(".") ||
                text.EndsWith(":") ||
                text.EndsWith(";"))
            {
                return false;
            }

            if (text.StartsWith(
                    "1.") ||
                text.StartsWith(
                    "2.") ||
                text.StartsWith(
                    "3.") ||
                text.StartsWith(
                    "4.") ||
                text.StartsWith(
                    "5."))
            {
                return false;
            }

            return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 10;
        }

        private static string GetOverlapText(
            List<string> paragraphs,
            int overlapCharacters)
        {
            var result = string.Empty;

            for (var i = paragraphs.Count - 1; i >= 0; i--)
            {
                var candidate = paragraphs[i];

                result = string.IsNullOrWhiteSpace(result)
                    ? candidate
                    : candidate + Environment.NewLine + result;

                if (result.Length >= overlapCharacters)
                {
                    break;
                }
            }

            return result.Length > overlapCharacters
                ? result[^overlapCharacters..]
                : result;
        }
    }
}
