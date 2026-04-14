using SharedModels.Customs;

namespace MockDataServices.Customs;

public class MockDocumentService
{
    private static readonly List<TradeDocument> Documents =
    [
        new("DOC-CI-3001", DocumentType.CommercialInvoice, "CSH-3001",
            "Acme Corp UK", "Shanghai Exports Ltd", DateTime.UtcNow.AddDays(-2),
            [
                new("InvoiceNumber",      "INV-2025-3001",    true),
                new("InvoiceDate",        DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd"), true),
                new("BuyerName",          "Acme Corp UK",     true),
                new("SellerName",         "Shanghai Exports Ltd", true),
                new("TotalValue",         "7850.00 USD",      true),
                new("Currency",           "USD",              true),
                new("PaymentTerms",       "NET 30",           false),
                new("CountryOfOrigin",    "CN",               true),
                new("HsCodes",            "8542.31, 8507.60", true),
            ]),

        new("DOC-PL-3001", DocumentType.PackingList, "CSH-3001",
            "Acme Corp UK", "Shanghai Exports Ltd", DateTime.UtcNow.AddDays(-2),
            [
                new("PackingListNumber",  "PL-2025-3001",     true),
                new("TotalPackages",      "10",               true),
                new("GrossWeight",        "25.5 kg",          true),
                new("NetWeight",          "22.0 kg",          true),
                new("Dimensions",         null,               false),  // MISSING optional
            ]),

        new("DOC-BL-3001", DocumentType.BillOfLading, "CSH-3001",
            "Acme Corp UK", "Shanghai Exports Ltd", DateTime.UtcNow.AddDays(-3),
            [
                new("BLNumber",           "MAEU-BL-9988221",  true),
                new("Shipper",            "Shanghai Exports Ltd", true),
                new("Consignee",          "Acme Corp UK",     true),
                new("PortOfLoading",      "Shanghai",         true),
                new("PortOfDischarge",    "Felixstowe",       true),
                new("VesselName",         "Maersk Emerald",   true),
                new("NotifyParty",        null,               true),  // MISSING required
            ]),

        // CSH-3002 - missing commercial invoice (only has packing list)
        new("DOC-PL-3002", DocumentType.PackingList, "CSH-3002",
            "TechImports GmbH", "Dual-Use Systems Inc", DateTime.UtcNow.AddDays(-1),
            [
                new("PackingListNumber",  "PL-2025-3002",     true),
                new("TotalPackages",      "2",                true),
                new("GrossWeight",        "5.0 kg",           true),
                new("NetWeight",          "4.2 kg",           true),
            ]),
    ];

    public List<TradeDocument> GetByShipment(string shipmentId) =>
        Documents.Where(d => d.ShipmentId == shipmentId).ToList();

    public TradeDocument? GetById(string documentId) =>
        Documents.FirstOrDefault(d => d.DocumentId == documentId);
}
