using DomainCopilot.Application.Agents.DTOs;
using DomainCopilot.Application.Agents.Guardrails;
using DomainCopilot.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace DomainCopilot.Application.Agents;

public class MaintenanceCopilotOrchestrator
{
    private readonly SymptomMatcherAgent _symptomMatcher;
    private readonly DiagnosticPlannerAgent _plannerAgent;
    private readonly DeterministicSafetyGuardrail _safetyGuardrail;
    private readonly WorkOrderGeneratorAgent _workOrderAgent;
    private readonly ILlmProvider _llmProvider;
    private readonly ILogger<MaintenanceCopilotOrchestrator> _logger;

    public MaintenanceCopilotOrchestrator(
        SymptomMatcherAgent symptomMatcher,
        DiagnosticPlannerAgent plannerAgent,
        DeterministicSafetyGuardrail safetyGuardrail,
        WorkOrderGeneratorAgent workOrderAgent,
        ILlmProvider llmProvider,
        ILogger<MaintenanceCopilotOrchestrator> logger)
    {
        _symptomMatcher = symptomMatcher;
        _plannerAgent = plannerAgent;
        _safetyGuardrail = safetyGuardrail;
        _workOrderAgent = workOrderAgent;
        _llmProvider = llmProvider;
        _logger = logger;
    }

    public async Task<OrchestratorResponse> ExecuteDiagnosticWorkflowAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating Multi-Agent Diagnostic Workflow for Equipment: {EquipmentId}", request.EquipmentId);

        // 1. Agent 1: Symptom Matcher (Hybrid RAG + LLM analysis)
        var (symptomMatch, citations) = await _symptomMatcher.AnalyzeSymptomsAsync(request, cancellationToken);

        // 2. Agent 2: Diagnostic & Safety Planner
        var plan = await _plannerAgent.PlanDiagnosticAsync(request, symptomMatch, cancellationToken);

        // 3. Deterministic Safety Guardrail (C# Structural Code Enforcement)
        var safetyValidation = _safetyGuardrail.ValidateAndEnforce(request.EquipmentType, plan.SafetyRequirements);

        if (!safetyValidation.IsValid)
        {
            _logger.LogWarning("Deterministic safety guardrail blocked execution: {Violations}", string.Join("; ", safetyValidation.Violations));
            return new OrchestratorResponse
            {
                Success = false,
                ErrorMessage = $"Safety Guardrail Violation: {string.Join(" ", safetyValidation.Violations)}",
                DiagnosticPlan = plan,
                GeneratedWorkOrder = null,
                Citations = citations,
                IsDegraded = _llmProvider.IsDegraded
            };
        }

        // 4. Agent 3: Work Order Generator
        var workOrder = _workOrderAgent.GenerateWorkOrder(request, plan, safetyValidation);

        _logger.LogInformation("Multi-Agent Workflow complete. Work order emitted: {WorkOrderId} under Safety Certificate: {Cert}",
            workOrder.WorkOrderId, workOrder.SafetyCertificateNumber);

        return new OrchestratorResponse
        {
            Success = true,
            DiagnosticPlan = plan,
            GeneratedWorkOrder = workOrder,
            Citations = citations,
            IsDegraded = _llmProvider.IsDegraded
        };
    }
}
