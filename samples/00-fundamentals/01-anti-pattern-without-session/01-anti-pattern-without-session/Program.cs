using Fundamentals.Shared;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;

// Load configuration
IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Console.WriteLine("=== Anti-Pattern: Multi-Turn without Session ===\n");

AIAgent agent = FundamentalsAgentFactory.CreateAgent(
    config,
    instructions: "You are a helpful supply chain assistant. Remember important shipment details the user tells you.",
    name: "ContextAgent");

Console.WriteLine(">>> Turn 1: User shares their logistics profile\n");
AgentResponse response1 = await agent.RunAsync("My name is Alice. I manage customs clearance for electronics imports through Rotterdam.");
Console.WriteLine($"Agent: {response1.Text}\n");

Console.WriteLine(">>> Turn 2: User asks agent to recall details (PROBLEM: Agent won't remember!)\n");
AgentResponse response2 = await agent.RunAsync("What is my name?");
Console.WriteLine($"Agent: {response2.Text}\n");

Console.WriteLine(">>> Turn 3: User provides more information\n");
AgentResponse response3 = await agent.RunAsync("Our current average customs clearance time is 30 hours, and I need to reduce it below 24 hours.");
Console.WriteLine($"Agent: {response3.Text}\n");

Console.WriteLine(">>> Turn 4: User asks about previous info (PROBLEM: Agent has lost all context!)\n");
AgentResponse response4 = await agent.RunAsync("Can you summarize what you know about my role and clearance target?");
Console.WriteLine($"Agent: {response4.Text}\n");
