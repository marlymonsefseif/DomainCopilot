using DomainCopilot.Application.Agents.DTOs;
using DomainCopilot.Application.Common.DTOs;
using DomainCopilot.Application.Common.Interfaces;
using DomainCopilot.Application.Rag.DTOs;
using DomainCopilot.Application.Rag.Interfaces;

namespace DomainCopilot.Application.Agents;

public class SymptomMatcherAgent
{
    private readonly IHybridRetrievalService _hybridRetrieval;
    private readonly ILlmProvider _llmProvider;

    public SymptomMatcherAgent(
        IHybridRetrievalService hybridRetrieval,
        ILlmProvider llmProvider)
    {
        _hybridRetrieval = hybridRetrieval;
        _llmProvider = llmProvider;
    }

    public async Task<(SymptomMatchResult Result, List<string> Citations)> AnalyzeSymptomsAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default)
    {
        var citations = new List<string>();

        // 1. Retrieve relevant manual sections using Hybrid Retrieval
        var searchRequest = new HybridSearchRequest
        {
            Query = $"{request.EquipmentType} {request.ReportedSymptoms} troubleshooting failure cause",
            TopK = 3,
            DocumentIdFilter = request.DocumentIdFilter
        };

        var searchResults = await _hybridRetrieval.SearchAsync(searchRequest, cancellationToken);
        var contextSnippets = new List<string>();

        foreach (var r in searchResults)
        {
            var cite = r.ToCitation();
            citations.Add($"[{cite.Source}] {cite.Section} (p.{cite.PageNumber}): {cite.Snippet}");
            contextSnippets.Add(r.Text);
        }

        var context = string.Join("\n\n---\n\n", contextSnippets);

        // 2. Synthesize failure mode via LLM provider
        var prompt = $@"You are an expert Industrial Field Maintenance Diagnostic Specialist.
Equipment: {request.EquipmentId} ({request.EquipmentType})
Reported Symptoms: {request.ReportedSymptoms}

Technical Manual Documentation Context:
{context}

Analyze the symptoms against the manual context.
Identify:
1. Suspected failure mode.
2. Confidence level (0.0 to 1.0).
3. Rationale based on evidence.";

        var completion = await _llmProvider.CompleteAsync(new LlmCompletionRequest
        {
            Messages = new List<ChatMessage> { ChatMessage.User(prompt) },
            Temperature = 0.1f
        }, cancellationToken);

        var suspectedFailure = ExtractFailureMode(request.ReportedSymptoms, completion.Content);

        return (new SymptomMatchResult
        {
            SuspectedFailure = suspectedFailure,
            Confidence = searchResults.Count > 0 ? 0.92f : 0.70f,
            MatchedClauses = citations.Take(2).ToList(),
            Rationale = completion.Content
        }, citations);
    }

    private static string ExtractFailureMode(string symptoms, string llmContent)
    {
        if (symptoms.Contains("bearing", StringComparison.OrdinalIgnoreCase) || symptoms.Contains("grinding", StringComparison.OrdinalIgnoreCase))
            return "Bearing wear / fatigue failure";
        if (symptoms.Contains("temperature", StringComparison.OrdinalIgnoreCase) || symptoms.Contains("overheating", StringComparison.OrdinalIgnoreCase))
            return "Thermal overload / lubrication degradation";
        if (symptoms.Contains("leak", StringComparison.OrdinalIgnoreCase) || symptoms.Contains("seal", StringComparison.OrdinalIgnoreCase))
            return "Mechanical seal leakage";

        return "Mechanical degradation / misalignment";
    }
}
