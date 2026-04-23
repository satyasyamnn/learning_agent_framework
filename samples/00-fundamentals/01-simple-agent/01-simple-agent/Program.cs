using Fundamentals.Shared;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;

// Load configuration
IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Console.WriteLine("=== Simple Agent Example ===\n");

AIAgent agent = AiAgentFactory.CreateAgent(
    config,
    instructions: "You are a helpful supply chain and customs assistant that provides concise and accurate information.",
    name: "BasicAgent");

Console.WriteLine(">>> Single turn - non-streaming\n");
AgentResponse response = await agent.RunAsync("What are three common causes of delays at an international customs checkpoint?");
Console.WriteLine($"Response: {response.Text}\n");

Console.WriteLine(">>> Single turn - another question with calculation involved \n");
AgentResponse mathResponse = await agent.RunAsync("A warehouse ships 25 cartons per pallet across 4 pallets. How many cartons are being shipped in total?");
Console.WriteLine($"Response: {mathResponse.Text}\n");

Console.WriteLine(">>> Single turn - streaming\n");
await foreach (var update in agent.RunStreamingAsync("List three best practices for reducing last-mile delivery disruptions."))
{
    Console.Write(update);
    Thread.Sleep(10);
}
Console.WriteLine("\n");


