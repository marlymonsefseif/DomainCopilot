using DomainCopilot.Application.Agents.DTOs;
using DomainCopilot.Application.Common.DTOs;
using DomainCopilot.Application.Common.Interfaces;

namespace DomainCopilot.Application.Agents;

public class DiagnosticPlannerAgent
{
    private readonly ILlmProvider _llmProvider;

    public DiagnosticPlannerAgent(ILlmProvider llmProvider)
    {
        _llmProvider = llmProvider;
    }

    public async Task<DiagnosticPlan> PlanDiagnosticAsync(
        DiagnosticRequest request,
        SymptomMatchResult matchResult,
        CancellationToken cancellationToken = default)
    {
        var prompt = $@"As an Industrial Safety & Reliability Planner, formulate an actionable diagnostic procedure for:
Equipment: {request.EquipmentId} ({request.EquipmentType})
Suspected Failure: {matchResult.SuspectedFailure}
Symptoms: {request.ReportedSymptoms}

Formulate:
1. Physical isolation & safety prerequisites
2. Step-by-step diagnostic actions";

        var completion = await _llmProvider.CompleteAsync(new LlmCompletionRequest
        {
            Messages = new List<ChatMessage> { ChatMessage.User(prompt) },
            Temperature = 0.2f
        }, cancellationToken);

        // Formulate safety requirements with mandatory industry defaults for industrial equipment
        var safety = new SafetyRequirements
        {
            RequiresLockoutTagout = true,
            IsolationPoint = $"Main Electrical Breaker & Supply Valve for {request.EquipmentId}",
            RequiredPpe = new List<string>
            {
                "Safety Glasses with Side Shields",
                "Cut-Resistant Gloves (Level 3)",
                "Steel-Toed Boots",
                "Hearing Protection"
            },
            ZeroEnergyChecks = new List<string>
            {
                "Verify electrical zero voltage via calibrated multimeter at motor terminals",
                "Depressurize hydraulic/fluid lines and confirm gauge reads 0 PSI",
                "Allow thermal surface cooldown below 40 deg C before tactile inspection"
            }
        };

        var procedures = new List<string>
        {
            "Step 1: Execute Lockout-Tagout (LOTO) at designated isolation point.",
            "Step 2: Physically verify zero-energy state (voltage, pressure, thermal).",
            "Step 3: Remove protective guard and inspect coupling / bearing housing for heat discoloration or metal debris.",
            $"Step 4: Conduct diagnostic evaluation for suspected {matchResult.SuspectedFailure}.",
            "Step 5: Record baseline vibration / backlash measurements and compare against manual tolerances."
        };

        return new DiagnosticPlan
        {
            SymptomMatch = matchResult,
            SafetyRequirements = safety,
            StepByStepProcedures = procedures
        };
    }
}
