// Copyright (c) Microsoft. All rights reserved.
// Fundamentals Example 3: Anti-Pattern - Missing Session for Multi-Turn Conversations
//
// This example demonstrates what NOT to do when you need multi-turn conversations.
// It shows the consequences of not using AgenticSession for maintaining context.
//
// Key Concepts:
// - ❌ ANTI-PATTERN: Making multiple calls to agent.RunAsync() without a session
// - Problem: Each call loses context from previous interactions
// - Result: The agent forgets previous messages and cannot maintain conversation state
//
// Use Case: This example shows a PROBLEMATIC pattern. Do NOT do this!
// If you need multi-turn conversations, use AgenticSession (see example 03).

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

Console.WriteLine("=== Anti-Pattern: Multi-Turn without Session ===\n");
Console.WriteLine("⚠️  WARNING: This example demonstrates an ANTI-PATTERN!\n");

AIAgent agent = (string.IsNullOrWhiteSpace(apiKey)
    ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey)))
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a helpful assistant. Remember important details the user tells you.",
        name: "ContextAgent");

Console.WriteLine(">>> Turn 1: User introduces themselves\n");
string response1 = (await agent.RunAsync("My name is Alice and I'm interested in machine learning.")).Text;
Console.WriteLine($"Agent: {response1}\n");

Console.WriteLine(">>> Turn 2: User asks agent to recall their name (PROBLEM: Agent won't remember!)\n");
string response2 = (await agent.RunAsync("What is my name?")).Text;
Console.WriteLine($"Agent: {response2}\n");

Console.WriteLine(">>> Turn 3: User provides more information\n");
string response3 = (await agent.RunAsync("I work as a data scientist with 5 years of experience.")).Text;
Console.WriteLine($"Agent: {response3}\n");

Console.WriteLine(">>> Turn 4: User asks about previous info (PROBLEM: Agent has lost all context!)\n");
string response4 = (await agent.RunAsync("Can you summarize what you know about me?")).Text;
Console.WriteLine($"Agent: {response4}\n");

Console.WriteLine("❌ PROBLEMS WITH THIS APPROACH:\n");
Console.WriteLine("1. Agent loses context between calls");
Console.WriteLine("2. No conversation history is maintained");
Console.WriteLine("3. Each call starts fresh - agent doesn't remember previous exchanges");
Console.WriteLine("4. Inefficient - you're not leveraging conversation continuity");
Console.WriteLine("5. Poor user experience - agent appears to have no memory\n");

Console.WriteLine("✓ Demonstrated the anti-pattern");
Console.WriteLine("\n🔧 SOLUTION: See example 03-proper-session-multiturn for the correct approach!");
Console.WriteLine("Use AgenticSession to maintain conversation context across multiple turns.");
