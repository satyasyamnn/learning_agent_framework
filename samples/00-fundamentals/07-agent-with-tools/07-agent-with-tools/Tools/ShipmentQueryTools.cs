using System.ComponentModel;

/// <summary>
/// Customs query tools whose public declared methods are auto-discovered via
/// reflection in Program.cs and registered as agent tools.
/// Adding a new public method here makes it available to the agent automatically.
/// </summary>
public class ShipmentQueryTools
{
    // ── Mock shipment registry ────────────────────────────────────────────────
    private static readonly Dictionary<string, ShipmentSummary> Shipments = new()
    {
        ["CSH-3001"] = new("CSH-3001", "Acme Corp UK",           "CN", 7850m,  "USD", "Pending",     ["8542.31", "8507.60"]),
        ["CSH-3002"] = new("CSH-3002", "TechImports GmbH",       "US", 45000m, "USD", "UnderReview", ["9014.20"]),
        ["CSH-3003"] = new("CSH-3003", "Fashion House BV",       "IN", 6000m,  "USD", "Pending",     ["5208.21", "5407.61"]),
        ["CSH-3004"] = new("CSH-3004", "SanctionedCorp Trading", "IR", 15000m, "USD", "Pending",     ["8542.39"]),
    };

    // ── Mock tariff table ─────────────────────────────────────────────────────
    private static readonly Dictionary<string, TariffInfo> TariffTable = new()
    {
        ["8542.31"] = new("Processors and controllers",     0.0m,  20.0m, false),
        ["8507.60"] = new("Lithium batteries",              5.0m,  20.0m, false),
        ["9014.20"] = new("Navigational instruments",       3.5m,  20.0m, true),
        ["5208.21"] = new("Woven cotton fabrics",           12.0m, 20.0m, false),
        ["5407.61"] = new("Woven synthetic fabrics",        6.5m,  20.0m, false),
        ["8542.39"] = new("Electronic integrated circuits", 0.0m,  20.0m, true),
        ["8482.10"] = new("Ball bearings",                  3.0m,  20.0m, false),
    };

    private static readonly HashSet<string> SanctionedCountries = ["IR", "KP", "SY", "CU"];

    // ─────────────────────────────────────────────────────────────────────────
    // Each method below is automatically registered as an agent tool via
    // reflection in Program.cs.  Add [Description] to help the LLM understand
    // when and how to call each tool.
    // ─────────────────────────────────────────────────────────────────────────

    [Description("Get the customs clearance status and summary for a shipment.")]
    public string GetShipmentStatus([Description("Shipment ID, e.g. CSH-3001")] string shipmentId)
    {
        if (!Shipments.TryGetValue(shipmentId.ToUpperInvariant(), out var s))
            return $"Shipment '{shipmentId}' not found.";

        return $"Shipment {s.Id}: Importer={s.Importer}, Origin={s.Origin}, " +
               $"Value={s.Value} {s.Currency}, Status={s.Status}, " +
               $"HS Codes=[{string.Join(", ", s.HsCodes)}]";
    }

    [Description("Look up the tariff entry for a commodity HS code — includes duty rate, VAT rate, and import-licence requirement.")]
    public string LookupTariffEntry([Description("6-digit HS commodity code, e.g. 8542.31")] string hsCode)
    {
        if (!TariffTable.TryGetValue(hsCode, out var t))
            return $"No tariff entry found for HS code '{hsCode}'.";

        return $"HS {hsCode} — {t.Description}: Duty={t.DutyRatePercent}%, " +
               $"VAT={t.VatRatePercent}%, ImportLicenceRequired={t.RequiresLicense}";
    }

    [Description("Check whether a country is on the customs sanctions list and subject to import restrictions.")]
    public string IsCountrySanctioned([Description("ISO-2 country code, e.g. IR, CN, US")] string countryCode)
    {
        bool sanctioned = SanctionedCountries.Contains(countryCode.ToUpperInvariant());
        return sanctioned
            ? $"Country '{countryCode}' IS on the sanctions list — all shipments require a mandatory hold."
            : $"Country '{countryCode}' is not on the sanctions list.";
    }

    [Description("List all shipments currently awaiting customs clearance (status: Pending or UnderReview).")]
    public string GetPendingShipments()
    {
        var rows = Shipments.Values
            .Where(s => s.Status is "Pending" or "UnderReview")
            .Select(s => $"  {s.Id}: {s.Importer} | Origin: {s.Origin} | Status: {s.Status}");

        return "Open shipments:\n" + string.Join("\n", rows);
    }

    [Description("Calculate the estimated customs duty and VAT for a shipment line given its HS code and declared value in USD.")]
    public string CalculateDutyAmount(
        [Description("6-digit HS commodity code")] string hsCode,
        [Description("Declared line value in USD")] decimal declaredValueUsd)
    {
        if (!TariffTable.TryGetValue(hsCode, out var t))
            return $"Cannot calculate — HS code '{hsCode}' not found in tariff table.";

        var duty = Math.Round(declaredValueUsd * t.DutyRatePercent / 100, 2);
        var vat  = Math.Round((declaredValueUsd + duty) * t.VatRatePercent / 100, 2);

        return $"HS {hsCode} on USD {declaredValueUsd:N2}: " +
               $"Duty = USD {duty:N2} ({t.DutyRatePercent}%), " +
               $"VAT = USD {vat:N2} ({t.VatRatePercent}%), " +
               $"Total Charges = USD {duty + vat:N2}";
    }
}

// ── Supporting record types (private to this file) ────────────────────────────
internal record ShipmentSummary(string Id, string Importer, string Origin, decimal Value, string Currency, string Status, string[] HsCodes);

internal record TariffInfo(string Description, decimal DutyRatePercent, decimal VatRatePercent, bool RequiresLicense);
