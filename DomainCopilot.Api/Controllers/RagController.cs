using DomainCopilot.Application.Rag.DTOs;
using DomainCopilot.Application.Rag.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DomainCopilot.Api.Controllers;

[Route("api/rag")]
[ApiController]
public class RagController : ControllerBase
{
    private readonly IVectorSearchService _vectorSearchService;

    public RagController(IVectorSearchService vectorSearchService)
    {
        _vectorSearchService = vectorSearchService;
    }

    [HttpPost("search")]
    public async Task<ActionResult<RagSearchResponse>> Search(
        [FromBody] VectorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Search query cannot be empty." });
        }

        var results = await _vectorSearchService.SearchAsync(request, cancellationToken);

        if (results.Count == 0)
        {
            return Ok(new RagSearchResponse
            {
                Query = request.Query,
                Refusal = true,
                Message = "Not enough information in the corpus.",
                Citations = Array.Empty<Citation>()
            });
        }

        var citations = results.Select(r => r.ToCitation()).ToList();

        return Ok(new RagSearchResponse
        {
            Query = request.Query,
            Refusal = false,
            ResultCount = results.Count,
            Citations = citations
        });
    }
}

public class RagSearchResponse
{
    public string Query { get; init; } = string.Empty;

    public bool Refusal { get; init; }

    public string? Message { get; init; }

    public int ResultCount { get; init; }

    public IReadOnlyList<Citation> Citations { get; init; } = Array.Empty<Citation>();
}
