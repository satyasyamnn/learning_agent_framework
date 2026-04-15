using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using MockDataServices.Customs;
using SharedModels.Customs;

// ─────────────────────────────────────────────────────────────────────────────
// Shared clearance context — flows through all six executors
// ─────────────────────────────────────────────────────────────────────────────
record ClearanceContext(
    CustomsShipment Shipment,
    bool DocumentsComplete    = false,
    List<string>? MissingDocs = null,
    bool ClassificationPassed = false,
    List<string>? ClassificationIssues = null,
    bool CompliancePassed     = false,
    int RiskScore             = 0,
    List<string>? ComplianceFlags = null,
    decimal TotalDuty        = 0,
    decimal TotalVat         = 0,
    List<string>? DutyBreakdown = null,
    bool Filed               = false,
    string? DeclarationRef   = null
);

internal static class Trace
{
    internal static void Step(string step, string shipmentId, string detail = "")
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"[{DateTime.UtcNow:HH:mm:ss.fff}] ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"[{step}] ");
        Console.ResetColor();
        Console.WriteLine($"{shipmentId}{(string.IsNullOrEmpty(detail) ? "" : $" — {detail}")}");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Executor 1: Document Collection
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class DocumentCollectionExecutor(
    AIAgent agent, MockDocumentService docService) : Executor("DocumentCollection")
{
    private static readonly DocumentType[] Required =
        [DocumentType.CommercialInvoice, DocumentType.PackingList, DocumentType.BillOfLading];

    [MessageHandler]
    private async ValueTask<ClearanceContext> HandleAsync(CustomsShipment shipment, IWorkflowContext wf)
    {
        Trace.Step("DocumentCollection", shipment.ShipmentId);

        var docs     = docService.GetByShipment(shipment.ShipmentId);
        var present  = docs.Select(d => d.Type).ToHashSet();
        var missing  = Required.Where(t => !present.Contains(t)).Select(t => t.ToString()).ToList();
        var missingFields = docs
            .SelectMany(d => d.Fields.Where(f => f.IsRequired && f.Value is null)
                .Select(f => $"{d.Type}: {f.Name}"))
            .ToList();

        var allIssues = missing.Select(m => $"Missing document: {m}")
            .Concat(missingFields.Select(f => $"Missing field: {f}")).ToList();

        var prompt = $"""
            Document completeness for {shipment.ShipmentId}:
            - Present: {string.Join(", ", present.Select(t => t.ToString()))}
            - Missing documents: {(missing.Any() ? string.Join(", ", missing) : "None")}
            - Missing required fields: {(missingFields.Any() ? string.Join("; ", missingFields) : "None")}
            One-paragraph assessment. Can we proceed to classification?
            """;

        var assessment = await agent.RunAsync(prompt);
        Console.WriteLine($"  {assessment.Text}");

        return new ClearanceContext(shipment,
            DocumentsComplete: !allIssues.Any(), MissingDocs: allIssues);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Executor 2: Classification
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class ClassificationExecutor(
    AIAgent agent, MockTariffService tariff) : Executor("Classification")
{
    [MessageHandler]
    private async ValueTask<ClearanceContext> HandleAsync(ClearanceContext ctx, IWorkflowContext wf)
    {
        Trace.Step("Classification", ctx.Shipment.ShipmentId);

        var issues = new List<string>();
        foreach (var line in ctx.Shipment.Lines)
        {
            var entry = tariff.Lookup(line.HsCode);
            if (entry is null)
                issues.Add($"Line {line.LineId}: HS {line.HsCode} not found in tariff database");
            else if (entry.RequiresLicense)
                issues.Add($"Line {line.LineId}: HS {line.HsCode} ({entry.Description}) requires import licence");
        }

        var prompt = $"""
            HS classification for {ctx.Shipment.ShipmentId}:
            {string.Join("\n", ctx.Shipment.Lines.Select(l =>
                $"  Line {l.LineId}: '{l.Description}' → HS {l.HsCode}, ${l.TotalValue:N2}"))}
            Issues: {(issues.Any() ? string.Join("; ", issues) : "None")}
            Confirm HS codes are appropriate. (2-3 sentences)
            """;

        var assessment = await agent.RunAsync(prompt);
        Console.WriteLine($"  {assessment.Text}");

        return ctx with { ClassificationPassed = !issues.Any(), ClassificationIssues = issues };
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Executor 3: Compliance
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class ComplianceExecutor(
    AIAgent agent, MockTariffService tariff) : Executor("Compliance")
{
    [MessageHandler]
    private async ValueTask<ClearanceContext> HandleAsync(ClearanceContext ctx, IWorkflowContext wf)
    {
        Trace.Step("Compliance", ctx.Shipment.ShipmentId);

        var flags = new List<string>();
        if (tariff.IsCountrySanctioned(ctx.Shipment.CountryOfOrigin))
            flags.Add($"Sanctioned origin: {ctx.Shipment.CountryOfOrigin}");
        foreach (var line in ctx.Shipment.Lines)
        {
            if (tariff.IsHsCodeRestricted(line.HsCode))
                flags.Add($"Restricted HS: {line.HsCode} ({line.Description})");
            if (line.IsDualUse)
                flags.Add($"Dual-use: {line.HsCode}");
            if (line.IsRestrictedGood)
                flags.Add($"Restricted good: {line.HsCode}");
        }

        int riskScore = Math.Min(ctx.Shipment.RiskScore + flags.Count * 2, 10);

        var prompt = $"""
            Compliance for {ctx.Shipment.ShipmentId}: origin {ctx.Shipment.CountryOfOrigin},
            importer {ctx.Shipment.ImporterName}, risk {riskScore}/10,
            flags: {(flags.Any() ? string.Join("; ", flags) : "None")}.
            2-sentence verdict.
            """;

        var assessment = await agent.RunAsync(prompt);
        Console.WriteLine($"  {assessment.Text}");

        if (riskScore > 7)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  HIGH RISK ({riskScore}/10) — flagged for physical inspection");
            Console.ResetColor();
        }

        return ctx with { CompliancePassed = !flags.Any(), RiskScore = riskScore, ComplianceFlags = flags };
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Executor 4: Duty Calculation (no LLM — pure arithmetic)
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class DutyCalculationExecutor(MockTariffService tariff) : Executor("DutyCalculation")  // primary ctor ✓
{
    [MessageHandler]
    private ValueTask<ClearanceContext> HandleAsync(ClearanceContext ctx, IWorkflowContext wf)
    {
        Trace.Step("DutyCalculation", ctx.Shipment.ShipmentId);

        decimal totalDuty = 0, totalVat = 0;
        var breakdown = new List<string>();

        foreach (var line in ctx.Shipment.Lines)
        {
            var duty = tariff.CalculateDuty(line.HsCode, line.TotalValue);
            var vat  = tariff.CalculateVat(line.HsCode, line.TotalValue, duty);
            totalDuty += duty;
            totalVat  += vat;
            var rate = tariff.Lookup(line.HsCode)?.DutyRatePercent ?? 0;
            breakdown.Add($"HS {line.HsCode}: ${line.TotalValue:N2} × {rate}% = ${duty:N2} duty, ${vat:N2} VAT");
        }

        Console.WriteLine($"  Duty: ${totalDuty:N2} | VAT: ${totalVat:N2}");

        return ValueTask.FromResult(ctx with
        {
            TotalDuty = totalDuty, TotalVat = totalVat, DutyBreakdown = breakdown
        });
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Executor 5: Filing
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class FilingExecutor(AIAgent agent) : Executor("Filing")
{
    [MessageHandler]
    private async ValueTask<ClearanceContext> HandleAsync(ClearanceContext ctx, IWorkflowContext wf)
    {
        Trace.Step("Filing", ctx.Shipment.ShipmentId);

        var declRef = $"CDEC-{ctx.Shipment.ShipmentId}-{DateTime.UtcNow:yyyyMMddHHmm}";

        var prompt = $"""
            Customs declaration filing summary:
            - Ref: {declRef} | Shipment: {ctx.Shipment.ShipmentId}
            - Importer: {ctx.Shipment.ImporterName} (EORI: {ctx.Shipment.ImporterEori})
            - Port: {ctx.Shipment.PortOfEntry} | Value: ${ctx.Shipment.TotalDeclaredValue:N2} {ctx.Shipment.CurrencyCode}
            - Duty: ${ctx.TotalDuty:N2} | VAT: ${ctx.TotalVat:N2} | Risk: {ctx.RiskScore}/10
            2-paragraph filing confirmation note.
            """;

        var confirmation = await agent.RunAsync(prompt);
        Console.WriteLine($"  Declaration ref: {declRef}");
        Console.WriteLine($"  {confirmation.Text}");

        return ctx with { Filed = true, DeclarationRef = declRef };
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Executor 6: Status — issues final clearance certificate
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class StatusExecutor(AIAgent agent) : Executor("Status")
{
    [MessageHandler]
    private async ValueTask HandleAsync(ClearanceContext ctx, IWorkflowContext wf)
    {
        Trace.Step("Status", ctx.Shipment.ShipmentId, "Generating clearance certificate");

        var overallStatus = (ctx.ComplianceFlags?.Any() == true && ctx.RiskScore > 7)
            ? "HELD FOR INSPECTION"
            : "CLEARED FOR IMPORT";

        var prompt = $"""
            Formal customs clearance certificate:
            - Shipment: {ctx.Shipment.ShipmentId} | Declaration: {ctx.DeclarationRef}
            - Importer: {ctx.Shipment.ImporterName} | Port: {ctx.Shipment.PortOfEntry}
            - Status: {overallStatus} | Duty: ${ctx.TotalDuty:N2} | VAT: ${ctx.TotalVat:N2}
            - Risk: {ctx.RiskScore}/10 | Documents complete: {ctx.DocumentsComplete}
            4-5 sentence formal certificate suitable for importer's records.
            """;

        var certificate = await agent.RunAsync(prompt);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n{'='.ToString().PadRight(60, '=')}");
        Console.WriteLine("  CUSTOMS CLEARANCE CERTIFICATE");
        Console.WriteLine($"{'='.ToString().PadRight(60, '=')}");
        Console.ResetColor();
        Console.WriteLine(certificate.Text);

        await wf.YieldOutputAsync(
            $"[{overallStatus}] {ctx.Shipment.ShipmentId} | Ref: {ctx.DeclarationRef} | " +
            $"Duty: ${ctx.TotalDuty:N2} | VAT: ${ctx.TotalVat:N2} | Risk: {ctx.RiskScore}/10",
            CancellationToken.None);
    }
}
