using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DomainCopilot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {

        [HttpGet]
        public ActionResult<HealthCheckResponse> GetHealth()
        {
            var response = new HealthCheckResponse
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow
            };
            return Ok(response);
        }
    } 


    public class HealthCheckResponse
    {
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
