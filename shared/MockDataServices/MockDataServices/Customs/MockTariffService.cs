namespace MockDataServices.Customs;

/// <summary>
/// Simulates a tariff/duty lookup service (simplified).
/// Real-world equivalent: HMRC Trade Tariff, EU TARIC, or CBP ACE.
/// </summary>
public class MockTariffService
{
    private static readonly Dictionary<string, TariffEntry> TariffTable = new()
    {
        ["8542.31"] = new("8542.31", "Processors and controllers", 0.0m,  20.0m, false),
        ["8507.60"] = new("8507.60", "Lithium batteries",          5.0m,  20.0m, false),
        ["9014.20"] = new("9014.20", "Navigational instruments",   3.5m,  20.0m, true),
        ["5208.21"] = new("5208.21", "Woven cotton fabrics",       12.0m, 20.0m, false),
        ["5407.61"] = new("5407.61", "Woven synthetic fabrics",    6.5m,  20.0m, false),
        ["8542.39"] = new("8542.39", "Electronic integrated circuits", 0.0m, 20.0m, true),
        ["7616.99"] = new("7616.99", "Aluminium articles",         4.0m,  20.0m, false),
        ["8532.10"] = new("8532.10", "Fixed capacitors",           0.0m,  20.0m, false),
        ["8482.10"] = new("8482.10", "Ball bearings",              3.0m,  20.0m, false),
    };

    private static readonly HashSet<string> SanctionedCountries = ["IR", "KP", "SY", "CU"];
    private static readonly HashSet<string> RestrictedHsCodes   = ["9014.20", "8542.39", "8537.10"];

    public TariffEntry? Lookup(string hsCode) =>
        TariffTable.TryGetValue(hsCode, out var entry) ? entry : null;

    public bool IsCountrySanctioned(string countryCode) =>
        SanctionedCountries.Contains(countryCode.ToUpperInvariant());

    public bool IsHsCodeRestricted(string hsCode) =>
        RestrictedHsCodes.Contains(hsCode);

    public decimal CalculateDuty(string hsCode, decimal value) =>
        TariffTable.TryGetValue(hsCode, out var e) ? Math.Round(value * e.DutyRatePercent / 100, 2) : 0m;

    public decimal CalculateVat(string hsCode, decimal value, decimal duty) =>
        TariffTable.TryGetValue(hsCode, out var e) ? Math.Round((value + duty) * e.VatRatePercent / 100, 2) : 0m;
}

public record TariffEntry(
    string HsCode,
    string Description,
    decimal DutyRatePercent,
    decimal VatRatePercent,
    bool RequiresLicense
);
