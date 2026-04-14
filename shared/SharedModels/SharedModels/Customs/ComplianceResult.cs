namespace SharedModels.Customs;

public record ComplianceResult(
    string ShipmentId,
    bool PassedSanctionsCheck,
    bool PassedRestrictedGoodsCheck,
    int RiskScore,
    List<ComplianceFlag> Flags,
    decimal EstimatedDuty,
    decimal EstimatedVat,
    string Recommendation
);

public record ComplianceFlag(
    string Code,
    string Description,
    FlagSeverity Severity
);

public enum FlagSeverity
{
    Info,
    Warning,
    Critical
}
