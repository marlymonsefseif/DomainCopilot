using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using DomainCopilot.Application.Common.DTOs;
using DomainCopilot.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DomainCopilot.Infrastructure.Providers;

public class LocalLlmProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocalLlmProvider> _logger;
    private readonly string _endpoint;
    private readonly string _model;

    public string ProviderName => "Local/Degraded";
    public bool IsDegraded => true;

    public LocalLlmProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LocalLlmProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _endpoint = configuration["Llm:Local:Endpoint"] ?? "http://localhost:11434/api/chat";
        _model = configuration["Llm:Local:Model"] ?? "llama3.2";
    }

    public async Task<LlmCompletionResponse> CompleteAsync(
        LlmCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                model = _model,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
                stream = false,
                options = new { temperature = request.Temperature }
            };

            using var response = await _httpClient.PostAsJsonAsync(_endpoint, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken);
                if (!string.IsNullOrEmpty(ollamaResponse?.Message?.Content))
                {
                    return new LlmCompletionResponse
                    {
                        Content = ollamaResponse.Message.Content,
                        Model = _model,
                        PromptTokens = ollamaResponse.PromptEvalCount,
                        CompletionTokens = ollamaResponse.EvalCount,
                        IsDegraded = true,
                        ProviderName = "Ollama-Local"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ollama local LLM endpoint unavailable. Executing offline deterministic mode (Twist T2).");
        }

        // Deterministic Offline Degraded Fallback (T2)
        return GenerateOfflineCompletion(request);
    }

    public async IAsyncEnumerable<string> StreamAsync(
        LlmCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Stream? stream = null;
        HttpResponseMessage? response = null;

        try
        {
            var payload = new
            {
                model = _model,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
                stream = true
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = JsonContent.Create(payload)
            };

            response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ollama stream unavailable. Streaming offline deterministic chunks.");
        }

        if (stream != null)
        {
            using (response)
            await using (stream)
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    using var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("message", out var msgElem) &&
                        msgElem.TryGetProperty("content", out var contentElem))
                    {
                        var chunk = contentElem.GetString();
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            yield return chunk;
                        }
                    }
                }
            }
        }
        else
        {
            var fallback = GenerateOfflineCompletion(request);
            var words = fallback.Content.Split(' ');
            foreach (var word in words)
            {
                if (cancellationToken.IsCancellationRequested) yield break;
                yield return word + " ";
                await Task.Delay(15, cancellationToken);
            }
        }
    }

    public async Task<LlmToolCallResponse> CallWithToolsAsync(
        LlmToolCallRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                model = _model,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
                tools = request.Tools.Select(t => new
                {
                    type = "function",
                    function = new
                    {
                        name = t.Name,
                        description = t.Description,
                        parameters = JsonDocument.Parse(t.ParametersJsonSchema).RootElement
                    }
                }),
                stream = false
            };

            using var response = await _httpClient.PostAsJsonAsync(_endpoint, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken);
                if (ollamaResponse?.Message?.ToolCalls != null && ollamaResponse.Message.ToolCalls.Count > 0)
                {
                    var tools = ollamaResponse.Message.ToolCalls.Select(tc => new LlmToolCall
                    {
                        Name = tc.Function?.Name ?? string.Empty,
                        ArgumentsJson = tc.Function?.Arguments?.ToString() ?? "{}"
                    }).ToList();

                    return new LlmToolCallResponse
                    {
                        Content = ollamaResponse.Message.Content,
                        ToolCalls = tools,
                        IsDegraded = true,
                        ProviderName = "Ollama-Local"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ollama tool calling unavailable. Executing offline deterministic tool resolution (Twist T2).");
        }

        // Offline deterministic tool resolution based on intent
        return GenerateOfflineToolCalls(request);
    }

    private static LlmCompletionResponse GenerateOfflineCompletion(LlmCompletionRequest request)
    {
        var lastUserMsg = request.Messages.LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[DEGRADED MODE - OFFLINE EXECUTION]");
        sb.AppendLine("Notice: Operational without hosted cloud connectivity. Local deterministic fallback engaged.");
        sb.AppendLine();
        sb.AppendLine($"Analysis of inquiry: \"{lastUserMsg}\"");
        sb.AppendLine("- Safety protocol verification: Lockout-Tagout (LOTO) and PPE compliance required prior to physical equipment intervention.");
        sb.AppendLine("- Consult manufacturer technical manual for equipment isolation specifications.");

        return new LlmCompletionResponse
        {
            Content = sb.ToString(),
            Model = "offline-deterministic-fallback",
            PromptTokens = lastUserMsg.Length / 4,
            CompletionTokens = sb.Length / 4,
            IsDegraded = true,
            ProviderName = "Local/Degraded"
        };
    }

    private static LlmToolCallResponse GenerateOfflineToolCalls(LlmToolCallRequest request)
    {
        var lastUserMsg = request.Messages.LastOrDefault(m => m.Role == "user")?.Content.ToLowerInvariant() ?? string.Empty;
        var toolCalls = new List<LlmToolCall>();

        // Heuristic tool matching for offline mode
        foreach (var tool in request.Tools)
        {
            if (tool.Name.Contains("search", StringComparison.OrdinalIgnoreCase) &&
                (lastUserMsg.Contains("how") || lastUserMsg.Contains("what") || lastUserMsg.Contains("procedure") || lastUserMsg.Contains("bearing") || lastUserMsg.Contains("pump")))
            {
                toolCalls.Add(new LlmToolCall
                {
                    Name = tool.Name,
                    ArgumentsJson = JsonSerializer.Serialize(new { query = lastUserMsg })
                });
            }
            else if (tool.Name.Contains("safety", StringComparison.OrdinalIgnoreCase) || tool.Name.Contains("loto", StringComparison.OrdinalIgnoreCase))
            {
                toolCalls.Add(new LlmToolCall
                {
                    Name = tool.Name,
                    ArgumentsJson = JsonSerializer.Serialize(new { equipmentId = "CNC-MILL-01", procedure = "LOTO" })
                });
            }
        }

        return new LlmToolCallResponse
        {
            Content = "Engaging offline maintenance tools based on query intent.",
            ToolCalls = toolCalls,
            IsDegraded = true,
            ProviderName = "Local/Degraded"
        };
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }
    }

    private class OllamaMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<OllamaToolCall>? ToolCalls { get; set; }
    }

    private class OllamaToolCall
    {
        [JsonPropertyName("function")]
        public OllamaFunction? Function { get; set; }
    }

    private class OllamaFunction
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("arguments")]
        public object? Arguments { get; set; }
    }
}
