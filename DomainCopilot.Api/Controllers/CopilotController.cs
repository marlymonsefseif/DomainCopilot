using DomainCopilot.Application.Common.DTOs;
using DomainCopilot.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DomainCopilot.Api.Controllers;

[Route("api/copilot")]
[ApiController]
public class CopilotController : ControllerBase
{
    private readonly ILlmProvider _llmProvider;
    private readonly IEmbeddingProvider _embeddingProvider;

    public CopilotController(
        ILlmProvider llmProvider,
        IEmbeddingProvider embeddingProvider)
    {
        _llmProvider = llmProvider;
        _embeddingProvider = embeddingProvider;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            llmProvider = _llmProvider.ProviderName,
            isLlmDegraded = _llmProvider.IsDegraded,
            embeddingProvider = _embeddingProvider.ProviderName,
            twistT2OfflineSupported = true
        });
    }

    [HttpPost("complete")]
    public async Task<ActionResult<LlmCompletionResponse>> Complete(
        [FromBody] LlmCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Messages.Count == 0)
        {
            return BadRequest(new { error = "At least one message is required." });
        }

        var response = await _llmProvider.CompleteAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("stream")]
    public async Task Stream(
        [FromQuery] string prompt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Response.StatusCode = 400;
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        var request = new LlmCompletionRequest
        {
            Messages = new List<ChatMessage> { ChatMessage.User(prompt) }
        };

        await foreach (var token in _llmProvider.StreamAsync(request, cancellationToken))
        {
            await Response.WriteAsync($"data: {token}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
