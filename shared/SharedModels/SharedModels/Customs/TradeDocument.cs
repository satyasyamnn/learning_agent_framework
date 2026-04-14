namespace SharedModels.Customs;

public record TradeDocument(
    string DocumentId,
    DocumentType Type,
    string ShipmentId,
    string Importer,
    string Exporter,
    DateTime IssuedDate,
    List<DocumentField> Fields
);

public record DocumentField(
    string Name,
    string? Value,
    bool IsRequired
);

public enum DocumentType
{
    CommercialInvoice,
    PackingList,
    BillOfLading,
    CertificateOfOrigin,
    ImportLicense,
    CustomsDeclaration
}
