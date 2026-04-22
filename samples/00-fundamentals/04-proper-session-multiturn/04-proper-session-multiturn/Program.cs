using Fundamentals.Shared;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;

// Load configuration
IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Console.WriteLine("=== Proper Multi-Turn with AgentSession ===\n");

AIAgent agent = AiAgentFactory.CreateAgent(
    config,
    instructions: "You are a helpful supply chain and customs assistant. Remember important operational details the user tells you and use them in future responses.",
    name: "ContextAwareAgent");


async Task RunTurnAsync(string title, string prompt, string tokenLabel, AgentSession currentSession)
{
    Console.WriteLine($">>> {title}\n");
    Console.WriteLine($"User: {prompt}\n");

    AgentResponse response = await agent.RunAsync(prompt, currentSession);
    Console.WriteLine($"Agent: {response.Text}\n");
    response.WriteTokenUsageToConsole(tokenLabel);
    Console.WriteLine();
}

// Create a session - this maintains conversation history
AgentSession session = await agent.CreateSessionAsync();

await RunTurnAsync(
    "Turn 1: Share operating context",
    "My name is Alice. I lead customs operations for electronics imports through Rotterdam and Singapore. Our baseline customs clearance time is 30 hours, and my goal is to get it under 24 hours this quarter.",
    "Turn 1",
    session);

await RunTurnAsync(
    "Turn 2: Confirm memory",
    "Summarize what you know about my role, ports, and clearance target.",
    "Turn 2",
    session);

Console.WriteLine(">>> Turn 3: Streaming follow-up with the same session\n");
Console.WriteLine("User: Based on that context, give me three weekly actions to improve customs document quality and lower inspection delays.\n");

await agent.RunStreamingAsync(
        "Based on that context, give me three weekly actions to improve customs document quality and lower inspection delays.",
        session)
    .WriteStreamingResponseAndTokenUsageToConsoleAsync("Turn 3 (Streaming)");
Console.WriteLine();

Console.WriteLine(">>> Persist and restore the session\n");
var serializedSession = await agent.SerializeSessionAsync(session);
Console.WriteLine("Session serialized successfully.");
Console.WriteLine("In a real app, store this in a database or cache.\n");

AgentSession restoredSession = await agent.DeserializeSessionAsync(serializedSession);
await RunTurnAsync(
    "Restore check",
    "What clearance target am I trying to hit, and which ports did I mention?",
    "Restore Check",
    restoredSession);