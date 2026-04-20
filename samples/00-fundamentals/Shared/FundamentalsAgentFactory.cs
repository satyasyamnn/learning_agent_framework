using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Fundamentals.Shared;

internal static class AiAgentFactory
{
    public static AIAgent CreateAgent(
        IConfiguration config,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        return CreateChatClient(config).AsAIAgent(
            instructions,
            name,
            description,
            tools,
            clientFactory,
            loggerFactory,
            services);
    }   

    public static ChatClient CreateChatClient(IConfiguration config, string? fallbackDeploymentName = null)
    {
        var azureOpenAIClient = CreateAzureOpenAIClient(config);
        var deploymentName = string.IsNullOrWhiteSpace(config["AzureOpenAI:DeploymentName"])
            ? fallbackDeploymentName ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName not configured")
            : config["AzureOpenAI:DeploymentName"]!;

        return azureOpenAIClient.GetChatClient(deploymentName);
    }

    public static AzureOpenAIClient CreateAzureOpenAIClient(IConfiguration config)
    {
        var endpointUrl = GetRequired(config, "AzureOpenAI:Endpoint");
        var endpoint = new Uri(new Uri(endpointUrl).GetLeftPart(UriPartial.Authority));
        var apiKey = config["AzureOpenAI:ApiKey"];

        return string.IsNullOrWhiteSpace(apiKey)
            ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
            : new AzureOpenAIClient(endpoint, new AzureKeyCredential(apiKey));
    }

    public static string GetRequired(IConfiguration config, string key)
    {
        return config[key] ?? throw new InvalidOperationException($"{key} not configured");
    }
}
