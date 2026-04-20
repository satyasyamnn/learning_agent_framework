// =============================================================================
// Sample 2: Supply Chain - Disruption Alert Agent (Multi-Turn + Memory)
// =============================================================================
// Demonstrates:
//   - AgentSession for persistent multi-turn conversations
//   - Agent accumulates context across turns (remembers disruptions discussed)
//   - Session serialization via SerializeSessionAsync / DeserializeSessionAsync
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
var supplierService = new MockSupplierService();
var shipmentService = new MockShipmentService();
var orderService    = new MockOrderService();

// ── Tool Definitions ──────────────────────────────────────────────────────────

[Description("Gets all active supplier disruptions reported in the last 30 days.")]
List<DisruptionInfo> GetActiveDisruptions() =>
    supplierService.GetActiveDisruptions().Select(d => new DisruptionInfo(
        d.SupplierId, supplierService.GetById(d.SupplierId)?.Name ?? "Unknown",
        d.DisruptionType, d.Description, d.Severity.ToString(),
        d.EstimatedRecoveryDays, d.ReportedAt.ToString("yyyy-MM-dd"))).ToList();

[Description("Gets the full profile of a supplier including reliability score, lead time, capacity and status.")]
SupplierProfile? GetSupplierProfile([Description("The supplier ID, e.g. SUP-002")] string supplierId)
{
    var s = supplierService.GetById(supplierId);
    if (s is null) return null;
    return new SupplierProfile(s.SupplierId, s.Name, s.Country, s.Status.ToString(),
        s.ReliabilityScore, s.LeadTimeDays, s.PriceIndex, s.AvailableCapacity, s.ProductCategories);
}

[Description("Finds active alternative suppliers for given product categories, ranked by reliability score.")]
List<AlternativeSupplier> FindAlternativeSuppliers(
    [Description("The ID of the disrupted supplier to exclude")] string disruptedSupplierId,
    [Description("Comma-separated required product categories, e.g. 'Electronics,Semiconductors'")] string categories)
{
    var cats = categories.Split(',').Select(c => c.Trim()).ToList();
    return supplierService.GetActiveAlternatives(disruptedSupplierId, cats)
        .Select(s => new AlternativeSupplier(s.SupplierId, s.Name, s.Country,
            s.ReliabilityScore, s.LeadTimeDays, s.PriceIndex, s.AvailableCapacity))
        .ToList();
}

[Description("Gets all open orders affected by a disruption at a given supplier.")]
List<AffectedOrder> GetAffectedOrders([Description("The supplier ID")] string supplierId) =>
    orderService.GetBySupplier(supplierId)
        .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
        .Select(o => new AffectedOrder(o.OrderId, o.CustomerId, o.Status.ToString(),
            o.RequiredByDate.ToString("yyyy-MM-dd"), o.TotalValue, o.TrackingNumber,
            (o.RequiredByDate - DateTime.UtcNow).Days))
        .ToList();

[Description("Gets all shipments currently in transit or delayed.")]
List<ShipmentRisk> GetInTransitShipments() =>
    shipmentService.GetShipmentsByStatus(ShipmentStatus.InTransit)
        .Concat(shipmentService.GetDelayedShipments())
        .Select(s =>
        {
            var e = s.Events.OrderByDescending(x => x.Timestamp).FirstOrDefault();
            return new ShipmentRisk(s.TrackingNumber, s.CarrierId, s.Status.ToString(),
                s.Destination, s.EstimatedDelivery.ToString("yyyy-MM-dd"),
                e?.Description ?? "No update");
        }).ToList();

// ── Agent Setup ───────────────────────────────────────────────────────────────
var agent = new AzureOpenAIClient(endpoint, new System.ClientModel.ApiKeyCredential(apiKey))
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: """
            You are a supply chain risk analyst for a global manufacturing company.
            Monitor supplier disruptions, assess their impact on open orders,
            and recommend mitigation strategies.

            In every response:
            - Reference information shared in earlier turns of this conversation
            - Prioritise actions by business impact (value at risk, delivery deadlines)
            - Suggest specific alternative suppliers when appropriate
            - Use bullet points for action items
            """,
        name: "DisruptionAlertAgent",
        tools:
        [
            AIFunctionFactory.Create(GetActiveDisruptions),
            AIFunctionFactory.Create(GetSupplierProfile),
            AIFunctionFactory.Create(FindAlternativeSuppliers),
            AIFunctionFactory.Create(GetAffectedOrders),
            AIFunctionFactory.Create(GetInTransitShipments),
        ]
    );

// ── Multi-Turn Conversation ───────────────────────────────────────────────────
Console.WriteLine("=============================================================");
Console.WriteLine("  Supply Chain Disruption Alert Agent — Multi-Turn Session");
Console.WriteLine("=============================================================");
Console.WriteLine();

AgentSession session = await agent.CreateSessionAsync();

var turns = new[]
{
    "What supplier disruptions are active right now? Give me an overview.",
    "For the disrupted supplier you just identified, what is their reliability profile and what open orders do we have with them?",
    "Given the disruption and affected orders you have described, which alternative suppliers should we approach first? Factor in lead times and our delivery deadlines.",
    "Are there any in-transit shipments that might also be at risk? Summarise the total exposure across all active issues.",
};

foreach (var (turn, index) in turns.Select((t, i) => (t, i + 1)))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[Turn {index}] {turn}");
    Console.ResetColor();
    Console.WriteLine();

    await foreach (var update in agent.RunStreamingAsync(turn, session))
        Console.Write(update.Text);

    Console.WriteLine();
    Console.WriteLine(new string('-', 60));
    Console.WriteLine();
}

// ── Session Serialization Demo ────────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("[Demo] Serializing and resuming session...");
Console.ResetColor();
Console.WriteLine();

// Persist the session (e.g. to a database), then restore it
var serialized     = await agent.SerializeSessionAsync(session);
var resumedSession = await agent.DeserializeSessionAsync(serialized);

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("[Turn 5 — Resumed Session] What did we decide about the supplier disruption?");
Console.ResetColor();
Console.WriteLine();

await foreach (var update in agent.RunStreamingAsync(
    "What was the primary supplier disruption we discussed earlier, and what actions did we agree on?",
    resumedSession))
    Console.Write(update.Text);

Console.WriteLine();

// ── Supporting Records ────────────────────────────────────────────────────────
record DisruptionInfo(string SupplierId, string SupplierName, string DisruptionType,
    string Description, string Severity, int EstimatedRecoveryDays, string ReportedAt);
record SupplierProfile(string SupplierId, string Name, string Country, string Status,
    int ReliabilityScore, int LeadTimeDays, decimal PriceIndex, int AvailableCapacity,
    List<string> Categories);
record AlternativeSupplier(string SupplierId, string Name, string Country,
    int ReliabilityScore, int LeadTimeDays, decimal PriceIndex, int AvailableCapacity);
record AffectedOrder(string OrderId, string CustomerId, string Status,
    string RequiredByDate, decimal TotalValue, string TrackingNumber, int DaysUntilDue);
record ShipmentRisk(string TrackingNumber, string Carrier, string Status,
    string Destination, string EstimatedDelivery, string LatestUpdate);
