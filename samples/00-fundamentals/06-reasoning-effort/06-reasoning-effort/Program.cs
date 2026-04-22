using Fundamentals.Shared;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

#pragma warning disable OPENAI001

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var deploymentName = AiAgentFactory.GetRequired(config, "AzureOpenAI:DeploymentName");
var azureOpenAIClient = AiAgentFactory.CreateAzureOpenAIClient(config);

const string SystemPrompt = "You are a customs clearance operations expert. Give concise and practical guidance.";
const string Question = "For customs shipment CSH-9021 entering Germany from Singapore, identify likely inspection focus areas and recommend a fast-track action plan. Return in max 35 words.";

await RunAsync("Baseline");

await RunAsync("Minimal Reasoning", ChatReasoningEffortLevel.Minimal);

await RunAsync("High Reasoning", ChatReasoningEffortLevel.High);

return;

async Task RunAsync(string label, ChatReasoningEffortLevel? reasoningEffort = null)
{
    var chatOptions = new ChatOptions { Instructions = SystemPrompt };
    if (reasoningEffort is not null)
        chatOptions.RawRepresentationFactory = _ => new ChatCompletionOptions { ReasoningEffortLevel = reasoningEffort };

    ChatClientAgent agent = azureOpenAIClient
        .GetChatClient(deploymentName)
        .AsAIAgent(new ChatClientAgentOptions { Name = "CustomsReasoningAgent", ChatOptions = chatOptions });

    AgentResponse response = await agent.RunAsync(Question);
    Console.WriteLine("------------------------------------------------");
    Console.WriteLine(response.Text);
    response.WriteTokenUsageToConsole(label);
    Console.WriteLine("------------------------------------------------");
}
