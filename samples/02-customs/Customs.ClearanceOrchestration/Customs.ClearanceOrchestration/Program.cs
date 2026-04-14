// =============================================================================
// Sample 6: Customs - End-to-End Clearance Orchestration (Advanced)
// =============================================================================
// Demonstrates:
//   - Complete customs clearance lifecycle as a multi-agent workflow
//   - Six specialised executors (defined in Executors.cs):
//       1. DocumentCollectionExecutor  — verifies required documents present
//       2. ClassificationExecutor      — validates HS codes and tariff entries
//       3. ComplianceExecutor          — sanctions + restricted goods screening
//       4. DutyCalculationExecutor     — computes customs duty and VAT
//       5. FilingExecutor              — prepares the customs declaration
//       6. StatusExecutor              — issues the final clearance certificate
//   - Timestamp tracing on each executor step
//   - WorkflowOutputEvent collection after InProcessExecution.Default.RunAsync
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
var documentService = new MockDocumentService();
var tariffService   = new MockTariffService();

// ── LLM client factory ────────────────────────────────────────────────────────
var azureOpenAI = new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(apiKey));

AIAgent CreateAgent(string name, string instructions) =>
    azureOpenAI.GetChatClient(deploymentName).AsAIAgent(instructions: instructions, name: name);

// ── Executor instances ────────────────────────────────────────────────────────
var docCollection  = new DocumentCollectionExecutor(
    CreateAgent("DocumentAgent",       "You are a trade documentation specialist."), documentService);
var classification = new ClassificationExecutor(
    CreateAgent("ClassificationAgent", "You are a tariff classification expert."), tariffService);
var compliance     = new ComplianceExecutor(
    CreateAgent("ComplianceAgent",     "You are a trade compliance and sanctions specialist."), tariffService);
var dutyCalc       = new DutyCalculationExecutor(tariffService);
var filing         = new FilingExecutor(
    CreateAgent("FilingAgent",         "You are a customs filing and declaration specialist."));
var status         = new StatusExecutor(
    CreateAgent("StatusAgent",         "You issue formal customs clearance certificates."));

// ── Build Workflow ─────────────────────────────────────────────────────────────
//
//  [DocCollection] → [Classification] → [Compliance] → [DutyCalc] → [Filing] → [Status]
//
var bDocColl    = docCollection.BindExecutor();
var bClassify   = classification.BindExecutor();
var bCompliance = compliance.BindExecutor();
var bDutyCalc   = dutyCalc.BindExecutor();
var bFiling     = filing.BindExecutor();
var bStatus     = status.BindExecutor();

var workflow = new WorkflowBuilder("ClearanceOrchestration")
    .BindExecutor(bDocColl)
    .BindExecutor(bClassify)
    .BindExecutor(bCompliance)
    .BindExecutor(bDutyCalc)
    .BindExecutor(bFiling)
    .BindExecutor(bStatus)
    .AddEdge(bDocColl,    bClassify)
    .AddEdge(bClassify,   bCompliance)
    .AddEdge(bCompliance, bDutyCalc)
    .AddEdge(bDutyCalc,   bFiling)
    .AddEdge(bFiling,     bStatus)
    .Build();

// ── Process Pending Shipments ─────────────────────────────────────────────────
Console.WriteLine("=============================================================");
Console.WriteLine("  Customs Clearance Orchestration — End-to-End Multi-Agent");
Console.WriteLine("=============================================================");
Console.WriteLine();

var shipments = shipmentService.GetPending();
Console.WriteLine($"Processing {shipments.Count} pending shipment(s)...\n");

foreach (var shipment in shipments)
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine($"Starting clearance: {shipment.ShipmentId} — {shipment.ImporterName}");
    Console.WriteLine($"  Origin: {shipment.CountryOfOrigin} | Port: {shipment.PortOfEntry} | Value: ${shipment.TotalDeclaredValue:N2}");
    Console.ResetColor();
    Console.WriteLine();

    var run = await InProcessExecution.Default.RunAsync(workflow, shipment);

    Console.ForegroundColor = ConsoleColor.Green;
    foreach (var evt in run.OutgoingEvents)
    {
        if (evt is WorkflowOutputEvent outputEvt)
            Console.WriteLine($"\n>> {outputEvt.Data}");
    }
    Console.ResetColor();

    Console.WriteLine();
    Console.WriteLine(new string('=', 60));
    Console.WriteLine();
}
