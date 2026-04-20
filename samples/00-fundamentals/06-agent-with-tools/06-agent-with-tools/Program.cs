#pragma warning disable MEAI001
using System.Reflection;
using System.Text;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var endpointUrl    = config["AzureOpenAI:Endpoint"]       ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
var deploymentName = config["AzureOpenAI:DeploymentName"] ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName not configured");
var apiKey         = config["AzureOpenAI:ApiKey"];
var endpoint       = new Uri(new Uri(endpointUrl).GetLeftPart(UriPartial.Authority));

var azureClient = string.IsNullOrWhiteSpace(apiKey)
    ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
    : new AzureOpenAIClient(endpoint, new AzureKeyCredential(apiKey));

CustomsQueryTools queryTools = new();
MethodInfo[] methods = typeof(CustomsQueryTools).GetMethods(
    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

SimpleTools simpleTools = new();
MethodInfo[] simpleMethods = typeof(SimpleTools).GetMethods(
    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

List<AITool> tools = methods
    .Select(m => AIFunctionFactory.Create(m, queryTools))
    .Concat(simpleMethods.Select(m => AIFunctionFactory.Create(m, simpleTools)))
    .Cast<AITool>()
    .ToList();

tools.Add(new ApprovalRequiredAIFunction(
    AIFunctionFactory.Create(RestrictedOfficerActions.FlagShipmentForDetention)));



AIAgent agent = azureClient
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: """
            You are a customs operations assistant with access to shipment data,
            tariff lookups, sanction screening, and duty calculations.
            Always use your tools to retrieve accurate data before answering.
            If a shipment originates from a sanctioned country or carries restricted goods,
            you may recommend flagging it for detention — but that requires officer approval.
            Use the available tools to answer questions about port risk status, customs duty estimation, and clearance time. Always provide helpful and accurate information. If a user asks about a shipment that originates from a sanctioned country or carries restricted goods, you may recommend flagging it for detention — but that requires officer approval, so be sure to use the detention tool which will trigger an approval request to the operator.            
            """,
        tools: tools)
    .AsBuilder()
    .Use(ToolCallingMiddleware)
    .Build();

// ── 4. Multi-turn session loop with approval gate ─────────────────────────────
AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("=============================================================");
Console.WriteLine("  Customs — Advanced Tool Calling Demo");
Console.WriteLine("=============================================================");
Console.WriteLine("Suggested prompts:");
Console.WriteLine("  > What is the current disruption risk status at Rotterdam port?");
Console.WriteLine("  > Estimate duty for a shipment valued at 120000 USD with a duty rate of 7.5%. Include a short operational note.");
Console.WriteLine("  > Which port has lower disruption risk: Singapore or Los Angeles? Also estimate duty on 50000 USD at 4%.");
Console.WriteLine("  > What is the status of shipment CSH-3004?");
Console.WriteLine("  > Look up the tariff for HS code 9014.20");
Console.WriteLine("  > Is Iran sanctioned?");
Console.WriteLine("  > Calculate duty on HS 8507.60 with declared value 10000");
Console.WriteLine("  > Flag shipment CSH-3004 for detention  (triggers approval)");
Console.WriteLine("Type 'exit' to quit.\n");

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    AgentResponse response = await agent.RunAsync(new ChatMessage(ChatRole.User, input), session);

    // Approval gate — loop until every pending approval request is resolved
    List<ToolApprovalRequestContent> approvalRequests = response.Messages
        .SelectMany(m => m.Contents)
        .OfType<ToolApprovalRequestContent>()
        .ToList();

    while (approvalRequests.Count > 0)
    {
        List<ChatMessage> approvalResponses = approvalRequests
            .Select(request =>
            {
                var toolCall = (FunctionCallContent)request.ToolCall;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[OFFICER APPROVAL REQUIRED]");
                Console.WriteLine($"  Tool     : {toolCall.Name}");
                if (toolCall.Arguments?.Count > 0)
                    Console.WriteLine($"  Arguments: {string.Join(", ", toolCall.Arguments.Select(a => $"{a.Key} = {a.Value}"))}");
                Console.Write("  Approve? Enter Y to confirm, anything else to deny: ");
                Console.ResetColor();

                bool approved = Console.ReadLine()?.Equals("Y", StringComparison.OrdinalIgnoreCase) ?? false;
                return new ChatMessage(ChatRole.User, [request.CreateResponse(approved)]);
            })
            .ToList();

        response = await agent.RunAsync(approvalResponses, session);

        approvalRequests = response.Messages
            .SelectMany(m => m.Contents)
            .OfType<ToolApprovalRequestContent>()
            .ToList();
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n{response.Text}");
    Console.ResetColor();
    Console.WriteLine(new string('-', 60));
}

// ── Middleware: log every tool invocation before it executes ─────────────────
async ValueTask<object?> ToolCallingMiddleware(
    AIAgent callingAgent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken)
{
    var sb = new StringBuilder();
    sb.Append($"- Tool Call: '{context.Function.Name}'");
    if (context.Arguments.Count > 0)
        sb.Append($" (Args: {string.Join(", ", context.Arguments.Select(a => $"[{a.Key} = {a.Value}]"))})");

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine(sb.ToString());
    Console.ResetColor();

    return await next(context, cancellationToken);
}
