using DomainCopilot.Application.Rag.DTOs;

namespace DomainCopilot.Application.Rag.Interfaces;

public interface IHybridRetrievalService
{
    Task<IReadOnlyList<HybridSearchResult>> SearchAsync(
        HybridSearchRequest request,
        CancellationToken cancellationToken = default);
}
