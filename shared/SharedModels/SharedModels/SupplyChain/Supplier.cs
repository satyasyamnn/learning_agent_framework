namespace SharedModels.SupplyChain;

public record Supplier(
    string SupplierId,
    string Name,
    string Country,
    string Region,
    SupplierStatus Status,
    int ReliabilityScore,       // 0-100
    int LeadTimeDays,
    decimal PriceIndex,         // relative price (1.0 = baseline)
    List<string> ProductCategories,
    int AvailableCapacity       // units per month
);

public enum SupplierStatus
{
    Active,
    Disrupted,
    Inactive,
    UnderReview
}

public record SupplierDisruption(
    string SupplierId,
    string DisruptionType,
    string Description,
    DateTime ReportedAt,
    DisruptionSeverity Severity,
    int EstimatedRecoveryDays
);

public enum DisruptionSeverity
{
    Low,
    Medium,
    High,
    Critical
}
