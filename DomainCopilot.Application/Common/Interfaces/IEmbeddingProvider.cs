namespace DomainCopilot.Application.Common.Interfaces;

public interface IEmbeddingProvider
{
    string ProviderName { get; }

    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
