using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Fundamentals.Shared;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using OpenAI.Responses;
using System.ClientModel;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

#pragma warning disable OPENAI001

// Load configuration
IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var endpointUrl = config["AzureOpenAI:Endpoint"]
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
var deploymentName = config["AzureOpenAI:DeploymentName"]
    ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName not configured");
var responsesModel = config["AzureOpenAI:ResponsesModel"] ?? deploymentName;
var apiKey = config["AzureOpenAI:ApiKey"];
var endpoint = new Uri(new Uri(endpointUrl).GetLeftPart(UriPartial.Authority)).ToString();
var isProjectEndpoint = endpointUrl.Contains("/api/projects/", StringComparison.OrdinalIgnoreCase);

var customsQuestion =
    "For customs shipment CSH-9021 entering Germany from Singapore, identify likely inspection focus areas and recommend a fast-track action plan. Return in max 35 words.";

var azureOpenAIClient = string.IsNullOrWhiteSpace(apiKey)
    ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

WriteHeader("Customs Reasoning Effort Sample (Microsoft Agent Framework)");

WriteSection("1) Baseline (default reasoning effort)");
await RunBaselineAsync();

WriteSection("2) ChatClient with minimal reasoning effort");
await RunChatClientMinimalReasoningAsync();

WriteSection("3) Responses API with high reasoning effort + detailed reasoning summary");
await RunResponsesHighReasoningAsync();

return;

async Task RunBaselineAsync()
{
    ChatClientAgent agent = azureOpenAIClient
        .GetChatClient(deploymentName)
        .AsAIAgent(
            name: "CustomsReasoningBaseline",
            instructions: "You are a customs clearance operations expert. Give concise and practical guidance.");

    AgentResponse response = await agent.RunAsync(customsQuestion);
    Console.WriteLine(response.Text);
    response.WriteTokenUsageToConsole("Baseline");
}

async Task RunChatClientMinimalReasoningAsync()
{
    ChatClientAgent agent = azureOpenAIClient
        .GetChatClient(deploymentName)
        .AsAIAgent(new ChatClientAgentOptions
        {
            Name = "CustomsReasoningMinimal",
            ChatOptions = new ChatOptions
            {
                Instructions = "You are a customs clearance operations expert. Give concise and practical guidance.",
                RawRepresentationFactory = _ => new ChatCompletionOptions
                {
                    ReasoningEffortLevel = ChatReasoningEffortLevel.Minimal
                }
            }
        });

    AgentResponse response = await agent.RunAsync(customsQuestion);
    Console.WriteLine(response.Text);    
    response.WriteTokenUsageToConsole("Minimal Reasoning");
}

async Task RunResponsesHighReasoningAsync()
{
    if (isProjectEndpoint)
    {
        //Console.WriteLine("Configured endpoint is an Azure AI Foundry project endpoint.");
        //Console.WriteLine("Responses API with this OpenAI client path is not available here, so using ChatClient high-reasoning fallback.\n");
        await RunChatClientHighReasoningFallbackAsync();
        return;
    }

    try
    {
        ChatClientAgent agent = azureOpenAIClient
            .GetResponsesClient()
            .AsAIAgent(
                model: responsesModel,
                options: new ChatClientAgentOptions
                {
                    Name = "CustomsReasoningHigh",
                    ChatOptions = new ChatOptions
                    {
                        Instructions = "You are a customs clearance operations expert. Give concise and practical guidance.",
                        RawRepresentationFactory = _ => new CreateResponseOptions
                        {
                            ReasoningOptions = new ResponseReasoningOptions
                            {
                                ReasoningEffortLevel = ResponseReasoningEffortLevel.High,
                                ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed
                            }
                        }
                    }
                });

        AgentResponse response = await agent.RunAsync(customsQuestion);

        foreach (ChatMessage message in response.Messages)
        {
            foreach (AIContent content in message.Contents)
            {
                if (content is TextReasoningContent reasoning)
                {
                    Console.WriteLine("Reasoning Summary:");
                    Console.WriteLine(reasoning.Text);
                    Console.WriteLine();
                }
            }
        }

        Console.WriteLine(response.Text);
        response.WriteTokenUsageToConsole("High Reasoning (Responses API)");
    }
    catch (ClientResultException ex) when (ex.Status == 404)
    {
        Console.WriteLine("Responses API returned 404 for the configured endpoint/model.");
        Console.WriteLine($"Endpoint: {endpointUrl}");
        Console.WriteLine($"Responses model/deployment: {responsesModel}");
        Console.WriteLine("Falling back to ChatClient with high reasoning effort (no reasoning summary available).\n");

        await RunChatClientHighReasoningFallbackAsync();
    }
}

async Task RunChatClientHighReasoningFallbackAsync()
{
    ChatClientAgent fallbackAgent = azureOpenAIClient
        .GetChatClient(deploymentName)
        .AsAIAgent(new ChatClientAgentOptions
        {
            Name = "CustomsReasoningHighFallback",
            ChatOptions = new ChatOptions
            {
                Instructions = "You are a customs clearance operations expert. Give concise and practical guidance.",
                RawRepresentationFactory = _ => new ChatCompletionOptions
                {
                    ReasoningEffortLevel = ChatReasoningEffortLevel.High
                }
            }
        });

    AgentResponse response = await fallbackAgent.RunAsync(customsQuestion);
    Console.WriteLine(response.Text);    
    response.WriteTokenUsageToConsole("High Reasoning (ChatClient Fallback)");
}

static void WriteHeader(string text)
{
    Console.WriteLine($"=== {text} ===");
    Console.WriteLine();
}

static void WriteSection(string text)
{
    Console.WriteLine($">>> {text}");
    Console.WriteLine();
}
