using System.ComponentModel;
using System.Text;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

const int EmbeddingDimensions = 1536;
const string SqlConnectionStringEnvironmentVariable = "SQLSERVER2025_CONNECTION_STRING";

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

var sqlConnectionString = config.GetConnectionString(SqlConnectionStringEnvironmentVariable);
if (string.IsNullOrWhiteSpace(sqlConnectionString))
{
    throw new InvalidOperationException($"Set the {SqlConnectionStringEnvironmentVariable} environment variable to a SQL Server 2025 connection string before running this sample.");
}

var chatEndpoint = EndpointUtilities.NormalizeAzureOpenAiEndpoint(endpointUrl);
var embeddingEndpoint = EndpointUtilities.NormalizeAzureOpenAiEndpoint(string.IsNullOrWhiteSpace(embeddingEndpointUrl) ? endpointUrl : embeddingEndpointUrl);

AzureOpenAIClient client = new(new Uri(chatEndpoint), new AzureKeyCredential(apiKey));
var embeddingClient = new AzureOpenAIClient(new Uri(embeddingEndpoint), new AzureKeyCredential(string.IsNullOrWhiteSpace(embeddingApiKey) ? apiKey : embeddingApiKey));

var customsData = CustomsKnowledgeBaseData.Build();

Console.WriteLine("=== Customs RAG With SQL Server 2025 Vectors (Agent Framework) ===");
Console.WriteLine($"Chat model: {chatDeployment}");
Console.WriteLine($"Embedding model: {embeddingDeployment}");
Console.WriteLine();

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = embeddingClient
    .GetEmbeddingClient(embeddingDeployment)
    .AsIEmbeddingGenerator();

await SqlVectorKnowledgeStore.CreateDatabaseIfNeededAsync(sqlConnectionString);
if (!await SqlVectorKnowledgeStore.SupportsVectorTypeAsync(sqlConnectionString))
{
    throw new InvalidOperationException("This sample requires SQL Server 2025 (or later) with native vector type support.");
}

await SqlVectorKnowledgeStore.EnsureSchemaAsync(sqlConnectionString, EmbeddingDimensions);

var knowledgeStore = new SqlVectorKnowledgeStore(sqlConnectionString, embeddingGenerator, EmbeddingDimensions);

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("Vector mode: SQL Server native vector type");
Console.ResetColor();

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("Seeding customs vectors into SQL Server 2025...");
Console.ResetColor();

await knowledgeStore.SeedAsync(customsData);

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
    preloadEverythingMessages.Add(new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant,  entry.GetTitleAndDetails()));
}

preloadEverythingMessages.Add(new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, question));

var baselineResponse = await baselineAgent.RunAsync(preloadEverythingMessages);
Console.WriteLine(baselineResponse);
Console.WriteLine();
#endregion

#region Sample 2 - Retrieve top SQL vector matches
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Sample 2 - SQL Server 2025 vector retrieval before answering");
Console.ResetColor();

var ragPreloadMessages = new List<Microsoft.Extensions.AI.ChatMessage>
{
    new(Microsoft.Extensions.AI.ChatRole.Assistant, "Here are the most relevant customs snippets")
};

foreach (var match in await knowledgeStore.SearchAsync(question, topK: 5))
{
    ragPreloadMessages.Add(new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, match));
}

ragPreloadMessages.Add(new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, question));

var ragResponse = await baselineAgent.RunAsync(ragPreloadMessages);
Console.WriteLine(ragResponse);
Console.WriteLine();
#endregion

#region Sample 3 - Tool-based SQL vector retrieval
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Sample 3 - Agent retrieves customs snippets from SQL Server 2025 via tool call");
Console.ResetColor();

var searchTool = new CustomsSqlSearchTool(knowledgeStore);
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
        tools: [AIFunctionFactory.Create(searchTool.SearchSqlVectorStore)])
    .AsBuilder()
    .Use(LogFunctionCalls)
    .Build();

var toolResponse = await toolAgent.RunAsync(question);
Console.WriteLine(toolResponse);

Console.WriteLine();
Console.WriteLine("Ask customs questions (blank line to exit):");
while (true)
{
    Console.Write("customs-sql-rag> ");
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