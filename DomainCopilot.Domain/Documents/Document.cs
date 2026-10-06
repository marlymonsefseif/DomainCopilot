namespace DomainCopilot.Domain.Documents;

public class Document
{
    public Guid Id { get; private set; }

    public string FileName { get; private set; }

    public string Source { get; private set; }

    public DocumentFormat Format { get; private set; }

    public string Version { get; private set; }

    public DocumentStatus Status { get; private set; }

    public string ContentHash { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public string? ErrorMessage { get; private set; }

    private Document()
    {
        FileName = string.Empty;
        Source = string.Empty;
        Version = string.Empty;
        ContentHash = string.Empty;
    }

    public Document(
        string fileName,
        string source,
        DocumentFormat format,
        string version,
        string contentHash)
    {
        Id = Guid.NewGuid();

        FileName = fileName;
        Source = source;
        Format = format;
        Version = version;
        ContentHash = contentHash;

        Status = DocumentStatus.Pending;

        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkProcessing()
    {
        Status = DocumentStatus.Processing;
        ErrorMessage = null;
    }

    public void MarkProcessed()
    {
        Status = DocumentStatus.Processed;
        ProcessedAtUtc = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = DocumentStatus.Failed;
        ErrorMessage = errorMessage;
    }
}