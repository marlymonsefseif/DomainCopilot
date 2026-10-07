namespace DomainCopilot.Application.Common.DTOs;

public class ChatMessage
{
    public string Role { get; init; } = "user";
    public string Content { get; init; } = string.Empty;

    public static ChatMessage System(string content) => new() { Role = "system", Content = content };
    public static ChatMessage User(string content) => new() { Role = "user", Content = content };
    public static ChatMessage Assistant(string content) => new() { Role = "assistant", Content = content };
}

public class LlmCompletionRequest
{
    public List<ChatMessage> Messages { get; init; } = new();
    public float Temperature { get; init; } = 0.2f;
    public int MaxTokens { get; init; } = 1000;
}

public class LlmCompletionResponse
{
    public string Content { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public bool IsDegraded { get; init; }
    public string ProviderName { get; init; } = string.Empty;
}

public class LlmToolDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ParametersJsonSchema { get; init; } = "{}";
}

public class LlmToolCall
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = string.Empty;
    public string ArgumentsJson { get; init; } = "{}";
}

public class LlmToolCallRequest
{
    public List<ChatMessage> Messages { get; init; } = new();
    public List<LlmToolDefinition> Tools { get; init; } = new();
    public float Temperature { get; init; } = 0.1f;
}

public class LlmToolCallResponse
{
    public string? Content { get; init; }
    public List<LlmToolCall> ToolCalls { get; init; } = new();
    public bool IsDegraded { get; init; }
    public string ProviderName { get; init; } = string.Empty;
}
