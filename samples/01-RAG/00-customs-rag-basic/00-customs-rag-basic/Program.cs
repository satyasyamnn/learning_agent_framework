using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

// Load configuration
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var endpointUrl = config["AzureOpenAI:Endpoint"]
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
var deploymentName = config["AzureOpenAI:DeploymentName"]
    ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName not configured");
var apiKey = config["AzureOpenAI:ApiKey"];
var endpoint = new Uri(new Uri(endpointUrl).GetLeftPart(UriPartial.Authority)).ToString();

Console.WriteLine("=== Customs RAG (Basic) with Microsoft Agent Framework ===\n");

var azureOpenAIClient = string.IsNullOrWhiteSpace(apiKey)
    ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

var chatClient = azureOpenAIClient.GetChatClient(deploymentName);

var knowledgeBase = BuildCustomsKnowledgeBase();

[Description("Retrieves the most relevant customs policy and procedure snippets for a user question.")]
string RetrieveCustomsKnowledge(
    [Description("The customs question to retrieve grounded context for.")] string question,
    [Description("How many snippets to retrieve. Use values between 1 and 5.")] int topK = 3)
{
    var retrieved = RetrieveTopDocuments(question, knowledgeBase, Math.Clamp(topK, 1, 5));
    if (retrieved.Count == 0)
    {
        return "No relevant customs context found in the local knowledge base.";
    }

    var sb = new StringBuilder();
    sb.AppendLine("Grounding snippets:");

    foreach (var doc in retrieved)
    {
        sb.AppendLine($"- [{doc.Id}] {doc.Title}: {doc.Content}");
    }

    return sb.ToString();
}

List<AITool> tools = [
    AIFunctionFactory.Create(RetrieveCustomsKnowledge)
];

AIAgent ragAgent = chatClient.AsAIAgent(
    instructions:
        """
        You are a customs clearance copilot.
        Always retrieve customs knowledge before answering policy, tariff, documentation, or compliance questions.
        Ground answers in retrieved snippets and include citation IDs like [KB-001].
        If grounding is missing for a detail, explicitly say it is not available in the current knowledge base.
        Keep answers concise and operational.
        """,
    name: "CustomsRagAgent",
    tools: tools);

var starterQuestions = new[]
{
    "What documents are required for electronics imports into Germany?",
    "How should we handle a shipment that may include dual-use components?",
    "How is customs duty generally calculated for an import declaration?"
};

foreach (var question in starterQuestions)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($">>> {question}");
    Console.ResetColor();

    await foreach (var update in ragAgent.RunStreamingAsync(question))
    {
        Console.Write(update.Text);
    }

    Console.WriteLine("\n");
}

Console.WriteLine("Enter your own customs question (blank line to exit):");
while (true)
{
    Console.Write("customs-rag> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        break;
    }

    await foreach (var update in ragAgent.RunStreamingAsync(input))
    {
        Console.Write(update.Text);
    }

    Console.WriteLine("\n");
}

static List<KnowledgeDocument> BuildCustomsKnowledgeBase()
{
    return
    [
        new(
            "KB-001",
            "Core Commercial Documents",
            "Typical import declaration packets include a commercial invoice, packing list, bill of lading or airway bill, and importer identity details.",
            ["commercial invoice", "packing list", "bill of lading", "airway bill", "import declaration"]),
        new(
            "KB-002",
            "HS Classification Requirement",
            "Every line item should have an HS code. Misclassification can trigger delays, reassessment of duty, and penalties.",
            ["hs code", "classification", "tariff code", "misclassification", "penalty"]),
        new(
            "KB-003",
            "Duty and Tax Basics",
            "Import duty is generally calculated from customs value multiplied by the applicable duty rate. VAT is usually applied after duty is added, based on local rules.",
            ["duty", "vat", "customs value", "duty rate", "tax"]),
        new(
            "KB-004",
            "Sanctions and Denied Party Screening",
            "Shipments should be screened against sanctions and denied party lists before filing. Positive matches require escalation and legal review.",
            ["sanctions", "denied party", "screening", "escalation", "legal review"]),
        new(
            "KB-005",
            "Dual-Use Goods Handling",
            "Dual-use items may require export or import licenses depending on origin, destination, end-user, and end-use. Missing license evidence is a high-risk compliance gap.",
            ["dual-use", "license", "end-user", "end-use", "compliance risk"]),
        new(
            "KB-006",
            "Country of Origin and Preference",
            "Country of origin declarations and proof of preference can affect duty eligibility under trade agreements.",
            ["country of origin", "preference", "trade agreement", "certificate of origin"]),
        new(
            "KB-007",
            "Pre-Clearance Validation",
            "Before submission, validate consistency across invoice values, quantities, weights, and shipment references to reduce customs holds.",
            ["pre-clearance", "validation", "consistency", "customs hold", "submission"])
    ];
}

static List<KnowledgeDocument> RetrieveTopDocuments(string question, IReadOnlyCollection<KnowledgeDocument> documents, int topK)
{
    var queryTokens = Tokenize(question);

    return documents
        .Select(doc => new
        {
            Document = doc,
            Score = Score(doc, queryTokens)
        })
        .Where(x => x.Score > 0)
        .OrderByDescending(x => x.Score)
        .ThenBy(x => x.Document.Id)
        .Take(topK)
        .Select(x => x.Document)
        .ToList();
}

static int Score(KnowledgeDocument document, HashSet<string> queryTokens)
{
    if (queryTokens.Count == 0)
    {
        return 0;
    }

    var contentTokens = Tokenize($"{document.Title} {document.Content} {string.Join(' ', document.Keywords)}");
    var overlap = queryTokens.Count(token => contentTokens.Contains(token));

    var keywordBoost = document.Keywords.Count(keyword => queryTokens.Contains(keyword.ToLowerInvariant()));

    return (overlap * 3) + (keywordBoost * 5);
}

static HashSet<string> Tokenize(string text)
{
    return Regex.Matches(text.ToLowerInvariant(), "[a-z0-9-]+")
        .Select(m => m.Value)
        .Where(token => token.Length > 2)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}

internal sealed record KnowledgeDocument(string Id, string Title, string Content, string[] Keywords);
