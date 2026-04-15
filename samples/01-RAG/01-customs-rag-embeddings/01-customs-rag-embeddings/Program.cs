using System.ComponentModel;
using System.Text;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

// Load shared config so chat + embedding models can be changed without recompiling.
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var endpointUrl = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
var chatDeployment = config["AzureOpenAI:DeploymentName"] ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName not configured");
var apiKey = config["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey not configured");

var embeddingEndpointUrl = config["AzureOpenAI:EmbeddingEndpoint"];
var embeddingDeployment = config["AzureOpenAI:EmbeddingDeploymentName"] ?? throw new InvalidOperationException("AzureOpenAI:EmbeddingDeploymentName not configured");
var embeddingApiKey = config["AzureOpenAI:EmbeddingApiKey"];

var chatEndpoint = NormalizeAzureOpenAiEndpoint(endpointUrl);
var embeddingEndpoint = NormalizeAzureOpenAiEndpoint(string.IsNullOrWhiteSpace(embeddingEndpointUrl) ? endpointUrl : embeddingEndpointUrl);

AzureOpenAIClient client = new(new Uri(chatEndpoint), new AzureKeyCredential(apiKey));
var embeddingClient = new AzureOpenAIClient(new Uri(embeddingEndpoint), new AzureKeyCredential(string.IsNullOrWhiteSpace(embeddingApiKey) ? apiKey : embeddingApiKey));

var customsData = BuildCustomsKnowledgeBase();

Console.WriteLine("=== Customs RAG With Embeddings (Agent Framework) ===");
Console.WriteLine($"Chat model: {chatDeployment}");
Console.WriteLine($"Embedding model: {embeddingDeployment}");
Console.WriteLine();

var question = "What are the top compliance checks before filing an import declaration for electronics in the EU?";

ChatClientAgent baselineAgent = client
    .GetChatClient(chatDeployment)
    .AsIChatClient()
    .AsAIAgent(instructions:
        """
        You are a customs compliance specialist.
        Use only supplied customs snippets and do not invent regulations.
        Keep responses practical and concise.
        """);

#region Sample 1 - Preload all data
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Sample 1 - Preload all customs data");
Console.ResetColor();

var preloadEverythingMessages = new List<Microsoft.Extensions.AI.ChatMessage>
{
    new(Microsoft.Extensions.AI.ChatRole.Assistant, "Here are all customs knowledge snippets")
};

foreach (var entry in customsData)
{
    preloadEverythingMessages.Add(new Microsoft.Extensions.AI.ChatMessage(
        Microsoft.Extensions.AI.ChatRole.Assistant,
        entry.GetTitleAndDetails()));
}

preloadEverythingMessages.Add(new Microsoft.Extensions.AI.ChatMessage(
    Microsoft.Extensions.AI.ChatRole.User,
    question));

var baselineResponse = await baselineAgent.RunAsync(preloadEverythingMessages);
Console.WriteLine(baselineResponse);
Console.WriteLine();
#endregion

#region Sample 2 - Preload only top semantic matches
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Sample 2 - Embedding-based retrieval before answering");
Console.ResetColor();

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = embeddingClient
    .GetEmbeddingClient(embeddingDeployment)
    .AsIEmbeddingGenerator();

var vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions
{
    EmbeddingGenerator = embeddingGenerator
});

var collection = vectorStore.GetCollection<Guid, CustomsVectorStoreRecord>("customs-knowledge");
await collection.EnsureCollectionExistsAsync();

var count = 0;
foreach (var entry in customsData)
{
    count++;
    Console.Write($"\rEmbedding customs records: {count}/{customsData.Count}");

    await collection.UpsertAsync(new CustomsVectorStoreRecord
    {
        Id = Guid.NewGuid(),
        Reference = entry.Reference,
        Title = entry.Title,
        Content = entry.Content,
        Region = entry.Region,
        RiskLevel = entry.RiskLevel
    });
}

Console.WriteLine();

var ragPreloadMessages = new List<Microsoft.Extensions.AI.ChatMessage>
{
    new(Microsoft.Extensions.AI.ChatRole.Assistant, "Here are the most relevant customs snippets")
};

await foreach (var match in collection.SearchAsync(
                   question,
                   top: 5,
                   options: new VectorSearchOptions<CustomsVectorStoreRecord> { IncludeVectors = false }))
{
    ragPreloadMessages.Add(new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, match.Record.GetTitleAndDetails()));
}

ragPreloadMessages.Add(new Microsoft.Extensions.AI.ChatMessage( Microsoft.Extensions.AI.ChatRole.User, question));

var ragResponse = await baselineAgent.RunAsync(ragPreloadMessages);
Console.WriteLine(ragResponse);
Console.WriteLine();
#endregion

#region Sample 3 - Tool-based semantic retrieval
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Sample 3 - Agent retrieves customs snippets via tool call");
Console.ResetColor();

var searchTool = new CustomsSearchTool(collection);
AIAgent toolAgent = client
    .GetChatClient(chatDeployment)
    .AsIChatClient()
    .AsAIAgent(
        instructions:
        """
        You are a customs compliance specialist.
        Always call the retrieval tool for customs/tariff/document/compliance questions.
        Cite references in square brackets, for example [CUS-001].
        If a detail is not present in retrieved snippets, explicitly say it is unavailable.
        """,
        tools: [AIFunctionFactory.Create(searchTool.SearchVectorStore)])
    .AsBuilder()
    .Use(LogFunctionCalls)
    .Build();

var toolResponse = await toolAgent.RunAsync(question);
Console.WriteLine(toolResponse);

Console.WriteLine();
Console.WriteLine("Ask customs questions (blank line to exit):");
while (true)
{
    Console.Write("customs-embedding-rag> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        break;
    }

    await foreach (var update in toolAgent.RunStreamingAsync(input))
    {
        Console.Write(update.Text);
    }

    Console.WriteLine();
    Console.WriteLine();
}
#endregion

async ValueTask<object?> LogFunctionCalls(
    AIAgent callingAgent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken)
{
    var details = new StringBuilder();
    details.Append($"- Tool Call: '{context.Function.Name}'");

    if (context.Arguments.Count > 0)
    {
        details.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))})");
    }

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine(details.ToString());
    Console.ResetColor();

    return await next(context, cancellationToken);
}

static List<CustomsKnowledgeEntry> BuildCustomsKnowledgeBase()
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

static string NormalizeAzureOpenAiEndpoint(string endpoint)
{
    return new Uri(endpoint).GetLeftPart(UriPartial.Authority);
}

internal sealed record CustomsKnowledgeEntry(
    string Reference,
    string Title,
    string Content,
    string Region,
    string RiskLevel)
{
    public string GetTitleAndDetails() =>
        $"[{Reference}] {Title} (Region: {Region}, Risk: {RiskLevel}) - {Content}";
}

internal sealed class CustomsVectorStoreRecord
{
    [VectorStoreKey]
    public required Guid Id { get; set; }

    [VectorStoreData]
    public required string Reference { get; set; }

    [VectorStoreData]
    public required string Title { get; set; }

    [VectorStoreData]
    public required string Content { get; set; }

    [VectorStoreData]
    public required string Region { get; set; }

    [VectorStoreData]
    public required string RiskLevel { get; set; }

    // If you switch to a model with a different embedding dimension, update this attribute.
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public string EmbeddingInput => $"[{Reference}] {Title} | {Content} | Region={Region} | Risk={RiskLevel}";

    public string GetTitleAndDetails() =>
        $"[{Reference}] {Title} (Region: {Region}, Risk: {RiskLevel}) - {Content}";
}

internal sealed class CustomsSearchTool(InMemoryCollection<Guid, CustomsVectorStoreRecord> collection)
{
    [Description("Searches customs semantic knowledge and returns top matching snippets.")]
    public async Task<List<string>> SearchVectorStore(
        [Description("Customs question or keywords to search for.")] string question,
        [Description("Maximum number of snippets to return (1-8). Defaults to 5.")] int topK = 5)
    {
        var results = new List<string>();
        var effectiveTop = Math.Clamp(topK, 1, 8);

        await foreach (var hit in collection.SearchAsync(
                           question,
                           top: effectiveTop,
                           options: new VectorSearchOptions<CustomsVectorStoreRecord> { IncludeVectors = false }))
        {
            results.Add(hit.Record.GetTitleAndDetails());
        }

        return results;
    }
}
