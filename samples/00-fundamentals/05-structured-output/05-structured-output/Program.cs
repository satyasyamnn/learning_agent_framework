using System.Text.Json;
using Fundamentals.Shared;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Console.WriteLine("=== Structured Output for Customs Clearance ===\n");

ChatClient chatClient = AiAgentFactory.CreateChatClient(config);
JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };

await UseStructuredOutputWithResponseFormatAsync(chatClient, jsonOptions);
await UseStructuredOutputWithRunAsyncAsync(chatClient);
await UseStructuredOutputWithRunStreamingAsync(chatClient, jsonOptions);

static async Task UseStructuredOutputWithRunAsyncAsync(ChatClient chatClient)
{
    Console.WriteLine(">>> 2) Structured output with RunAsync<T>\n");

    AIAgent agent = chatClient.AsAIAgent(
        name: "CustomsTypedOutputAgent",
        instructions: "You are a customs clearance analyst. Return only valid JSON matching the requested schema.");

    AgentResponse<CustomsClearanceAssessment> response = await agent.RunAsync<CustomsClearanceAssessment>(
        "Assess shipment CSH-4002 to the United Arab Emirates with HS code 870899, declared value 98000 USD, and duty rate 5.0%. " +
        "Include risk level, required documents, and recommended next action.");

    Console.WriteLine("Assistant Output (Typed Object):");
    PrintAssessment(response.Result);
    Console.WriteLine();
}

static AIAgent CreateStructuredAgent(ChatClient chatClient, string name) =>
    chatClient.AsAIAgent(new ChatClientAgentOptions
    {
        Name = name,
        ChatOptions = new()
        {
            Instructions = "You are a customs clearance analyst. Return only valid JSON matching the requested schema.",
            ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema<CustomsClearanceAssessment>()
        }
    });

static async Task UseStructuredOutputWithResponseFormatAsync(ChatClient chatClient, JsonSerializerOptions jsonOptions)
{
    Console.WriteLine(">>> 1) Structured output with ResponseFormat\n");

    AIAgent agent = CreateStructuredAgent(chatClient, "CustomsStructuredOutputAgent");

    AgentResponse response = await agent.RunAsync(
        "Assess shipment CSH-3017 to Germany with HS code 854231, declared value 125000 USD, and a duty rate of 4.2%. " +
        "Include risk level, required documents, and a concise recommended action.");

    Console.WriteLine("Assistant Output (JSON):");
    Console.WriteLine(response.Text);
    DeserializeAndPrint(response.Text, jsonOptions, "\nAssistant Output (Deserialized):");
}

static async Task UseStructuredOutputWithRunStreamingAsync(ChatClient chatClient, JsonSerializerOptions jsonOptions)
{
    Console.WriteLine(">>> 3) Structured output with RunStreamingAsync\n");

    AIAgent agent = CreateStructuredAgent(chatClient, "CustomsStreamingStructuredOutputAgent");

    Console.WriteLine("Streaming JSON Output:");
    IAsyncEnumerable<AgentResponseUpdate> updates = agent.RunStreamingAsync(
        "Assess shipment CSH-5144 to Canada with HS code 847330, declared value 64000 USD, and duty rate 3.5%. " +
        "Include risk level, required documents, and recommended next action.");

    AgentResponse response = await updates.ToAgentResponseAsync();
    Console.WriteLine(response.Text);
    DeserializeAndPrint(response.Text, jsonOptions, "\n\nAssembled Structured Output:");
}


static void DeserializeAndPrint(string json, JsonSerializerOptions options, string label)
{
    CustomsClearanceAssessment assessment = JsonSerializer.Deserialize<CustomsClearanceAssessment>(json, options)
                                            ?? throw new InvalidOperationException("Failed to deserialize structured output.");
    Console.WriteLine(label);
    PrintAssessment(assessment);
    Console.WriteLine();
}


static void PrintAssessment(CustomsClearanceAssessment assessment)
{
    Console.WriteLine($"ShipmentId: {assessment.ShipmentId}");
    Console.WriteLine($"DestinationCountry: {assessment.DestinationCountry}");
    Console.WriteLine($"HsCode: {assessment.HsCode}");
    Console.WriteLine($"DeclaredValueUsd: {assessment.DeclaredValueUsd}");
    Console.WriteLine($"DutyRatePercent: {assessment.DutyRatePercent}");
    Console.WriteLine($"EstimatedDutyUsd: {assessment.EstimatedDutyUsd}");
    Console.WriteLine($"RiskLevel: {assessment.RiskLevel}");
    Console.WriteLine($"RequiredDocuments: {string.Join(", ", assessment.RequiredDocuments)}");
    Console.WriteLine($"RecommendedAction: {assessment.RecommendedAction}");
}

internal sealed class CustomsClearanceAssessment
{
    public string ShipmentId { get; set; } = string.Empty;
    public string DestinationCountry { get; set; } = string.Empty;
    public string HsCode { get; set; } = string.Empty;
    public double DeclaredValueUsd { get; set; }
    public double DutyRatePercent { get; set; }
    public double EstimatedDutyUsd { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string[] RequiredDocuments { get; set; } = [];
    public string RecommendedAction { get; set; } = string.Empty;
}
