using DomainCopilot.Application.Documents.DTOs;
using DomainCopilot.Application.Documents.Services;
using Microsoft.AspNetCore.Mvc;

namespace DomainCopilot.Api.Controllers
{
    [Route("api/documents")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentIngestionService _ingestionService;

        public DocumentsController(
            DocumentIngestionService ingestionService)
        {
            _ingestionService = ingestionService;
        }

        [HttpPost("ingest")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<IngestDocumentResponse>> Ingest(
            IFormFile file,
            [FromForm] string version = "1.0",
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    error = "A non-empty PDF or DOCX file is required."
                });
            }

            await using var stream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();

            await stream.CopyToAsync(memoryStream, cancellationToken);

            var request = new IngestDocumentRequest
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                Content = memoryStream.ToArray(),
                Version = version
            };

            try
            {
                var result = await _ingestionService.IngestAsync(
                    request,
                    cancellationToken);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    error = ex.Message
                });
            }
        }

    }
}

