using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var apiKey =  config["OpenAI:ApiKey"];
var model = config["OpenAI:DeploymentName"];

AIAgent agent = new OpenAIClient(apiKey)
    .GetChatClient(model)
    .AsIChatClient()
    .AsAIAgent(
        instructions: "You are a helpful supply chain and customs assistant that gives concise, practical answers.",
        name: "SimpleOpenAIAgent");

Console.WriteLine("=== Fundamentals 02: Simple Agent with OpenAI ===\n");

AgentResponse response = await agent.RunAsync("Tell me three common causes of customs delays.");
Console.WriteLine($"Response: {response.Text}\n");

Console.WriteLine("Streaming response:\n");
await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("Give three actions to reduce last-mile delivery disruptions."))
{
    Console.Write(update.Text);
}

Console.WriteLine();


