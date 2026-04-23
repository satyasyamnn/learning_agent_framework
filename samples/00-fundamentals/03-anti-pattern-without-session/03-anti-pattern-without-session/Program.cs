using Fundamentals.Shared;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;

// Load configuration
IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Console.WriteLine("=== Anti-Pattern: Multi-Turn without Session ===\n");

AIAgent agent = AiAgentFactory.CreateAgent(
    config,
    instructions: "You are a helpful supply chain assistant. Remember important shipment details the user tells you.",
    name: "ContextAgent");

Console.WriteLine(">>> Turn 1: User shares their logistics profile\n");
string prompt1 = "My name is Alice. I manage customs clearance for electronics imports through Rotterdam.";
Console.WriteLine($"User: {prompt1}\n");
AgentResponse response1 = await agent.RunAsync(prompt1);
Console.WriteLine($"Agent: {response1.Text}\n");

Console.WriteLine(">>> Turn 2: User asks agent to recall details (PROBLEM: Agent won't remember!)\n");
string prompt2 = "What is my name?";
Console.WriteLine($"User: {prompt2}\n");
AgentResponse response2 = await agent.RunAsync(prompt2);
Console.WriteLine($"Agent: {response2.Text}\n");

Console.WriteLine(">>> Turn 3: User provides more information\n");
string prompt3 = "Our current average customs clearance time is 30 hours, and I need to reduce it below 24 hours.";
Console.WriteLine($"User: {prompt3}\n");
AgentResponse response3 = await agent.RunAsync(prompt3);
Console.WriteLine($"Agent: {response3.Text}\n");

Console.WriteLine(">>> Turn 4: User asks about previous info (PROBLEM: Agent has lost all context!)\n");
string prompt4 = "Can you summarize what you know about my role and clearance target?";
Console.WriteLine($"User: {prompt4}\n");
AgentResponse response4 = await agent.RunAsync(prompt4);
Console.WriteLine($"Agent: {response4.Text}\n");
