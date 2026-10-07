using System.Runtime.CompilerServices;
using DomainCopilot.Application.Common.DTOs;
using DomainCopilot.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace DomainCopilot.Infrastructure.Providers;

public class ResilientLlmProvider : ILlmProvider
{
    private readonly HostedLlmProvider _hostedProvider;
    private readonly LocalLlmProvider _localProvider;
    private readonly ILogger<ResilientLlmProvider> _logger;

    public string ProviderName => IsDegraded ? _localProvider.ProviderName : _hostedProvider.ProviderName;
    public bool IsDegraded { get; private set; }

    public ResilientLlmProvider(
        HostedLlmProvider hostedProvider,
        LocalLlmProvider localProvider,
        ILogger<ResilientLlmProvider> logger)
    {
        _hostedProvider = hostedProvider;
        _localProvider = localProvider;
        _logger = logger;
    }

    public async Task<LlmCompletionResponse> CompleteAsync(
        LlmCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _hostedProvider.CompleteAsync(request, cancellationToken);
            IsDegraded = false;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Hosted LLM provider failed or unconfigured. Automatically switching to degraded/offline LLM provider (Twist T2).");
            IsDegraded = true;
            return await _localProvider.CompleteAsync(request, cancellationToken);
        }
    }

    public async IAsyncEnumerable<string> StreamAsync(
        LlmCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerator<string>? enumerator = null;
        bool fallbackToLocal = false;

        try
        {
            var hostedStream = _hostedProvider.StreamAsync(request, cancellationToken);
            enumerator = hostedStream.GetAsyncEnumerator(cancellationToken);
            if (!await enumerator.MoveNextAsync())
            {
                fallbackToLocal = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hosted LLM streaming failed. Falling back to local/degraded stream.");
            fallbackToLocal = true;
        }

        if (!fallbackToLocal && enumerator != null)
        {
            IsDegraded = false;
            await using (enumerator)
            {
                yield return enumerator.Current;
                while (await enumerator.MoveNextAsync())
                {
                    yield return enumerator.Current;
                }
            }
        }
        else
        {
            IsDegraded = true;
            if (enumerator != null)
            {
                await enumerator.DisposeAsync();
            }
            await foreach (var chunk in _localProvider.StreamAsync(request, cancellationToken))
            {
                yield return chunk;
            }
        }
    }

    public async Task<LlmToolCallResponse> CallWithToolsAsync(
        LlmToolCallRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _hostedProvider.CallWithToolsAsync(request, cancellationToken);
            IsDegraded = false;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Hosted LLM tool calling failed or unconfigured. Automatically switching to degraded/offline tool caller (Twist T2).");
            IsDegraded = true;
            return await _localProvider.CallWithToolsAsync(request, cancellationToken);
        }
    }
}
