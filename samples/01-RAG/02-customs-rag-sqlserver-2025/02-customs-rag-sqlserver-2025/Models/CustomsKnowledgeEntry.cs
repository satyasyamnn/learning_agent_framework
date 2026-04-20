internal sealed record CustomsKnowledgeEntry(
    string Reference,
    string Title,
    string Content,
    string Region,
    string RiskLevel)
{
    public string GetTitleAndDetails() =>
        $"[{Reference}] {Title} (Region: {Region}, Risk: {RiskLevel}) - {Content}";

    public string GetEmbeddingInput() =>
        $"[{Reference}] {Title} | {Content} | Region={Region} | Risk={RiskLevel}";
}
