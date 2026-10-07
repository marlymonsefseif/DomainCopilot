using DomainCopilot.Application.Agents.DTOs;
using DomainCopilot.Application.Agents.Guardrails;

namespace DomainCopilot.Application.Agents;

public class WorkOrderGeneratorAgent
{
    public WorkOrder GenerateWorkOrder(
        DiagnosticRequest request,
        DiagnosticPlan plan,
        SafetyValidationResult safetyResult)
    {
        if (!safetyResult.IsValid)
        {
            throw new InvalidOperationException("Cannot generate work order: Safety guardrail validation failed.");
        }

        var workOrderId = $"WO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..18].ToUpperInvariant();

        return new WorkOrder
        {
            WorkOrderId = workOrderId,
            EquipmentId = request.EquipmentId,
            EquipmentType = request.EquipmentType,
            Title = $"Remediation & Service: {plan.SymptomMatch.SuspectedFailure}",
            Priority = request.Priority,
            SafetyValidated = true,
            SafetyCertificateNumber = safetyResult.SafetyCertificateNumber,
            SafetyPrerequisites = safetyResult.EnforcedPrerequisites,
            RequiredTools = new List<string>
            {
                "Calibrated Torque Wrench (10 - 100 Nm)",
                "Bearing Puller & Induction Heater",
                "Dial Indicator / Alignment Gauge",
                "Digital Infrared Thermometer",
                "Calibrated Multimeter (CAT IV)"
            },
            ReplacementParts = new List<string>
            {
                "Matched Bearing Set (per equipment OEM spec)",
                "Elastomer Shaft Coupling Insert",
                "Synthetic Lubricant Cartridge (ISO VG 320)"
            },
            ActionSteps = plan.StepByStepProcedures,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
