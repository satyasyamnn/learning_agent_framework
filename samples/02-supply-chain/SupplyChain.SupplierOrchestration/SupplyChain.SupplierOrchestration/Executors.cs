using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using MockDataServices.SupplyChain;
using SharedModels.SupplyChain;

// ─────────────────────────────────────────────────────────────────────────────
// Executor 1: Supplier Evaluator
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class SupplierEvaluatorExecutor(AIAgent agent, MockSupplierService supplierService)
    : Executor("SupplierEvaluator")
{
    [MessageHandler]
    private async ValueTask<EvaluationResult> HandleAsync(DisruptionEvent evt, IWorkflowContext ctx)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[SupplierEvaluator] Evaluating alternatives for: {evt.SupplierId}");
        Console.ResetColor();

        var disruption   = supplierService.GetDisruptionForSupplier(evt.SupplierId);
        var alternatives = supplierService.GetActiveAlternatives(evt.SupplierId, evt.RequiredCategories);

        var prompt = $"""
            Supplier {evt.SupplierId} ({evt.SupplierName}) has experienced: {disruption?.Description ?? "a disruption"}.
            Severity: {disruption?.Severity}. Estimated recovery: {disruption?.EstimatedRecoveryDays} days.
            Open order value at risk: ${evt.TotalOrderValue:N2}. Delivery deadline in {evt.DaysUntilDeadline} days.

            Available alternatives:
            {string.Join("\n", alternatives.Select(s =>
                $"  - {s.SupplierId} ({s.Name}, {s.Country}): reliability={s.ReliabilityScore}/100, " +
                $"leadTime={s.LeadTimeDays}d, priceIndex={s.PriceIndex}x, capacity={s.AvailableCapacity} units/month"))}

            Rank these alternatives and justify your top recommendation. Be concise.
            """;

        var response = await agent.RunAsync(prompt);
        Console.WriteLine(response.Text);

        var top = alternatives.OrderByDescending(s => s.ReliabilityScore).First();

        return new EvaluationResult(
            evt.SupplierId, top.SupplierId, top.Name, top.Country,
            top.ReliabilityScore, top.LeadTimeDays, evt.TotalOrderValue,
            response.Text
        );
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Executor 2: Negotiation Agent with human-in-the-loop approval gate
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class NegotiationExecutor(AIAgent agent) : Executor("NegotiationAgent")
{
    [MessageHandler]
    private async ValueTask<NegotiationResult> HandleAsync(EvaluationResult eval, IWorkflowContext ctx)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[NegotiationAgent] Preparing to switch to {eval.RecommendedSupplierName}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n  CONTRACT APPROVAL REQUIRED");
        Console.WriteLine($"  Supplier:       {eval.RecommendedSupplierId} — {eval.RecommendedSupplierName}");
        Console.WriteLine($"  Contract value: ${eval.TotalOrderValue:N2}");
        Console.Write("  Approve? (y/n): ");
        Console.ResetColor();

        var approved = Console.ReadLine()?.Trim().ToLower() == "y";

        if (!approved)
        {
            Console.WriteLine("  [Gate] DENIED — escalated to procurement team.");
            return new NegotiationResult(eval.DisruptedSupplierId, eval.RecommendedSupplierId,
                false, 0, 0, 0, "Rejected at human approval gate.");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  [Gate] APPROVED — proceeding with negotiation.");
        Console.ResetColor();

        var prompt = $"""
            Draft emergency supply terms with {eval.RecommendedSupplierName} ({eval.RecommendedCountry}).
            Order value: ${eval.TotalOrderValue:N2}. Their lead time: {eval.LeadTimeDays} days.
            Propose: price premium %, one-time expedite fee, delivery guarantee, and late-penalty clause.
            Format as a brief term sheet (4 bullet points).
            """;

        var summary = await agent.RunAsync(prompt);
        Console.WriteLine(summary.Text);

        return new NegotiationResult(
            eval.DisruptedSupplierId, eval.RecommendedSupplierId,
            true, 8.5m, 2500m, 500m, summary.Text
        );
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Executor 3: ERP Update Agent
// ─────────────────────────────────────────────────────────────────────────────
internal sealed partial class ErpUpdateExecutor(AIAgent agent, MockOrderService orderService)
    : Executor("ERPUpdateAgent")
{
    [MessageHandler]
    private async ValueTask HandleAsync(NegotiationResult negotiation, IWorkflowContext ctx)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n[ERPUpdateAgent] Updating records and notifying stakeholders");
        Console.ResetColor();

        if (!negotiation.Approved)
        {
            await ctx.YieldOutputAsync("Workflow completed without ERP changes — switch not approved.", CancellationToken.None);
            return;
        }

        var affected = orderService.GetBySupplier(negotiation.DisruptedSupplierId)
            .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
            .ToList();

        var prompt = $"""
            Write a 3-paragraph stakeholder notification email:
            - {affected.Count} open orders re-routed from {negotiation.DisruptedSupplierId} to {negotiation.NewSupplierId}
            - Terms: +{negotiation.PricePremiumPercent}% price premium, ${negotiation.ExpediteFee} expedite fee,
              ${negotiation.PenaltyPerDayLate}/day late penalty
            - Context: {negotiation.NegotiationSummary}
            Address it to: Procurement, Finance, Operations.
            """;

        var notification = await agent.RunAsync(prompt);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n[Stakeholder Notification]");
        Console.ResetColor();
        Console.WriteLine(notification.Text);

        await ctx.YieldOutputAsync(
            $"Supplier switch complete: {affected.Count} orders re-routed to {negotiation.NewSupplierId}. " +
            $"Price impact: +{negotiation.PricePremiumPercent}%. ERP updated. Stakeholders notified.",
            CancellationToken.None);
    }
}
