namespace DomainCopilot.Domain.Documents
{
    public class DocumentChunk
    {
        public Guid Id { get; private set; }

        public Guid DocumentId { get; private set; }

        public int ChunkIndex { get; private set; }

        public string Text { get; private set; }

        public string Source { get; private set; }

        public string Section { get; private set; }

        public int? PageNumber { get; private set; }

        public string? Clause { get; private set; }

        public string Version { get; private set; }

        private DocumentChunk()
        {
            Text = string.Empty;
            Source = string.Empty;
            Section = string.Empty;
            Version = string.Empty;
        }

        public DocumentChunk(
            Guid documentId,
            int chunkIndex,
            string text,
            string source,
            string section,
            int? pageNumber,
            string? clause,
            string version)
        {
            Id = Guid.NewGuid();

            DocumentId = documentId;

            ChunkIndex = chunkIndex;

            Text = text;

            Source = source;

            Section = section;

            PageNumber = pageNumber;

            Clause = clause;

            Version = version;
        }
    }
}

