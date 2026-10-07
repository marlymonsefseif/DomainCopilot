using DomainCopilot.Application.Common.DTOs;
using DomainCopilot.Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DomainCopilot.UnitTests;

public class LlmProviderTests
{
    [Fact]
    public async Task LocalLlmProvider_GeneratesDeterministicOfflineCompletion()
    {
        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient();
        var provider = new LocalLlmProvider(httpClient, config, NullLogger<LocalLlmProvider>.Instance);

        var request = new LlmCompletionRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.User("How do I perform lockout-tagout on a centrifugal pump?")
            }
        };

        var response = await provider.CompleteAsync(request);

        Assert.NotNull(response);
        Assert.True(response.IsDegraded);
        Assert.Contains("Lockout-Tagout", response.Content);
        Assert.Contains("DEGRADED MODE", response.Content);
    }

    [Fact]
    public async Task LocalLlmProvider_StreamsChunksInOfflineMode()
    {
        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient();
        var provider = new LocalLlmProvider(httpClient, config, NullLogger<LocalLlmProvider>.Instance);

        var request = new LlmCompletionRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.User("Check motor vibration")
            }
        };

        var chunks = new List<string>();
        await foreach (var token in provider.StreamAsync(request))
        {
            chunks.Add(token);
        }

        Assert.NotEmpty(chunks);
        var fullText = string.Join("", chunks);
        Assert.Contains("DEGRADED MODE", fullText);
    }

    [Fact]
    public async Task LocalLlmProvider_ResolvesToolsDeterministically()
    {
        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient();
        var provider = new LocalLlmProvider(httpClient, config, NullLogger<LocalLlmProvider>.Instance);

        var request = new LlmToolCallRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.User("search procedure for bearing replacement")
            },
            Tools = new List<LlmToolDefinition>
            {
                new()
                {
                    Name = "search_manuals",
                    Description = "Search technical documentation",
                    ParametersJsonSchema = "{\"type\":\"object\"}"
                }
            }
        };

        var response = await provider.CallWithToolsAsync(request);

        Assert.NotNull(response);
        Assert.True(response.IsDegraded);
        Assert.NotEmpty(response.ToolCalls);
        Assert.Equal("search_manuals", response.ToolCalls[0].Name);
    }

    [Fact]
    public async Task ResilientLlmProvider_SwitchesToLocal_WhenHostedFails()
    {
        var config = new ConfigurationBuilder().Build(); // No API key configured
        var httpClient = new HttpClient();

        var hosted = new HostedLlmProvider(httpClient, config, NullLogger<HostedLlmProvider>.Instance);
        var local = new LocalLlmProvider(httpClient, config, NullLogger<LocalLlmProvider>.Instance);
        var resilient = new ResilientLlmProvider(hosted, local, NullLogger<ResilientLlmProvider>.Instance);

        var request = new LlmCompletionRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.User("Calibrate pressure transmitter")
            }
        };

        var response = await resilient.CompleteAsync(request);

        Assert.NotNull(response);
        Assert.True(resilient.IsDegraded);
        Assert.Contains("DEGRADED MODE", response.Content);
    }
}
