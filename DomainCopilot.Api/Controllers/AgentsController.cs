using DomainCopilot.Application.Agents;
using DomainCopilot.Application.Agents.DTOs;
using DomainCopilot.Application.Agents.Guardrails;
using DomainCopilot.Application.Auth.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DomainCopilot.Api.Controllers;

[Route("api/agents")]
[ApiController]
[Authorize(Roles = $"{UserRoles.Technician},{UserRoles.Supervisor}")]
public class AgentsController : ControllerBase
{
    private readonly MaintenanceCopilotOrchestrator _orchestrator;
    private readonly DeterministicSafetyGuardrail _guardrail;

    public AgentsController(
        MaintenanceCopilotOrchestrator orchestrator,
        DeterministicSafetyGuardrail guardrail)
    {
        _orchestrator = orchestrator;
        _guardrail = guardrail;
    }

    [HttpPost("diagnose")]
    public async Task<ActionResult<OrchestratorResponse>> Diagnose(
        [FromBody] DiagnosticRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.EquipmentId) || string.IsNullOrWhiteSpace(request.ReportedSymptoms))
        {
            return BadRequest(new { error = "EquipmentId and ReportedSymptoms are required." });
        }

        var result = await _orchestrator.ExecuteDiagnosticWorkflowAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("verify-safety")]
    public ActionResult<SafetyValidationResult> VerifySafety(
        [FromQuery] string equipmentType,
        [FromBody] SafetyRequirements requirements)
    {
        var result = _guardrail.ValidateAndEnforce(equipmentType, requirements);
        return Ok(result);
    }
}
