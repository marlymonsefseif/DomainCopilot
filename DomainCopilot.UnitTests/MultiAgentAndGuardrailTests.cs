using DomainCopilot.Application.Agents;
using DomainCopilot.Application.Agents.DTOs;
using DomainCopilot.Application.Agents.Guardrails;
using Xunit;

namespace DomainCopilot.UnitTests;

public class MultiAgentAndGuardrailTests
{
    private readonly DeterministicSafetyGuardrail _guardrail = new();

    [Fact]
    public void DeterministicSafetyGuardrail_Rejects_HighRiskMachineryWithoutLoto()
    {
        var requirements = new SafetyRequirements
        {
            RequiresLockoutTagout = false, // VIOLATION
            IsolationPoint = "",
            RequiredPpe = new List<string> { "Safety Glasses" },
            ZeroEnergyChecks = new List<string> { "Verify zero voltage" }
        };

        var result = _guardrail.ValidateAndEnforce("Centrifugal Slurry Pump", requirements);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Violations);
        Assert.Contains(result.Violations, v => v.Contains("Lockout-Tagout"));
    }

    [Fact]
    public void DeterministicSafetyGuardrail_Rejects_MissingPpeOrZeroEnergy()
    {
        var requirements = new SafetyRequirements
        {
            RequiresLockoutTagout = true,
            IsolationPoint = "Main Circuit Breaker #4",
            RequiredPpe = new List<string>(), // VIOLATION
            ZeroEnergyChecks = new List<string>() // VIOLATION
        };

        var result = _guardrail.ValidateAndEnforce("Industrial Mill", requirements);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Violations.Count);
    }

    [Fact]
    public void DeterministicSafetyGuardrail_Approves_WhenPrerequisitesSatisfied()
    {
        var requirements = new SafetyRequirements
        {
            RequiresLockoutTagout = true,
            IsolationPoint = "Breaker MCC-02 & Valve V-101",
            RequiredPpe = new List<string> { "Safety Glasses", "Steel-Toed Boots", "Cut-Resistant Gloves" },
            ZeroEnergyChecks = new List<string> { "Check multimeter 0V", "Check pressure 0 PSI" }
        };

        var result = _guardrail.ValidateAndEnforce("Centrifugal Pump", requirements);

        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
        Assert.StartsWith("SAFE-CERT-", result.SafetyCertificateNumber);
        Assert.Equal(3, result.EnforcedPrerequisites.Count);
    }

    [Fact]
    public void WorkOrderGenerator_EmitsCertificateAndPrerequisites()
    {
        var generator = new WorkOrderGeneratorAgent();
        var request = new DiagnosticRequest
        {
            EquipmentId = "PUMP-01",
            EquipmentType = "Pump",
            ReportedSymptoms = "Grinding noise",
            Priority = "High"
        };

        var plan = new DiagnosticPlan
        {
            SymptomMatch = new SymptomMatchResult
            {
                SuspectedFailure = "Bearing Seizure"
            },
            StepByStepProcedures = new List<string> { "Step 1: Isolate", "Step 2: Replace" }
        };

        var safetyResult = new SafetyValidationResult
        {
            IsValid = true,
            SafetyCertificateNumber = "SAFE-CERT-TEST1234",
            EnforcedPrerequisites = new List<string> { "LOTO verified" }
        };

        var workOrder = generator.GenerateWorkOrder(request, plan, safetyResult);

        Assert.NotNull(workOrder);
        Assert.True(workOrder.SafetyValidated);
        Assert.Equal("SAFE-CERT-TEST1234", workOrder.SafetyCertificateNumber);
        Assert.NotEmpty(workOrder.RequiredTools);
        Assert.NotEmpty(workOrder.ReplacementParts);
    }

    [Fact]
    public void WorkOrderGenerator_ThrowsException_IfSafetyGuardrailFailed()
    {
        var generator = new WorkOrderGeneratorAgent();
        var request = new DiagnosticRequest();
        var plan = new DiagnosticPlan();
        var failedSafety = new SafetyValidationResult { IsValid = false };

        Assert.Throws<InvalidOperationException>(() =>
            generator.GenerateWorkOrder(request, plan, failedSafety));
    }
}
