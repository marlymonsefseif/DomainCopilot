using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using DomainCopilot.Application.Common.DTOs;
using DomainCopilot.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DomainCopilot.Infrastructure.Providers;

public class HostedLlmProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HostedLlmProvider> _logger;
    private readonly string _endpoint;
    private readonly string _model;
    private readonly string _apiKey;

    public string ProviderName => "Hosted";
    public bool IsDegraded => false;

    public HostedLlmProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<HostedLlmProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _endpoint = configuration["Llm:Hosted:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
        _model = configuration["Llm:Hosted:Model"] ?? "gpt-4o-mini";
        _apiKey = configuration["Llm:Hosted:ApiKey"] ?? string.Empty;
    }

    public async Task<LlmCompletionResponse> CompleteAsync(
        LlmCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        using var httpRequest = CreateRequestMessage(HttpMethod.Post, _endpoint);
        var payload = new
        {
            model = _model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens
        };

        httpRequest.Content = JsonContent.Create(payload);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<OpenAiChatCompletionResponse>(cancellationToken: cancellationToken);
        var choice = json?.Choices?.FirstOrDefault();

        return new LlmCompletionResponse
        {
            Content = choice?.Message?.Content ?? string.Empty,
            Model = _model,
            PromptTokens = json?.Usage?.PromptTokens ?? 0,
            CompletionTokens = json?.Usage?.CompletionTokens ?? 0,
            IsDegraded = false,
            ProviderName = ProviderName
        };
    }

    public async IAsyncEnumerable<string> StreamAsync(
        LlmCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        using var httpRequest = CreateRequestMessage(HttpMethod.Post, _endpoint);
        var payload = new
        {
            model = _model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            stream = true
        };

        httpRequest.Content = JsonContent.Create(payload);
        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("data: "))
            {
                var data = line["data: ".Length..].Trim();
                if (data == "[DONE]") break;

                using var doc = JsonDocument.Parse(data);
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var delta = choices[0].GetProperty("delta");
                    if (delta.TryGetProperty("content", out var contentElement))
                    {
                        var text = contentElement.GetString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            yield return text;
                        }
                    }
                }
            }
        }
    }

    public async Task<LlmToolCallResponse> CallWithToolsAsync(
        LlmToolCallRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        using var httpRequest = CreateRequestMessage(HttpMethod.Post, _endpoint);
        var toolsPayload = request.Tools.Select(t => new
        {
            type = "function",
            function = new
            {
                name = t.Name,
                description = t.Description,
                parameters = JsonDocument.Parse(t.ParametersJsonSchema).RootElement
            }
        });

        var payload = new
        {
            model = _model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            tools = toolsPayload,
            tool_choice = "auto",
            temperature = request.Temperature
        };

        httpRequest.Content = JsonContent.Create(payload);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<OpenAiChatCompletionResponse>(cancellationToken: cancellationToken);
        var choice = json?.Choices?.FirstOrDefault();

        var toolCalls = new List<LlmToolCall>();
        if (choice?.Message?.ToolCalls != null)
        {
            foreach (var tc in choice.Message.ToolCalls)
            {
                toolCalls.Add(new LlmToolCall
                {
                    Id = tc.Id ?? Guid.NewGuid().ToString("N"),
                    Name = tc.Function?.Name ?? string.Empty,
                    ArgumentsJson = tc.Function?.Arguments ?? "{}"
                });
            }
        }

        return new LlmToolCallResponse
        {
            Content = choice?.Message?.Content,
            ToolCalls = toolCalls,
            IsDegraded = false,
            ProviderName = ProviderName
        };
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Hosted LLM API key is not configured.");
        }
    }

    private HttpRequestMessage CreateRequestMessage(HttpMethod method, string url)
    {
        var msg = new HttpRequestMessage(method, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        return msg;
    }

    private class OpenAiChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice> Choices { get; set; } = new();

        [JsonPropertyName("usage")]
        public OpenAiUsage? Usage { get; set; }
    }

    private class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiMessage? Message { get; set; }
    }

    private class OpenAiMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<OpenAiToolCall>? ToolCalls { get; set; }
    }

    private class OpenAiToolCall
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("function")]
        public OpenAiFunction? Function { get; set; }
    }

    private class OpenAiFunction
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("arguments")]
        public string? Arguments { get; set; }
    }

    private class OpenAiUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
    }
}
