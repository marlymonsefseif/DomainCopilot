namespace DomainCopilot.Application.Agents.DTOs;

public class DiagnosticRequest
{
    public string EquipmentId { get; init; } = string.Empty;
    public string EquipmentType { get; init; } = string.Empty;
    public string ReportedSymptoms { get; init; } = string.Empty;
    public string Priority { get; init; } = "High";
    public Guid? DocumentIdFilter { get; init; }
}

public class SymptomMatchResult
{
    public string SuspectedFailure { get; init; } = string.Empty;
    public float Confidence { get; init; }
    public List<string> MatchedClauses { get; init; } = new();
    public string Rationale { get; init; } = string.Empty;
}

public class SafetyRequirements
{
    public bool RequiresLockoutTagout { get; init; } = true;
    public string IsolationPoint { get; init; } = string.Empty;
    public List<string> RequiredPpe { get; init; } = new();
    public List<string> ZeroEnergyChecks { get; init; } = new();
    public bool SafetyPrerequisitesSatisfied { get; set; }
}

public class DiagnosticPlan
{
    public SymptomMatchResult SymptomMatch { get; init; } = new();
    public SafetyRequirements SafetyRequirements { get; init; } = new();
    public List<string> StepByStepProcedures { get; init; } = new();
}

public class WorkOrder
{
    public string WorkOrderId { get; init; } = string.Empty;
    public string EquipmentId { get; init; } = string.Empty;
    public string EquipmentType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Priority { get; init; } = "High";
    public bool SafetyValidated { get; init; }
    public string SafetyCertificateNumber { get; init; } = string.Empty;
    public List<string> SafetyPrerequisites { get; init; } = new();
    public List<string> RequiredTools { get; init; } = new();
    public List<string> ReplacementParts { get; init; } = new();
    public List<string> ActionSteps { get; init; } = new();
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}

public class OrchestratorResponse
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DiagnosticPlan DiagnosticPlan { get; init; } = new();
    public WorkOrder? GeneratedWorkOrder { get; init; }
    public List<string> Citations { get; init; } = new();
    public bool IsDegraded { get; init; }
}
