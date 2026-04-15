using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using MockDataServices.Customs;

// ─────────────────────────────────────────────────────────────────────────────
// Domain records shared across executors
// ─────────────────────────────────────────────────────────────────────────────
record ComplianceContext(
    SharedModels.Customs.CustomsShipment Shipment,
    bool PassedSanctions,
    List<string> SanctionsFlags,
    bool PassedRestrictedGoods,
    List<string> RestrictedFlags,
    int RiskScore,
    bool RequiresHumanReview,
    bool HumanApproved,
    decimal EstimatedDuty,
    decimal EstimatedVat
);

// ─────────────────────────────────────────────────────────────────────────────
// Executor 1: Sanctions Screening
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class SanctionsScreeningExecutor(AIAgent agent, MockTariffService tariff)
    : Executor("SanctionsScreening")
{
    [MessageHandler]
    private async ValueTask<ComplianceContext> HandleAsync(
        SharedModels.Customs.CustomsShipment shipment, IWorkflowContext ctx)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[SanctionsScreening] {shipment.ShipmentId} — origin: {shipment.CountryOfOrigin}");
        Console.ResetColor();

        var isSanctioned = tariff.IsCountrySanctioned(shipment.CountryOfOrigin);
        var flags = new List<string>();
        if (isSanctioned)
            flags.Add($"Country of origin '{shipment.CountryOfOrigin}' appears on sanctions list");
        if (shipment.Lines.Any(l => l.IsRestrictedGood))
            flags.Add("One or more line items flagged as restricted goods");

        var prompt = $"""
            Sanctions screening for shipment {shipment.ShipmentId}:
            - Importer: {shipment.ImporterName} (EORI: {shipment.ImporterEori})
            - Exporter: {shipment.ExporterName} | Origin: {shipment.CountryOfOrigin}
            - Flags: {(flags.Any() ? string.Join("; ", flags) : "None")}
            Provide a brief assessment (2-3 sentences).
            """;

        var assessment = await agent.RunAsync(prompt);
        Console.WriteLine(assessment.Text);

        return new ComplianceContext(
            shipment, PassedSanctions: !isSanctioned, SanctionsFlags: flags,
            PassedRestrictedGoods: true, RestrictedFlags: [],
            RiskScore: 0, RequiresHumanReview: false, HumanApproved: false,
            EstimatedDuty: 0, EstimatedVat: 0);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Executor 2: Restricted Goods Check
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class RestrictedGoodsExecutor(AIAgent agent, MockTariffService tariff)
    : Executor("RestrictedGoods")
{
    [MessageHandler]
    private async ValueTask<ComplianceContext> HandleAsync(ComplianceContext ctx, IWorkflowContext wf)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[RestrictedGoods] Checking {ctx.Shipment.Lines.Count} line items");
        Console.ResetColor();

        var flags = new List<string>();
        foreach (var line in ctx.Shipment.Lines)
        {
            if (tariff.IsHsCodeRestricted(line.HsCode))
                flags.Add($"HS {line.HsCode} ({line.Description}) — restricted/dual-use");
            if (line.IsDualUse)
                flags.Add($"HS {line.HsCode} ({line.Description}) — declared dual-use");
        }

        var prompt = $"""
            Restricted goods check for {ctx.Shipment.ShipmentId}:
            Lines: {ctx.Shipment.Lines.Count} | Issues: {(flags.Any() ? string.Join("; ", flags) : "None")}
            Provide a compliance assessment (2-3 sentences).
            """;

        var assessment = await agent.RunAsync(prompt);
        Console.WriteLine(assessment.Text);

        return ctx with { PassedRestrictedGoods = !flags.Any(), RestrictedFlags = flags };
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Executor 3: Risk Scoring (no LLM — pure logic)
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class RiskScoringExecutor() : Executor("RiskScoring")
{
    [MessageHandler]
    private ValueTask<ComplianceContext> HandleAsync(ComplianceContext ctx, IWorkflowContext wf)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[RiskScoring] Computing score for {ctx.Shipment.ShipmentId}");
        Console.ResetColor();

        int score = Math.Min(ctx.Shipment.RiskScore + ctx.SanctionsFlags.Count * 3
                             + ctx.RestrictedFlags.Count * 2, 10);
        bool needsHuman = score > 6;

        Console.WriteLine($"  Score: {score}/10 — {(needsHuman ? "REQUIRES HUMAN REVIEW" : "Auto-clearance eligible")}");

        return ValueTask.FromResult(ctx with { RiskScore = score, RequiresHumanReview = needsHuman });
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Executor 4: Human Review Gate
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class HumanReviewExecutor(AIAgent agent) : Executor("HumanReview")
{
    [MessageHandler]
    private async ValueTask<ComplianceContext> HandleAsync(ComplianceContext ctx, IWorkflowContext wf)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[HumanReview] HIGH RISK ({ctx.RiskScore}/10) — Officer review required: {ctx.Shipment.ShipmentId}");
        Console.ResetColor();

        var prompt = $"""
            Officer risk briefing for shipment {ctx.Shipment.ShipmentId}:
            - Importer: {ctx.Shipment.ImporterName} | Exporter: {ctx.Shipment.ExporterName}
            - Origin: {ctx.Shipment.CountryOfOrigin} → {ctx.Shipment.DestinationCountry}
            - Risk: {ctx.RiskScore}/10 | Value: ${ctx.Shipment.TotalDeclaredValue:N2}
            - Sanctions flags: {(ctx.SanctionsFlags.Any() ? string.Join("; ", ctx.SanctionsFlags) : "None")}
            - Restricted flags: {(ctx.RestrictedFlags.Any() ? string.Join("; ", ctx.RestrictedFlags) : "None")}
            3-bullet briefing with Hold / Release / Inspect recommendation.
            """;

        var briefing = await agent.RunAsync(prompt);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n[Officer Briefing]\n{briefing.Text}");
        Console.Write("\n[Officer] Approve clearance? (y/n): ");
        Console.ResetColor();

        var approved = Console.ReadLine()?.Trim().ToLower() == "y";
        Console.WriteLine(approved ? "  APPROVED." : "  HELD for inspection.");

        return ctx with { HumanApproved = approved };
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Executor 5: Tariff Calculation
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class TariffCalculationExecutor(MockTariffService tariff) : Executor("TariffCalculation")
{
    [MessageHandler]
    private async ValueTask HandleAsync(ComplianceContext ctx, IWorkflowContext wf)
    {
        if (ctx.RequiresHumanReview && !ctx.HumanApproved)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[TariffCalculation] Shipment held — no duty calculated.");
            Console.ResetColor();
            await wf.YieldOutputAsync(
                $"Shipment {ctx.Shipment.ShipmentId} HELD for inspection.", CancellationToken.None);
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[TariffCalculation] Calculating duties for {ctx.Shipment.ShipmentId}");
        Console.ResetColor();

        decimal totalDuty = 0, totalVat = 0;
        foreach (var line in ctx.Shipment.Lines)
        {
            var duty = tariff.CalculateDuty(line.HsCode, line.TotalValue);
            var vat  = tariff.CalculateVat(line.HsCode, line.TotalValue, duty);
            totalDuty += duty;
            totalVat  += vat;
            Console.WriteLine($"  {line.HsCode}: value=${line.TotalValue:N2}, duty=${duty:N2}, VAT=${vat:N2}");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  Total duty: ${totalDuty:N2} | Total VAT: ${totalVat:N2}");
        Console.ResetColor();

        var status = ctx.RiskScore <= 6 ? "Auto-Cleared" : "Cleared by Officer";
        await wf.YieldOutputAsync(
            $"Shipment {ctx.Shipment.ShipmentId} CLEARED ({status}). " +
            $"Risk: {ctx.RiskScore}/10. Duty: ${totalDuty:N2}. VAT: ${totalVat:N2}.",
            CancellationToken.None);
    }
}
