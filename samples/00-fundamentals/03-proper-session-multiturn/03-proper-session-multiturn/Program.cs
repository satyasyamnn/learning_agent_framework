// Copyright (c) Microsoft. All rights reserved.
// Fundamentals Example 4: Proper Multi-Turn with AgenticSession
//
// This example demonstrates the CORRECT way to implement multi-turn conversations.
// 
// Key Concepts:
// - ✓ PROPER PATTERN: Use AgenticSession for maintaining conversation context
// - AgenticSession maintains message history across multiple turns
// - Agent can reference previous messages and maintain conversation state
// - Sessions can be serialized and deserialized for persistence
//
// Use Case: Multi-turn conversations, chatbots, interactive dialogs, 
// scenarios where context from previous messages matters.

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

Console.WriteLine("=== Proper Multi-Turn with AgenticSession ===\n");

AIAgent agent = (string.IsNullOrWhiteSpace(apiKey)
    ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey)))
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a helpful assistant. Remember important details the user tells you and use them in future responses.",
        name: "ContextAwareAgent");

// Create a session - this maintains conversation history
AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine(">>> Turn 1: User introduces themselves\n");
string response1 = (await agent.RunAsync("My name is Alice and I'm interested in machine learning.", session)).Text;
Console.WriteLine($"Agent: {response1}\n");

Console.WriteLine(">>> Turn 2: User asks agent to recall their name (SUCCESS: Agent remembers!)\n");
string response2 = (await agent.RunAsync("What is my name?", session)).Text;
Console.WriteLine($"Agent: {response2}\n");

Console.WriteLine(">>> Turn 3: User provides more information\n");
string response3 = (await agent.RunAsync("I work as a data scientist with 5 years of experience.", session)).Text;
Console.WriteLine($"Agent: {response3}\n");

Console.WriteLine(">>> Turn 4: User asks about previous info (SUCCESS: Agent recalls everything!)\n");
string response4 = (await agent.RunAsync("Can you summarize what you know about me?", session)).Text;
Console.WriteLine($"Agent: {response4}\n");

Console.WriteLine(">>> Turn 5: Complex question using accumulated context\n");
string response5 = (await agent.RunAsync(
    "Based on my background, what machine learning specializations would you recommend for someone with my experience?", 
    session)).Text;
Console.WriteLine($"Agent: {response5}\n");

// Demonstrate streaming with session
Console.WriteLine(">>> Turn 6: Streaming response with context\n");
await foreach (var update in agent.RunStreamingAsync(
    "Give me 3 practical tips for advancing my machine learning career as a data scientist.", 
    session))
{
    Console.Write(update);
}
Console.WriteLine("\n");

// Demonstrate session serialization/persistence
Console.WriteLine(">>> Serializing session for persistence\n");
var serializedSession = await agent.SerializeSessionAsync(session);
Console.WriteLine($"✓ Session serialized successfully");
Console.WriteLine($"  (In real apps, save this to a database or cache)\n");

// Create a new session and restore from serialization
Console.WriteLine(">>> Creating new session and restoring from serialized data\n");
AgentSession restoredSession = await agent.DeserializeSessionAsync(serializedSession);
string response6 = (await agent.RunAsync("Do you still remember details about me?", restoredSession)).Text;
Console.WriteLine($"Agent: {response6}\n");

// Console.WriteLine("✓ Multi-turn session example completed!");
// Console.WriteLine("\n✓ BEST PRACTICES:\n");
// Console.WriteLine("1. Use AgenticSession for multi-turn conversations");
// Console.WriteLine("2. Pass the session to every agent.RunAsync() call in the conversation");
// Console.WriteLine("3. Sessions maintain full message history and context");
// Console.WriteLine("4. Sessions can be serialized for later restoration");
// Console.WriteLine("5. Each conversation should have its own session");
// Console.WriteLine("\n✓ WHEN TO USE SESSION:");
// Console.WriteLine("- Multi-turn conversations");
// Console.WriteLine("- Chatbots and interactive dialogs");
// Console.WriteLine("- Any scenario requiring context from previous messages");
// Console.WriteLine("\n✓ WHEN NOT TO USE SESSION:");
// Console.WriteLine("- Single-turn queries (simple Q&A)");
// Console.WriteLine("- Independent function calls");
// Console.WriteLine("- Stateless API endpoints");
