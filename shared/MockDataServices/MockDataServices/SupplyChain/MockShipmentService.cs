using SharedModels.SupplyChain;

namespace MockDataServices.SupplyChain;

public class MockShipmentService
{
    private static readonly List<Shipment> Shipments =
    [
        new("TRK-001-2025", "ORD-1001", "Singapore", "Rotterdam", "MAERSK",
            ShipmentStatus.InTransit,
            DateTime.UtcNow.AddDays(5), null,
            [
                new(DateTime.UtcNow.AddDays(-10), "Singapore Port", "Departed origin port"),
                new(DateTime.UtcNow.AddDays(-3),  "Suez Canal",     "Transited Suez Canal"),
            ]),

        new("TRK-002-2025", "ORD-1002", "Shanghai", "Los Angeles", "MSC",
            ShipmentStatus.Delayed,
            DateTime.UtcNow.AddDays(12), null,
            [
                new(DateTime.UtcNow.AddDays(-8), "Shanghai Port",   "Departed origin port"),
                new(DateTime.UtcNow.AddDays(-2), "Pacific Ocean",   "Delay: severe weather conditions"),
            ]),

        new("TRK-003-2025", "ORD-1003", "Hamburg", "New York", "CMA CGM",
            ShipmentStatus.Delivered,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1),
            [
                new(DateTime.UtcNow.AddDays(-15), "Hamburg Port", "Departed origin port"),
                new(DateTime.UtcNow.AddDays(-1),  "New York",     "Delivered to consignee"),
            ]),

        new("TRK-004-2025", "ORD-1004", "Shenzhen", "Felixstowe", "COSCO",
            ShipmentStatus.Pending,
            DateTime.UtcNow.AddDays(20), null,
            []),

        new("TRK-005-2025", "ORD-1005", "Mumbai", "Dubai", "DP World",
            ShipmentStatus.InTransit,
            DateTime.UtcNow.AddDays(3), null,
            [
                new(DateTime.UtcNow.AddDays(-5), "Mumbai Port", "Departed origin port"),
                new(DateTime.UtcNow.AddDays(-1), "Arabian Sea", "On schedule"),
            ]),
    ];

    public Shipment? GetByTrackingNumber(string trackingNumber) =>
        Shipments.FirstOrDefault(s => s.TrackingNumber.Equals(trackingNumber, StringComparison.OrdinalIgnoreCase));

    public List<Shipment> GetDelayedShipments() =>
        Shipments.Where(s => s.Status == ShipmentStatus.Delayed).ToList();

    public List<Shipment> GetShipmentsByStatus(ShipmentStatus status) =>
        Shipments.Where(s => s.Status == status).ToList();

    public void UpdateStatus(string trackingNumber, ShipmentStatus newStatus)
    {
        var idx = Shipments.FindIndex(s => s.TrackingNumber == trackingNumber);
        if (idx >= 0)
            Shipments[idx] = Shipments[idx] with { Status = newStatus };
    }
}
