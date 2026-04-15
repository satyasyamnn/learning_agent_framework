// Copyright (c) Microsoft. All rights reserved.
// Fundamentals Example 2: Agent with Tools
//
// This example demonstrates how to extend an agent with tools/functions.
// 
// Key Concepts:
// - Adding tools to an agent using AIFunctionFactory
// - Function descriptions via Description attributes
// - Parameter descriptions for LLM understanding
// - Tool invocation and result handling
//
// Use Case: When you need the agent to perform specific actions or access external data
// through predefined functions in a supply chain or customs workflow.

using System.ComponentModel;
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

Console.WriteLine("=== Agent with Tools Example (Supply Chain + Customs) ===\n");

// Define tools as attributed methods
// The Description attributes help the LLM understand what each tool/parameter does

[Description("Get shipment risk status for a specified port.")]
static string GetPortRiskStatus([Description("The port name to check risk status for")] string port)
    => port.ToLower() switch
    {
        "singapore" => "Low disruption risk, customs throughput normal",
        "rotterdam" => "Moderate disruption risk, berth congestion observed",
        "los angeles" => "High disruption risk, vessel queue extended",
        "dubai" => "Low disruption risk, inspections on schedule",
        _ => "Unable to retrieve risk status for this port"
    };

[Description("Estimate customs duty amount from declared value and duty rate.")]
static double EstimateCustomsDuty(
    [Description("Declared shipment value in USD")] double declaredValue,
    [Description("Duty rate as a percentage (for example, 8.5 for 8.5%)")] double dutyRatePercent)
{
    return declaredValue * (dutyRatePercent / 100);
}

[Description("Get the average customs clearance time for a port.")]
static string GetPortClearanceTime([Description("The port name")] string port)
    => port.ToLower() switch
    {
        "singapore" => "14 hours",
        "rotterdam" => "20 hours",
        "los angeles" => "36 hours",
        "dubai" => "18 hours",
        _ => "Unknown port"
    };

// Create agent with tools
var azureOpenAIClient = string.IsNullOrWhiteSpace(apiKey)
    ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

var chatClient = azureOpenAIClient.GetChatClient(deploymentName);

AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful supply chain and customs operations assistant. Use the available tools to answer questions about port risk status, customs duty estimation, and clearance time. Always provide helpful and accurate information.",
    tools:
    [
        AIFunctionFactory.Create(GetPortRiskStatus),
        AIFunctionFactory.Create(EstimateCustomsDuty),
        AIFunctionFactory.Create(GetPortClearanceTime)
    ]);

Console.WriteLine(">>> Example 1: Query port risk status (agent will use GetPortRiskStatus tool)\n");
AgentResponse response = await agent.RunAsync("What is the current disruption risk status at Rotterdam port?");
Console.WriteLine($"Response: {response.Text}\n");

Console.WriteLine(">>> Example 2: Estimate customs duty (agent will use EstimateCustomsDuty tool)\n");
response = await agent.RunAsync("Estimate duty for a shipment valued at 120000 USD with a duty rate of 7.5%. Include a short operational note.");
Console.WriteLine($"Response: {response.Text}\n");

Console.WriteLine(">>> Example 3: Streaming with tool usage\n");
await foreach (var update in agent.RunStreamingAsync(
    "Summarize Los Angeles port risk and average clearance time, then estimate duty for 85000 USD at 5%."))
{
    Console.Write(update);
}
Console.WriteLine("\n");

Console.WriteLine(">>> Example 4: Tool usage across multiple queries\n");
response = await agent.RunAsync("Which port has lower disruption risk: Singapore or Los Angeles? Also estimate duty on 50000 USD at 4%.");
Console.WriteLine($"Response: {response.Text}\n");
