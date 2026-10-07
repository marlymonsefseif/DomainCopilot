using DomainCopilot.Application.Rag.DTOs;

namespace DomainCopilot.Application.Rag.Interfaces;

public interface IVectorSearchService
{
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        VectorSearchRequest request,
        CancellationToken cancellationToken = default);
}
