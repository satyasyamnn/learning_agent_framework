// =============================================================================
// Sample 3: Supply Chain - Supplier Orchestration Workflow (Multi-Agent)
// =============================================================================
// Demonstrates:
//   - WorkflowBuilder with BindExecutor pattern (executor → ExecutorBinding → workflow)
//   - Three specialised agent-backed executors (defined in Executors.cs):
//       1. SupplierEvaluatorExecutor  — scores and ranks alternative suppliers
//       2. NegotiationExecutor        — simulates negotiation with human approval gate
//       3. ErpUpdateExecutor          — updates records and notifies stakeholders
//   - InProcessExecution.Default.RunAsync to run the pipeline
//   - WorkflowOutputEvent for structured output collection
// =============================================================================

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Configuration;
using MockDataServices.SupplyChain;
using OpenAI.Chat;

// ── Configuration ─────────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var endpoint       = config["AzureOpenAI:Endpoint"]!;
var deploymentName = config["AzureOpenAI:DeploymentName"]!;
var apiKey         = config["AzureOpenAI:ApiKey"]!;

// ── Services ──────────────────────────────────────────────────────────────────
var supplierService = new MockSupplierService();
var orderService    = new MockOrderService();

// ── LLM client factory ────────────────────────────────────────────────────────
var azureOpenAI = new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(apiKey));

AIAgent CreateAgent(string name, string instructions) =>
    azureOpenAI.GetChatClient(deploymentName).AsAIAgent(instructions: instructions, name: name);

// ── Executor instances ────────────────────────────────────────────────────────
var evaluatorAgent   = CreateAgent("SupplierEvaluator",
    "You are a procurement specialist. Evaluate supplier alternatives objectively using data.");
var negotiationAgent = CreateAgent("NegotiationAgent",
    "You are a commercial negotiator. Propose realistic contract terms for emergency procurement.");
var erpAgent         = CreateAgent("ERPUpdateAgent",
    "You are a supply chain coordinator. Draft clear stakeholder notifications about supply changes.");

var evaluator  = new SupplierEvaluatorExecutor(evaluatorAgent, supplierService);
var negotiator = new NegotiationExecutor(negotiationAgent);
var erpUpdater = new ErpUpdateExecutor(erpAgent, orderService);

// ── Build Workflow ─────────────────────────────────────────────────────────────
// executor.BindExecutor() creates the ExecutorBinding (used in AddEdge).
// builder.BindExecutor(binding) registers it and returns the builder (fluent).
var evalBinding  = evaluator.BindExecutor();
var negBinding   = negotiator.BindExecutor();
var erpBinding   = erpUpdater.BindExecutor();

var workflow = new WorkflowBuilder("SupplierOrchestration")
    .BindExecutor(evalBinding)
    .BindExecutor(negBinding)
    .BindExecutor(erpBinding)
    .AddEdge(evalBinding, negBinding)
    .AddEdge(negBinding, erpBinding)
    .Build();

// ── Run Workflow ──────────────────────────────────────────────────────────────
Console.WriteLine("=============================================================");
Console.WriteLine("  Supply Chain Supplier Orchestration — Multi-Agent Workflow");
Console.WriteLine("=============================================================");
Console.WriteLine();

// Trigger: SUP-002 factory fire disruption
var disruption = new DisruptionEvent(
    SupplierId: "SUP-002",
    SupplierName: "GlobalTech Components",
    RequiredCategories: ["Electronics", "Mechanical Parts"],
    TotalOrderValue: 1700m,
    DaysUntilDeadline: 5
);

Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine($"Triggering workflow: Disruption at {disruption.SupplierId} ({disruption.SupplierName})");
Console.WriteLine($"  Open order value at risk: ${disruption.TotalOrderValue:N2}");
Console.WriteLine($"  Days until deadline:      {disruption.DaysUntilDeadline}");
Console.ResetColor();
Console.WriteLine();

var run = await InProcessExecution.Default.RunAsync(workflow, disruption);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\n[Workflow Outputs]");
Console.ResetColor();

foreach (var evt in run.OutgoingEvents)
{
    if (evt is WorkflowOutputEvent outputEvt)
        Console.WriteLine($"  >> {outputEvt.Data}");
}

// ── Domain Records ────────────────────────────────────────────────────────────
record DisruptionEvent(
    string SupplierId,
    string SupplierName,
    List<string> RequiredCategories,
    decimal TotalOrderValue,
    int DaysUntilDeadline
);

record EvaluationResult(
    string DisruptedSupplierId,
    string RecommendedSupplierId,
    string RecommendedSupplierName,
    string RecommendedCountry,
    int ReliabilityScore,
    int LeadTimeDays,
    decimal TotalOrderValue,
    string EvaluationSummary
);

record NegotiationResult(
    string DisruptedSupplierId,
    string NewSupplierId,
    bool Approved,
    decimal PricePremiumPercent,
    decimal ExpediteFee,
    decimal PenaltyPerDayLate,
    string NegotiationSummary
);
