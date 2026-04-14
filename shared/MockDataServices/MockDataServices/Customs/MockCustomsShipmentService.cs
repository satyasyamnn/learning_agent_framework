using SharedModels.Customs;

namespace MockDataServices.Customs;

public class MockCustomsShipmentService
{
    private static readonly List<CustomsShipment> Shipments =
    [
        new("CSH-3001", "Acme Corp UK", "GB123456789", "Shanghai Exports Ltd", "CN", "GB",
            "Felixstowe", new List<CustomsLine>
            {
                new("L1", "Microcontrollers", "8542.31", 500, "units", 12.50m, 6250m, "CN", false, false),
                new("L2", "Lithium batteries", "8507.60", 200, "units", 8.00m,  1600m, "CN", false, false),
            },
            7850m, "USD", CustomsClearanceStatus.Pending, 3),

        new("CSH-3002", "TechImports GmbH", "DE987654321", "Dual-Use Systems Inc", "US", "DE",
            "Hamburg", new List<CustomsLine>
            {
                new("L1", "High-precision gyroscopes", "9014.20", 10, "units", 4500m, 45000m, "US", true, false),
            },
            45000m, "USD", CustomsClearanceStatus.UnderReview, 8),

        new("CSH-3003", "Fashion House BV", "NL112233445", "Textile Mills Pvt", "IN", "NL",
            "Rotterdam", new List<CustomsLine>
            {
                new("L1", "Cotton fabric rolls", "5208.21", 1000, "kg", 3.50m, 3500m, "IN", false, false),
                new("L2", "Synthetic fibre", "5407.61", 500, "kg", 5.00m, 2500m, "IN", false, false),
            },
            6000m, "USD", CustomsClearanceStatus.Pending, 1),

        new("CSH-3004", "SanctionedCorp Trading", "XX000000001", "Restricted Goods Co", "IR", "GB",
            "Heathrow Air Cargo", new List<CustomsLine>
            {
                new("L1", "Electronic components", "8542.39", 100, "units", 150m, 15000m, "IR", false, true),
            },
            15000m, "USD", CustomsClearanceStatus.Pending, 10),
    ];

    public CustomsShipment? GetById(string shipmentId) =>
        Shipments.FirstOrDefault(s => s.ShipmentId == shipmentId);

    public List<CustomsShipment> GetAll() => Shipments;

    public List<CustomsShipment> GetPending() =>
        Shipments.Where(s => s.ClearanceStatus == CustomsClearanceStatus.Pending).ToList();

    public void UpdateStatus(string shipmentId, CustomsClearanceStatus status)
    {
        var idx = Shipments.FindIndex(s => s.ShipmentId == shipmentId);
        if (idx >= 0)
            Shipments[idx] = Shipments[idx] with { ClearanceStatus = status };
    }
}
