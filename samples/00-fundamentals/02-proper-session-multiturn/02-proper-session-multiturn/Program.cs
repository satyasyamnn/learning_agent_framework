using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using Fundamentals.Shared;

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

Console.WriteLine("=== Proper Multi-Turn with AgenticSession (Supply Chain Context) ===\n");

AIAgent agent = (string.IsNullOrWhiteSpace(apiKey)
    ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey)))
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a helpful supply chain and customs assistant. Remember important operational details the user tells you and use them in future responses.",
        name: "ContextAwareAgent");

// Create a session - this maintains conversation history
AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine(">>> Turn 1: User shares logistics background\n");
AgentResponse response1 = await agent.RunAsync("My name is Alice. I lead customs operations for electronics imports through Rotterdam and Singapore.", session);
Console.WriteLine($"Agent: {response1.Text}\n");
response1.WriteTokenUsageToConsole("Turn 1");
Console.WriteLine();

Console.WriteLine(">>> Turn 2: User asks agent to recall their name (SUCCESS: Agent remembers!)\n");
AgentResponse response2 = await agent.RunAsync("What is my name?", session);
Console.WriteLine($"Agent: {response2.Text}\n");
response2.WriteTokenUsageToConsole("Turn 2");
Console.WriteLine();

Console.WriteLine(">>> Turn 3: User provides more information\n");
AgentResponse response3 = await agent.RunAsync("Our baseline customs clearance time is 30 hours, and my goal is to get it under 24 hours this quarter.", session);
Console.WriteLine($"Agent: {response3.Text}\n");
response3.WriteTokenUsageToConsole("Turn 3");
Console.WriteLine();

Console.WriteLine(">>> Turn 4: User asks about previous info (SUCCESS: Agent recalls everything!)\n");
AgentResponse response4 = await agent.RunAsync("Can you summarize what you know about my role, ports, and clearance target?", session);
Console.WriteLine($"Agent: {response4.Text}\n");
response4.WriteTokenUsageToConsole("Turn 4");
Console.WriteLine();

Console.WriteLine(">>> Turn 5: Complex question using accumulated context\n");
AgentResponse response5 = await agent.RunAsync(
    "Based on my role and goals, what are the top three operational changes I should prioritize to reduce customs delays?", 
    session);
Console.WriteLine($"Agent: {response5.Text}\n");
response5.WriteTokenUsageToConsole("Turn 5");
Console.WriteLine();

// Demonstrate streaming with session
Console.WriteLine(">>> Turn 6: Streaming response with context\n");
await agent.RunStreamingAsync(
        "Give me three practical weekly actions to improve customs document quality and lower inspection delays.",
        session)
    .WriteStreamingResponseAndTokenUsageToConsoleAsync("Turn 6 (Streaming)");
Console.WriteLine();

// Demonstrate session serialization/persistence
Console.WriteLine(">>> Serializing session for persistence\n");
var serializedSession = await agent.SerializeSessionAsync(session);
Console.WriteLine($"✓ Session serialized successfully");
Console.WriteLine($"  (In real apps, save this to a database or cache)\n");

// Create a new session and restore from serialization
Console.WriteLine(">>> Creating new session and restoring from serialized data\n");
AgentSession restoredSession = await agent.DeserializeSessionAsync(serializedSession);
AgentResponse response6 = await agent.RunAsync("Do you still remember my customs operations details?", restoredSession);
Console.WriteLine($"Agent: {response6.Text}\n");
response6.WriteTokenUsageToConsole("Turn 7");