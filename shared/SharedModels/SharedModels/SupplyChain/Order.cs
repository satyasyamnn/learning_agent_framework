namespace SharedModels.SupplyChain;

public record Order(
    string OrderId,
    string CustomerId,
    string SupplierId,
    string TrackingNumber,
    DateTime OrderDate,
    DateTime RequiredByDate,
    OrderStatus Status,
    List<OrderLine> Lines,
    decimal TotalValue
);

public record OrderLine(
    string ProductId,
    string ProductName,
    string HsCode,
    int Quantity,
    decimal UnitPrice,
    string CountryOfOrigin
);

public enum OrderStatus
{
    New,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    OnHold
}
