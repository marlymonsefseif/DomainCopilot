using DomainCopilot.Application.Common.DTOs;

namespace DomainCopilot.Application.Common.Interfaces;

public interface ILlmProvider
{
    string ProviderName { get; }

    bool IsDegraded { get; }

    Task<LlmCompletionResponse> CompleteAsync(
        LlmCompletionRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamAsync(
        LlmCompletionRequest request,
        CancellationToken cancellationToken = default);

    Task<LlmToolCallResponse> CallWithToolsAsync(
        LlmToolCallRequest request,
        CancellationToken cancellationToken = default);
}
