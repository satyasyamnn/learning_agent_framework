internal static class CustomsKnowledgeBaseData
{
    public static List<CustomsKnowledgeEntry> Build()
    {
        return
        [
            new("CUS-001", "Core Import Document Set", "Commercial invoice, packing list, transport document, and importer identification are baseline documents for filing.", "EU", "Medium"),
            new("CUS-002", "HS Classification Governance", "Each declared item requires an HS code with documented rationale. Classification gaps can trigger holds and duty reassessments.", "Global", "High"),
            new("CUS-003", "Customs Value and Duty", "Duty is computed from customs value and tariff rate. VAT may be applied over the customs value plus duty depending on jurisdiction.", "Global", "Medium"),
            new("CUS-004", "Sanctions and Denied Parties", "Run sanctions and denied-party screening before submission. Any match requires escalation and legal approval.", "Global", "High"),
            new("CUS-005", "Dual-Use Licensing", "Dual-use goods may require license evidence tied to end-user and end-use. Missing evidence is a critical compliance blocker.", "EU", "High"),
            new("CUS-006", "Origin and Preference", "Country of origin declarations and preference proofs can reduce duty under trade agreements when eligibility criteria are met.", "EU", "Medium"),
            new("CUS-007", "Pre-Filing Data Consistency", "Invoice values, quantities, gross weight, and shipment references must align across documents to reduce inspection risk.", "Global", "Low"),
            new("CUS-008", "EORI Requirement", "For EU customs procedures, declarants generally require a valid EORI number before lodging declarations.", "EU", "High")
        ];
    }
}
