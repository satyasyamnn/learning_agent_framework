// =============================================================================
// Sample 1: Supply Chain - Order Tracking Agent
// =============================================================================
// Demonstrates:
//   - Creating a ChatClientAgent using chatClient.AsAIAgent()
//   - Registering domain tools with AIFunctionFactory + [Description] attributes
//   - Running single-turn queries against supply chain domain data
//   - Streaming responses with RunStreamingAsync
// =============================================================================

using System.ComponentModel;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using MockDataServices.SupplyChain;
using OpenAI.Chat;
using SharedModels.SupplyChain;

// ── Configuration ─────────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var endpointUrl    = config["AzureOpenAI:Endpoint"]!;
var deploymentName = config["AzureOpenAI:DeploymentName"]!;
var apiKey         = config["AzureOpenAI:ApiKey"]!;

// Extract just the authority part from the project-scoped endpoint
var endpoint = new Uri(new Uri(endpointUrl).GetLeftPart(System.UriPartial.Authority));

// ── Services ──────────────────────────────────────────────────────────────────
var shipmentService = new MockShipmentService();
var orderService    = new MockOrderService();

// ── Tool Definitions ──────────────────────────────────────────────────────────

[Description("Retrieves the current status and tracking events for a shipment given its tracking number.")]
ShipmentStatusResult GetShipmentStatus(
    [Description("The shipment tracking number, e.g. TRK-001-2025")] string trackingNumber)
{
    var shipment = shipmentService.GetByTrackingNumber(trackingNumber);
    if (shipment is null)
        return new ShipmentStatusResult(false, null, $"No shipment found for '{trackingNumber}'.");

    var latest = shipment.Events.OrderByDescending(e => e.Timestamp).FirstOrDefault();
    return new ShipmentStatusResult(true, new ShipmentInfo(
        shipment.TrackingNumber, shipment.Status.ToString(),
        shipment.Origin, shipment.Destination, shipment.CarrierId,
        shipment.EstimatedDelivery.ToString("yyyy-MM-dd"),
        latest?.Description ?? "No events", latest?.Location ?? "Unknown"), null);
}

[Description("Returns all shipments that are currently delayed.")]
List<ShipmentInfo> GetDelayedShipments() =>
    shipmentService.GetDelayedShipments().Select(s =>
    {
        var e = s.Events.OrderByDescending(x => x.Timestamp).FirstOrDefault();
        return new ShipmentInfo(s.TrackingNumber, s.Status.ToString(), s.Origin, s.Destination,
            s.CarrierId, s.EstimatedDelivery.ToString("yyyy-MM-dd"),
            e?.Description ?? "No details", e?.Location ?? "Unknown");
    }).ToList();

[Description("Retrieves all orders placed with a specific supplier.")]
List<OrderSummary> GetOrdersBySupplier(
    [Description("The supplier ID, e.g. SUP-001")] string supplierId) =>
    orderService.GetBySupplier(supplierId).Select(o => new OrderSummary(
        o.OrderId, o.SupplierId, o.Status.ToString(),
        o.OrderDate.ToString("yyyy-MM-dd"), o.RequiredByDate.ToString("yyyy-MM-dd"),
        o.TotalValue, o.TrackingNumber)).ToList();

[Description("Flags a shipment as delayed and records the reason.")]
string FlagDelayedShipment(
    [Description("The tracking number of the delayed shipment")] string trackingNumber,
    [Description("A description of the delay reason")] string reason)
{
    var shipment = shipmentService.GetByTrackingNumber(trackingNumber);
    if (shipment is null) return $"Shipment '{trackingNumber}' not found.";
    shipmentService.UpdateStatus(trackingNumber, ShipmentStatus.Delayed);
    return $"Shipment '{trackingNumber}' flagged as delayed. Reason: {reason}. " +
           $"Carrier '{shipment.CarrierId}' has been notified.";
}

// ── Agent Setup ───────────────────────────────────────────────────────────────
// Use .AsAIAgent() extension from Microsoft.Agents.AI.OpenAI
var agent = new AzureOpenAIClient(endpoint, new System.ClientModel.ApiKeyCredential(apiKey))
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: """
            You are a supply chain logistics assistant for a global trading company.
            Help coordinators track shipments, identify delays, and manage supplier orders.
            Be precise with dates and tracking numbers. When you identify a delay,
            proactively suggest next steps such as contacting the carrier or finding alternatives.
            """,
        name: "OrderTrackingAgent",
        tools:
        [
            AIFunctionFactory.Create(GetShipmentStatus),
            AIFunctionFactory.Create(GetDelayedShipments),
            AIFunctionFactory.Create(GetOrdersBySupplier),
            AIFunctionFactory.Create(FlagDelayedShipment),
        ]
    );

// ── Demo Queries ──────────────────────────────────────────────────────────────
Console.WriteLine("=============================================================");
Console.WriteLine("  Supply Chain Order Tracking Agent — Microsoft Agent Framework");
Console.WriteLine("=============================================================");
Console.WriteLine();

var queries = new[]
{
    "What is the current status of shipment TRK-001-2025?",
    "Are there any delayed shipments right now? Give me a summary.",
    "Show me all orders placed with supplier SUP-002 and whether any are at risk.",
    "Flag shipment TRK-002-2025 as delayed due to severe Pacific weather conditions.",
};

foreach (var query in queries)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($">> {query}");
    Console.ResetColor();

    await foreach (var update in agent.RunStreamingAsync(query))
        Console.Write(update.Text);

    Console.WriteLine();
    Console.WriteLine(new string('-', 60));
    Console.WriteLine();
}

// ── Supporting Records ────────────────────────────────────────────────────────
record ShipmentStatusResult(bool Found, ShipmentInfo? Shipment, string? ErrorMessage);
record ShipmentInfo(string TrackingNumber, string Status, string Origin, string Destination,
    string Carrier, string EstimatedDelivery, string LatestEvent, string LatestLocation);
record OrderSummary(string OrderId, string SupplierId, string Status,
    string OrderDate, string RequiredByDate, decimal TotalValue, string TrackingNumber);
