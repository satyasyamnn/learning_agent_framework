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
// through predefined functions. The agent decides when and how to call these tools.

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

Console.WriteLine("=== Agent with Tools Example ===\n");

// Define tools as attributed methods
// The Description attributes help the LLM understand what each tool/parameter does

[Description("Get the current weather for a specified location.")]
static string GetWeather([Description("The city name to get weather for")] string city)
    => city.ToLower() switch
    {
        "london" => "Rainy, 12°C",
        "paris" => "Cloudy, 14°C",
        "sydney" => "Sunny, 25°C",
        "new york" => "Partly cloudy, 18°C",
        _ => "Unable to get weather for this location"
    };

[Description("Convert temperature from Celsius to Fahrenheit.")]
static double ConvertCelsiusToFahrenheit([Description("Temperature in Celsius")] double celsius)
{
    return (celsius * 9 / 5) + 32;
}

[Description("Get the population of a city.")]
static string GetCityPopulation([Description("The city name")] string city)
    => city.ToLower() switch
    {
        "london" => "8.9 million",
        "paris" => "2.2 million",
        "sydney" => "5.3 million",
        "new york" => "8.3 million",
        _ => "Unknown city"
    };

// Create agent with tools
var azureOpenAIClient = string.IsNullOrWhiteSpace(apiKey)
    ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

var chatClient = azureOpenAIClient.GetChatClient(deploymentName);

AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful travel assistant. Use the available tools to answer questions about weather, temperature conversion, and city information. Always provide helpful and accurate information.",
    tools:
    [
        AIFunctionFactory.Create(GetWeather),
        AIFunctionFactory.Create(ConvertCelsiusToFahrenheit),
        AIFunctionFactory.Create(GetCityPopulation)
    ]);

Console.WriteLine(">>> Example 1: Query weather (agent will use GetWeather tool)\n");
string response = (await agent.RunAsync("What's the weather like in Paris?")).Text;
Console.WriteLine($"Response: {response}\n");

Console.WriteLine(">>> Example 2: Convert temperature (agent will use ConvertCelsiusToFahrenheit tool)\n");
response = (await agent.RunAsync("Convert 20 degrees Celsius to Fahrenheit and tell me what that means for London weather.")).Text;
Console.WriteLine($"Response: {response}\n");

Console.WriteLine(">>> Example 3: Streaming with tool usage\n");
await foreach (var update in agent.RunStreamingAsync(
    "Tell me about Sydney's weather and population, then convert the temperature to Fahrenheit."))
{
    Console.Write(update);
}
Console.WriteLine("\n");

Console.WriteLine(">>> Example 4: Tool usage across multiple queries\n");
response = (await agent.RunAsync("What's colder: London or New York? Convert the coldest temperature to Fahrenheit.")).Text;
Console.WriteLine($"Response: {response}\n");

Console.WriteLine("✓ Agent with tools example completed!");
Console.WriteLine("\nKey Takeaway:");
Console.WriteLine("- Tools extend agent capabilities");
Console.WriteLine("- Use [Description] attributes for clarity");
Console.WriteLine("- Agent intelligently decides when to use which tool");
Console.WriteLine("- Still single-turn unless using sessions");
