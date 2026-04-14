using SharedModels.SupplyChain;

namespace MockDataServices.SupplyChain;

public class MockSupplierService
{
    private static readonly List<Supplier> Suppliers =
    [
        new("SUP-001", "Apex Electronics", "Taiwan", "Asia-Pacific",
            SupplierStatus.Active, 92, 14, 1.0m,
            ["Electronics", "Semiconductors"], 5000),

        new("SUP-002", "GlobalTech Components", "China", "Asia-Pacific",
            SupplierStatus.Disrupted, 78, 21, 0.85m,
            ["Electronics", "Mechanical Parts"], 8000),

        new("SUP-003", "EuroParts GmbH", "Germany", "Europe",
            SupplierStatus.Active, 88, 10, 1.25m,
            ["Automotive", "Precision Parts"], 3000),

        new("SUP-004", "MexManufacturing SA", "Mexico", "Americas",
            SupplierStatus.Active, 81, 7, 0.95m,
            ["Mechanical Parts", "Assemblies"], 6000),

        new("SUP-005", "IndoFlex Ltd", "India", "Asia-Pacific",
            SupplierStatus.Active, 75, 18, 0.80m,
            ["Textiles", "Packaging"], 10000),
    ];

    private static readonly List<SupplierDisruption> Disruptions =
    [
        new("SUP-002", "Factory Fire",
            "Main production facility partially damaged. Capacity reduced to 20%.",
            DateTime.UtcNow.AddDays(-3), DisruptionSeverity.Critical, 60),
    ];

    public Supplier? GetById(string supplierId) =>
        Suppliers.FirstOrDefault(s => s.SupplierId == supplierId);

    public List<Supplier> GetAll() => Suppliers;

    public List<Supplier> GetActiveAlternatives(string disruptedSupplierId, List<string> requiredCategories)
    {
        var disrupted = Suppliers.FirstOrDefault(s => s.SupplierId == disruptedSupplierId);
        return Suppliers
            .Where(s => s.SupplierId != disruptedSupplierId
                     && s.Status == SupplierStatus.Active
                     && s.ProductCategories.Any(c => requiredCategories.Contains(c)))
            .OrderByDescending(s => s.ReliabilityScore)
            .ToList();
    }

    public List<SupplierDisruption> GetActiveDisruptions() =>
        Disruptions.Where(d => d.ReportedAt >= DateTime.UtcNow.AddDays(-30)).ToList();

    public SupplierDisruption? GetDisruptionForSupplier(string supplierId) =>
        Disruptions.FirstOrDefault(d => d.SupplierId == supplierId);
}
