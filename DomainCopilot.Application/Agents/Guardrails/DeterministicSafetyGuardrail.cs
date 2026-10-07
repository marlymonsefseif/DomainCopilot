using DomainCopilot.Application.Agents.DTOs;

namespace DomainCopilot.Application.Agents.Guardrails;

public class SafetyValidationResult
{
    public bool IsValid { get; init; }
    public string SafetyCertificateNumber { get; init; } = string.Empty;
    public List<string> Violations { get; init; } = new();
    public List<string> EnforcedPrerequisites { get; init; } = new();
}

public class DeterministicSafetyGuardrail
{
    private static readonly HashSet<string> HighRiskMachineryTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "pump", "motor", "mill", "cnc", "turbine", "compressor", "gearbox", "press", "conveyor"
    };

    public SafetyValidationResult ValidateAndEnforce(
        string equipmentType,
        SafetyRequirements requirements)
    {
        var violations = new List<string>();
        var enforcedPrerequisites = new List<string>();

        bool isHighRisk = HighRiskMachineryTypes.Any(t => equipmentType.Contains(t, StringComparison.OrdinalIgnoreCase));

        // 1. Structural Check: Lockout-Tagout (LOTO)
        if (isHighRisk && !requirements.RequiresLockoutTagout)
        {
            violations.Add("VIOLATION: Rotating/electrical machinery requires mandatory Lockout-Tagout (LOTO).");
        }
        else if (requirements.RequiresLockoutTagout)
        {
            if (string.IsNullOrWhiteSpace(requirements.IsolationPoint))
            {
                violations.Add("VIOLATION: LOTO is declared but specific Isolation Point is not defined.");
            }
            else
            {
                enforcedPrerequisites.Add($"Lockout-Tagout verified at isolation point: {requirements.IsolationPoint}");
            }
        }

        // 2. Structural Check: PPE Mandatory Baseline
        if (requirements.RequiredPpe.Count == 0)
        {
            violations.Add("VIOLATION: No personal protective equipment (PPE) specified for hazardous field task.");
        }
        else
        {
            enforcedPrerequisites.Add($"PPE Verified: {string.Join(", ", requirements.RequiredPpe)}");
        }

        // 3. Structural Check: Zero-Energy Verification
        if (requirements.ZeroEnergyChecks.Count == 0)
        {
            violations.Add("VIOLATION: Zero-energy state physical verification step is missing prior to intervention.");
        }
        else
        {
            enforcedPrerequisites.Add($"Zero-Energy Verification: {string.Join("; ", requirements.ZeroEnergyChecks)}");
        }

        bool isValid = violations.Count == 0;
        requirements.SafetyPrerequisitesSatisfied = isValid;

        return new SafetyValidationResult
        {
            IsValid = isValid,
            SafetyCertificateNumber = isValid ? $"SAFE-CERT-{Guid.NewGuid():N}"[..18].ToUpperInvariant() : string.Empty,
            Violations = violations,
            EnforcedPrerequisites = enforcedPrerequisites
        };
    }
}
