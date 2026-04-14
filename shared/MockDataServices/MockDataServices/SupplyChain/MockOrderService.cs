using SharedModels.SupplyChain;

namespace MockDataServices.SupplyChain;

public class MockOrderService
{
    private static readonly List<Order> Orders =
    [
        new("ORD-1001", "CUST-A", "SUP-001", "TRK-001-2025",
            DateTime.UtcNow.AddDays(-12), DateTime.UtcNow.AddDays(7),
            OrderStatus.Shipped,
            [new("PROD-101", "Microcontroller Unit", "8542.31", 500, 12.50m, "Taiwan")],
            6250m),

        new("ORD-1002", "CUST-B", "SUP-002", "TRK-002-2025",
            DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(5),
            OrderStatus.Shipped,
            [new("PROD-202", "Capacitor Array", "8532.10", 2000, 0.85m, "China")],
            1700m),

        new("ORD-1003", "CUST-C", "SUP-003", "TRK-003-2025",
            DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddDays(-1),
            OrderStatus.Delivered,
            [new("PROD-303", "Precision Bearing", "8482.10", 100, 45.00m, "Germany")],
            4500m),

        new("ORD-1004", "CUST-A", "SUP-004", "",
            DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(25),
            OrderStatus.Processing,
            [new("PROD-404", "Aluminium Housing", "7616.99", 300, 22.00m, "Mexico")],
            6600m),
    ];

    public Order? GetById(string orderId) =>
        Orders.FirstOrDefault(o => o.OrderId == orderId);

    public List<Order> GetBySupplier(string supplierId) =>
        Orders.Where(o => o.SupplierId == supplierId).ToList();

    public List<Order> GetAll() => Orders;
}
