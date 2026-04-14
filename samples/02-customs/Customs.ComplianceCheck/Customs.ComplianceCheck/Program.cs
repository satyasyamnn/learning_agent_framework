// =============================================================================
// Sample 5: Customs - Compliance Check Workflow with Human-in-the-Loop
// =============================================================================
// Demonstrates:
//   - Five-executor workflow with BindExecutor pattern
//   - Conditional edge routing (risk score > 6 triggers human review)
//   - Human officer approval gate mid-pipeline
//   - WorkflowOutputEvent for collecting final results
// =============================================================================

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Configuration;
using MockDataServices.Customs;
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
var shipmentService = new MockCustomsShipmentService();
var tariffService   = new MockTariffService();

// ── LLM client factory ────────────────────────────────────────────────────────
var azureOpenAI = new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(apiKey));

AIAgent CreateAgent(string name, string instructions) =>
    azureOpenAI.GetChatClient(deploymentName).AsAIAgent(instructions: instructions, name: name);

// ── Executor instances ────────────────────────────────────────────────────────
var sanctions   = new SanctionsScreeningExecutor(
    CreateAgent("SanctionsAgent", "You are a trade compliance sanctions specialist."), tariffService);
var restricted  = new RestrictedGoodsExecutor(
    CreateAgent("RestrictedAgent", "You are a restricted and dual-use goods specialist."), tariffService);
var riskScoring = new RiskScoringExecutor();
var humanReview = new HumanReviewExecutor(
    CreateAgent("HumanReviewAgent", "You are a customs officer who prepares risk briefings."));
var tariffCalc  = new TariffCalculationExecutor(tariffService);

// ── Build Workflow ─────────────────────────────────────────────────────────────
//
//  [Sanctions] → [RestrictedGoods] → [RiskScoring] ─ score > 6 ──→ [HumanReview] ──→ [TariffCalc]
//                                                  └─ score ≤ 6 ──────────────────────────────────┘
//
var bSanctions   = sanctions.BindExecutor();
var bRestricted  = restricted.BindExecutor();
var bRiskScoring = riskScoring.BindExecutor();
var bHumanReview = humanReview.BindExecutor();
var bTariffCalc  = tariffCalc.BindExecutor();

var workflow = new WorkflowBuilder("CustomsComplianceWorkflow")
    .BindExecutor(bSanctions)
    .BindExecutor(bRestricted)
    .BindExecutor(bRiskScoring)
    .BindExecutor(bHumanReview)
    .BindExecutor(bTariffCalc)
    .AddEdge(bSanctions,   bRestricted)
    .AddEdge(bRestricted,  bRiskScoring)
    .AddEdge<ComplianceContext>(bRiskScoring, bHumanReview, condition: msg => msg != null && msg.RequiresHumanReview)
    .AddEdge(bHumanReview, bTariffCalc)
    .AddEdge<ComplianceContext>(bRiskScoring, bTariffCalc,  condition: msg => msg != null && !msg.RequiresHumanReview)
    .Build();

// ── Process Shipments ─────────────────────────────────────────────────────────
Console.WriteLine("=============================================================");
Console.WriteLine("  Customs Compliance Check — Workflow with Human-in-the-Loop");
Console.WriteLine("=============================================================");
Console.WriteLine();

// CSH-3001: Standard electronics (low risk, auto-cleared)
// CSH-3002: Dual-use gyroscopes (medium risk)
// CSH-3004: Sanctioned country + restricted goods (high risk, officer review)
var shipmentIds = new[] { "CSH-3001", "CSH-3002", "CSH-3004" };

foreach (var shipmentId in shipmentIds)
{
    var shipment = shipmentService.GetById(shipmentId);
    if (shipment is null) { Console.WriteLine($"Shipment {shipmentId} not found."); continue; }

    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine($"\nProcessing: {shipmentId} | {shipment.ImporterName} | Origin: {shipment.CountryOfOrigin}");
    Console.ResetColor();

    var run = await InProcessExecution.Default.RunAsync(workflow, shipment);

    Console.ForegroundColor = ConsoleColor.Green;
    foreach (var evt in run.OutgoingEvents)
    {
        if (evt is WorkflowOutputEvent outputEvt)
            Console.WriteLine($"\n[Final Result] {outputEvt.Data}");
    }
    Console.ResetColor();
    Console.WriteLine(new string('=', 60));
}
