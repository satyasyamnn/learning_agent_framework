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
        instructions: "You are a helpful supply chain assistant. Remember important shipment details the user tells you.",
        name: "ContextAgent");

    Console.WriteLine(">>> Turn 1: User shares their logistics profile\n");
    string response1 = (await agent.RunAsync("My name is Alice. I manage customs clearance for electronics imports through Rotterdam.")).Text;
Console.WriteLine($"Agent: {response1}\n");

    Console.WriteLine(">>> Turn 2: User asks agent to recall details (PROBLEM: Agent won't remember!)\n");
string response2 = (await agent.RunAsync("What is my name?")).Text;
Console.WriteLine($"Agent: {response2}\n");

Console.WriteLine(">>> Turn 3: User provides more information\n");
    string response3 = (await agent.RunAsync("Our current average customs clearance time is 30 hours, and I need to reduce it below 24 hours.")).Text;
Console.WriteLine($"Agent: {response3}\n");

Console.WriteLine(">>> Turn 4: User asks about previous info (PROBLEM: Agent has lost all context!)\n");
    string response4 = (await agent.RunAsync("Can you summarize what you know about my role and clearance target?")).Text;
Console.WriteLine($"Agent: {response4}\n");