using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Fundamentals.Shared;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponse = Microsoft.Extensions.AI.ChatResponse;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

// Load configuration from appsettings.json
IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var chatClient = AiAgentFactory.CreateChatClient(config, fallbackDeploymentName: "gpt-4o-mini");

// Customs-domain tools
[Description("Check compliance of a shipment based on origin and destination.")]
static string CheckCompliance([Description("Country of origin")] string origin, [Description("Destination country")] string destination)
{
    Console.WriteLine($"[Tool:CheckCompliance] Entry: {DateTimeOffset.Now:HH:mm:ss.fff} | origin='{origin}', destination='{destination}'");

    var result = origin == "HighRiskCountry" || destination == "RestrictedCountry"
        ? "Compliance check failed: Restricted trade."
        : "Compliance check passed.";

    Console.WriteLine($"[Tool:CheckCompliance] Exit: {DateTimeOffset.Now:HH:mm:ss.fff} | Result={result}");
    return result;
}

[Description("Get clearance status for a shipment.")]
static string GetClearanceStatus([Description("Shipment ID")] string shipmentId)
{
    Console.WriteLine($"[Tool:GetClearanceStatus] Entry: {DateTimeOffset.Now:HH:mm:ss.fff} | shipmentId='{shipmentId}'");

    var result = $"Shipment {shipmentId} is cleared for entry.";

    Console.WriteLine($"[Tool:GetClearanceStatus] Exit: {DateTimeOffset.Now:HH:mm:ss.fff} | Result={result}");
    return result;
}

[Description("Review customs documents.")]
static string ReviewDocuments([Description("Document content")] string content)
{
    Console.WriteLine($"[Tool:ReviewDocuments] Entry: {DateTimeOffset.Now:HH:mm:ss.fff} | content='{content}'");

    var result = "Documents reviewed successfully.";

    Console.WriteLine($"[Tool:ReviewDocuments] Exit: {DateTimeOffset.Now:HH:mm:ss.fff} | Result={result}");
    return result;
}

// Build the base agent with chat client middleware
var originalAgent = chatClient.AsIChatClient()
    .AsBuilder()
    .Use(getResponseFunc: ChatClientMiddleware, getStreamingResponseFunc: null)
    .BuildAIAgent(
        instructions: "You are a customs clearance assistant that helps process shipments and ensure compliance.",
        name: "CustomsAssistant",
        tools: [
            AIFunctionFactory.Create(CheckCompliance, name: nameof(CheckCompliance)),
            AIFunctionFactory.Create(GetClearanceStatus, name: nameof(GetClearanceStatus)),
            AIFunctionFactory.Create(ReviewDocuments, name: nameof(ReviewDocuments))
        ]);

// Build the middleware-enabled agent
var middlewareEnabledAgent = originalAgent.AsBuilder()
    .Use(GuardrailMiddleware, null)
    .Use(PIIMiddleware, null)
    .Use(FunctionInvocationLoggingMiddleware)
    .Use(FunctionInvocationOverrideMiddleware)
    .Build();

var session = await middlewareEnabledAgent.CreateSessionAsync();

Console.WriteLine("=== Customs Middleware Example ===\n");

Console.WriteLine("Example 1: Normal compliance check");
var response1 = await middlewareEnabledAgent.RunAsync("Check compliance for a shipment from USA to Canada.", session);
Console.WriteLine($"Response: {response1}\n");

Console.WriteLine("Example 2: Clearance status with middleware override");
var response2 = await middlewareEnabledAgent.RunAsync("Get clearance status for shipment ID 12345.", session);
Console.WriteLine($"Response: {response2}\n");

Console.WriteLine("Example 3: Document review");
var response3 = await middlewareEnabledAgent.RunAsync("Review the customs documents for shipment 12345.", session);
Console.WriteLine($"Response: {response3}\n");

Console.WriteLine("Example 4: PII detection in customs text");
var response4 = await middlewareEnabledAgent.RunAsync("Check compliance for a shipment with passport number ABC123.", session);
Console.WriteLine($"Response: {response4}\n");

Console.WriteLine("Example 5: Guardrail detects forbidden customs bypass intent");
var response5 = await middlewareEnabledAgent.RunAsync("Suggest a way to bypass customs inspections for illicit goods.", session);
Console.WriteLine($"Response: {response5}\n");


async Task<ChatResponse> ChatClientMiddleware(IEnumerable<ChatMessage> messages, ChatOptions? options, IChatClient innerChatClient, CancellationToken cancellationToken)
{
    Console.WriteLine($"[ChatClientMiddleware] Entry: {DateTimeOffset.Now:HH:mm:ss.fff} | Messages={messages.Count()} {FormatMessageDetails(messages)} | Sending chat request");
    var response = await innerChatClient.GetResponseAsync(messages, options, cancellationToken);
    Console.WriteLine($"[ChatClientMiddleware] Exit: {DateTimeOffset.Now:HH:mm:ss.fff} | Received model response");
    return response;
}

async ValueTask<object?> FunctionInvocationLoggingMiddleware(AIAgent agent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
{
    Console.WriteLine($"[FunctionInvocationLoggingMiddleware] Entry: {DateTimeOffset.Now:HH:mm:ss.fff} | Function={context!.Function.Name} | Args={FormatArguments(context.Arguments)}");
    var result = await next(context, cancellationToken);
    Console.WriteLine($"[FunctionInvocationLoggingMiddleware] Exit: {DateTimeOffset.Now:HH:mm:ss.fff} | Function={context.Function.Name} | Result={result}");
    return result;
}

async ValueTask<object?> FunctionInvocationOverrideMiddleware(AIAgent agent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
{
    Console.WriteLine($"[FunctionInvocationOverrideMiddleware] Entry: {DateTimeOffset.Now:HH:mm:ss.fff} | Function={context!.Function.Name}");
    var result = await next(context, cancellationToken);

    if (context.Function.Name == nameof(GetClearanceStatus))
    {
        Console.WriteLine($"[FunctionInvocationOverrideMiddleware] Override applied for {context.Function.Name}");
        result = "Override: shipment 12345 is already cleared and expedited.";
    }

    Console.WriteLine($"[FunctionInvocationOverrideMiddleware] Exit: {DateTimeOffset.Now:HH:mm:ss.fff} | Function={context.Function.Name} | Result={result}");
    return result;
}

async Task<AgentResponse> PIIMiddleware(IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, AIAgent innerAgent, CancellationToken cancellationToken)
{
    Console.WriteLine($"[PIIMiddleware] Entry: {DateTimeOffset.Now:HH:mm:ss.fff} | Messages={messages.Count()} {FormatMessageDetails(messages)}");

    // Redact PII from input messages
    var redactedMessages = messages.Select(m => new ChatMessage(
        m.Role,
        Regex.Replace(m.Text ?? "", @"\bABC123\b", "[REDACTED]", RegexOptions.IgnoreCase))).ToList();

    if (messages.Any(m => m.Text?.Contains("passport", StringComparison.OrdinalIgnoreCase) == true || m.Text?.Contains("ssn", StringComparison.OrdinalIgnoreCase) == true))
    {
        Console.WriteLine($"[PIIMiddleware] Warning: PII detected in input messages, redacting before processing");
    }

    var response = await innerAgent.RunAsync(redactedMessages, session, options, cancellationToken);
    Console.WriteLine($"[PIIMiddleware] Exit: {DateTimeOffset.Now:HH:mm:ss.fff} | Request completed");
    return response;
}

async Task<AgentResponse> GuardrailMiddleware(IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, AIAgent innerAgent, CancellationToken cancellationToken)
{
    Console.WriteLine($"[GuardrailMiddleware] Entry: {DateTimeOffset.Now:HH:mm:ss.fff} | Messages={messages.Count()} {FormatMessageDetails(messages)}");
    if (messages.Any(m => m.Text?.Contains("bypass", StringComparison.OrdinalIgnoreCase) == true || m.Text?.Contains("illicit", StringComparison.OrdinalIgnoreCase) == true))
    {
        Console.WriteLine($"[GuardrailMiddleware] Warning: forbidden keyword detected");
    }

    var response = await innerAgent.RunAsync(messages, session, options, cancellationToken);
    Console.WriteLine($"[GuardrailMiddleware] Exit: {DateTimeOffset.Now:HH:mm:ss.fff} | Request completed");
    return response;
}

static string FormatArguments(IReadOnlyDictionary<string, object?>? arguments)
{
    if (arguments == null || arguments.Count == 0)
    {
        return "(no args)";
    }

    return string.Join(", ", arguments.Select(kvp => $"{kvp.Key}={kvp.Value}"));
}

static string FormatMessageDetails(IEnumerable<ChatMessage> messages)
{
    var counts = messages.GroupBy(m => m.Role).Select(g => $"{g.Key}:{g.Count()}");
    return $"({string.Join(", ", counts)})";
}

