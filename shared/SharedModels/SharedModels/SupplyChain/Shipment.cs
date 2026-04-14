namespace SharedModels.SupplyChain;

public record Shipment(
    string TrackingNumber,
    string OrderId,
    string Origin,
    string Destination,
    string CarrierId,
    ShipmentStatus Status,
    DateTime EstimatedDelivery,
    DateTime? ActualDelivery,
    List<ShipmentEvent> Events
);

public record ShipmentEvent(
    DateTime Timestamp,
    string Location,
    string Description
);

public enum ShipmentStatus
{
    Pending,
    InTransit,
    OutForDelivery,
    Delivered,
    Delayed,
    Lost
}
