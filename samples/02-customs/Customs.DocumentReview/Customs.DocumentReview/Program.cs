// =============================================================================
// Sample 4: Customs - Document Review Agent
// =============================================================================
// Demonstrates:
//   - Single-agent pattern for trade document compliance review
//   - Tools that inspect commercial invoices, bills of lading, packing lists
//   - Missing field detection and HS code validation
//   - Clear GO / NO-GO recommendations per shipment
// =============================================================================

using System.ComponentModel;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using MockDataServices.Customs;
using OpenAI.Chat;
using SharedModels.Customs;

// ── Configuration ─────────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var endpoint       = config["AzureOpenAI:Endpoint"]!;
var deploymentName = config["AzureOpenAI:DeploymentName"]!;
var apiKey         = config["AzureOpenAI:ApiKey"]!;

// ── Services ──────────────────────────────────────────────────────────────────
var documentService = new MockDocumentService();
var tariffService   = new MockTariffService();
var shipmentService = new MockCustomsShipmentService();

// ── Tool Definitions ──────────────────────────────────────────────────────────

[Description("Lists all trade documents submitted for a customs shipment.")]
List<DocumentSummary> ListDocumentsForShipment(
    [Description("The customs shipment ID, e.g. CSH-3001")] string shipmentId) =>
    documentService.GetByShipment(shipmentId)
        .Select(d => new DocumentSummary(d.DocumentId, d.Type.ToString(), d.ShipmentId,
            d.IssuedDate.ToString("yyyy-MM-dd"),
            d.Fields.Count(f => f.IsRequired && f.Value is null),
            d.Fields.Count(f => f.IsRequired)))
        .ToList();

[Description("Returns all fields of a document and flags any required fields that are missing.")]
DocumentFieldReport ReviewDocumentFields(
    [Description("The document ID, e.g. DOC-CI-3001")] string documentId)
{
    var doc = documentService.GetById(documentId);
    if (doc is null) return new DocumentFieldReport(documentId, false, [], "Document not found.");

    var missing = doc.Fields.Where(f => f.IsRequired && f.Value is null).ToList();
    var fields  = doc.Fields.Select(f => new FieldDetail(f.Name, f.Value ?? "[MISSING]",
        f.IsRequired, f.IsRequired && f.Value is null)).ToList();

    return new DocumentFieldReport(documentId, !missing.Any(), fields,
        missing.Any()
            ? $"Missing {missing.Count} required field(s): {string.Join(", ", missing.Select(f => f.Name))}"
            : "All required fields present.");
}

[Description("Validates an HS code and returns description, duty rate, and licence requirement.")]
HsCodeValidation ValidateHsCode(
    [Description("The HS code, e.g. 8542.31")] string hsCode,
    [Description("ISO 2-letter country of origin, e.g. CN")] string countryOfOrigin)
{
    var entry = tariffService.Lookup(hsCode);
    if (entry is null)
        return new HsCodeValidation(hsCode, false, "Unknown HS code — not found in tariff database.", 0, 0, false);

    return new HsCodeValidation(hsCode, true,
        $"{entry.Description} (origin: {countryOfOrigin})",
        entry.DutyRatePercent, entry.VatRatePercent, entry.RequiresLicense);
}

[Description("Checks whether all required document types are present for a shipment.")]
DocumentCompletenessCheck CheckDocumentCompleteness(
    [Description("The customs shipment ID, e.g. CSH-3001")] string shipmentId)
{
    var docs    = documentService.GetByShipment(shipmentId);
    var present = docs.Select(d => d.Type).ToHashSet();
    var required = new[] { DocumentType.CommercialInvoice, DocumentType.PackingList, DocumentType.BillOfLading };
    var missing  = required.Where(t => !present.Contains(t)).ToList();

    return new DocumentCompletenessCheck(shipmentId, !missing.Any(),
        present.Select(t => t.ToString()).ToList(),
        missing.Select(t => t.ToString()).ToList());
}

[Description("Returns the full shipment details including declared lines and HS codes.")]
ShipmentDetail? GetShipmentDetails(
    [Description("The customs shipment ID, e.g. CSH-3001")] string shipmentId)
{
    var s = shipmentService.GetById(shipmentId);
    if (s is null) return null;
    return new ShipmentDetail(s.ShipmentId, s.ImporterName, s.ExporterName,
        s.CountryOfOrigin, s.PortOfEntry, s.TotalDeclaredValue, s.CurrencyCode,
        s.Lines.Select(l => new LineDetail(l.LineId, l.Description, l.HsCode,
            l.Quantity, l.TotalValue, l.CountryOfOrigin, l.IsDualUse, l.IsRestrictedGood))
            .ToList());
}

// ── Agent Setup ───────────────────────────────────────────────────────────────
var agent = new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(apiKey))
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: """
            You are a customs documentation specialist with expertise in international trade compliance.
            For each shipment review:
            1. Check all required document types are present (commercial invoice, packing list, bill of lading)
            2. Verify all required fields in each document are populated
            3. Validate HS codes match the described goods and flag any requiring import licences
            4. Highlight discrepancies between documents
            5. Give a clear GO / NO-GO recommendation with specific issues to resolve
            Be precise about document IDs and field names. Use bullet points for issues.
            """,
        name: "DocumentReviewAgent",
        tools:
        [
            AIFunctionFactory.Create(ListDocumentsForShipment),
            AIFunctionFactory.Create(ReviewDocumentFields),
            AIFunctionFactory.Create(ValidateHsCode),
            AIFunctionFactory.Create(CheckDocumentCompleteness),
            AIFunctionFactory.Create(GetShipmentDetails),
        ]
    );

// ── Demo Queries ──────────────────────────────────────────────────────────────
Console.WriteLine("=============================================================");
Console.WriteLine("  Customs Document Review Agent — Microsoft Agent Framework");
Console.WriteLine("=============================================================");
Console.WriteLine();

var queries = new[]
{
    ("CSH-3001",
     "Please review all trade documents for shipment CSH-3001. Are they complete and correct? Provide a filing recommendation."),
    ("CSH-3002",
     "Review the documents for shipment CSH-3002. What documents are missing and are there any special compliance concerns given the goods?"),
};

foreach (var (shipmentId, query) in queries)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($">> Shipment {shipmentId}: {query}");
    Console.ResetColor();
    Console.WriteLine();

    await foreach (var update in agent.RunStreamingAsync(query))
        Console.Write(update.Text);

    Console.WriteLine();
    Console.WriteLine(new string('-', 60));
    Console.WriteLine();
}

// ── Supporting Records ────────────────────────────────────────────────────────
record DocumentSummary(string DocumentId, string Type, string ShipmentId,
    string IssuedDate, int MissingRequiredFields, int TotalRequiredFields);
record FieldDetail(string Name, string Value, bool IsRequired, bool IsMissing);
record DocumentFieldReport(string DocumentId, bool IsComplete, List<FieldDetail> Fields, string Summary);
record HsCodeValidation(string HsCode, bool IsValid, string Description,
    decimal DutyRatePercent, decimal VatRatePercent, bool RequiresLicense);
record DocumentCompletenessCheck(string ShipmentId, bool IsComplete,
    List<string> PresentDocuments, List<string> MissingDocuments);
record LineDetail(string LineId, string Description, string HsCode, int Quantity,
    decimal TotalValue, string CountryOfOrigin, bool IsDualUse, bool IsRestricted);
record ShipmentDetail(string ShipmentId, string Importer, string Exporter,
    string CountryOfOrigin, string PortOfEntry, decimal TotalValue, string Currency,
    List<LineDetail> Lines);
