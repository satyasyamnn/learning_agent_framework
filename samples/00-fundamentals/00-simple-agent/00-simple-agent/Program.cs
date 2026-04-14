using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

// Load configuration
IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var endpointUrl = config["AzureOpenAI:Endpoint"] 
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
var deploymentName = config["AzureOpenAI:DeploymentName"] 
    ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName not configured");
var apiKey = config["AzureOpenAI:ApiKey"];
var endpoint = new Uri(new Uri(endpointUrl).GetLeftPart(UriPartial.Authority)).ToString();

Console.WriteLine("=== Simple Agent Example ===\n");

// Create a chat client and convert it to an AI agent
// The agent is a simple wrapper around the chat client that adds AI-specific features
var azureOpenAIClient = string.IsNullOrWhiteSpace(apiKey)
    ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

var chatClient = azureOpenAIClient.GetChatClient(deploymentName);

AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful assistant that provides concise and accurate information.",
    name: "BasicAgent");

Console.WriteLine(">>> Example 1: Single turn - non-streaming\n");
string response = (await agent.RunAsync("What is the capital of France?")).Text;
Console.WriteLine($"Response: {response}\n");

Console.WriteLine(">>> Example 2: Single turn - streaming\n");
await foreach (var update in agent.RunStreamingAsync("List three benefits of renewable energy."))
{
    Console.Write(update);
}
Console.WriteLine("\n");

Console.WriteLine(">>> Example 3: Another single-turn question\n");
string mathResponse = (await agent.RunAsync("Calculate 25 * 4")).Text;
Console.WriteLine($"Response: {mathResponse}\n");

