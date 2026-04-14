namespace SharedModels.Customs;

public record CustomsShipment(
    string ShipmentId,
    string ImporterName,
    string ImporterEori,
    string ExporterName,
    string CountryOfOrigin,
    string DestinationCountry,
    string PortOfEntry,
    List<CustomsLine> Lines,
    decimal TotalDeclaredValue,
    string CurrencyCode,
    CustomsClearanceStatus ClearanceStatus,
    int RiskScore                // 0-10
);

public record CustomsLine(
    string LineId,
    string Description,
    string HsCode,
    int Quantity,
    string UnitOfMeasure,
    decimal UnitValue,
    decimal TotalValue,
    string CountryOfOrigin,
    bool IsDualUse,
    bool IsRestrictedGood
);

public enum CustomsClearanceStatus
{
    Pending,
    UnderReview,
    HoldForInspection,
    ApprovedForClearance,
    Cleared,
    Rejected
}
