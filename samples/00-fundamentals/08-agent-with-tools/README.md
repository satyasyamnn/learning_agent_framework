#  Fundamentals 08: Agent with Tools

## Quick Context
This project demonstrates how to extend agents with **function calling** capabilities. The agent can call tools to retrieve real-time data, perform calculations, or execute domain-specific logic before responding.

**Point to Remember:** Tools enable agents to access external data and services dynamically.

---

## Points to Consider

-  Register tools with an agent using `AIFunctionFactory`
-  Use reflection to expose public methods as tools
-  Enable agent function calling (tool invocation)
-  Combine tools from multiple classes
-  Handle tool responses and integrate them into answers

---

## Main Ideas

### 1. Define Tool Methods

```csharp
public class CustomsQueryTools
{
    [Description("Get the current disruption risk status at a port.")]
    public string GetPortDisruptionStatus([Description("Port name")] string portName)
    {
        // Return port status (e.g., "Rotterdam: Low disruption risk")
        return $"{portName}: Current disruption risk data...";
    }

    [Description("Estimate customs duty from declared value and rate.")]
    public decimal EstimateDuty(decimal declaredValue, decimal dutyRatePercent)
    {
        return declaredValue * (dutyRatePercent / 100m);
    }
}
```

Use `[Description]` attributes to explain parameters for the agent.

### 2. Register Tools via Reflection

```csharp
CustomsQueryTools queryTools = new();
MethodInfo[] methods = typeof(CustomsQueryTools).GetMethods(
    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

List<AITool> tools = methods
    .Select(m => AIFunctionFactory.Create(m, queryTools))
    .Cast<AITool>()
    .ToList();
```

Reflection automatically discovers public methods and wraps them as tools.

### 3. Create Agent with Tools

```csharp
AIAgent agent = chatClient
    .AsAIAgent(
        instructions: "You are a customs assistant with access to port data and duty calculators.",
        tools: tools)
    .AsBuilder()
    .Use(ToolCallingMiddleware)  // Optional: log tool invocations
    .Build();
```

The agent now has access to all registered tools.

### 4. Multi-Turn Session with Tool Calling

```csharp
AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string userInput = Console.ReadLine();
    
    AgentResponse response = await agent.RunAsync(userInput, session);
    Console.WriteLine($"Agent: {response.Text}\n");
}
```

---

## Folder Layout

```
08-agent-with-tools/
 Program.cs              # Main entry point with session loop
 Tools/
    CustomsQueryTools.cs    # Customs domain tools (port status, duty calc, etc.)
    SimpleTools.cs          # General utilities
    ApprovalRequiredAIFunction.cs  # Special tool with approval gate
 appsettings.json        # Azure OpenAI config
 08-agent-with-tools.csproj
```

---

## Sample Interaction

```
>>> Suggested prompts:
  > What is the current disruption risk at Rotterdam?
  > Estimate duty for a shipment valued at $120,000 with 7.5% rate
  > Which port has lower disruption risk: Singapore or LA?

> What is the disruption risk at Rotterdam?

[Tool Called: GetPortDisruptionStatus(portName="Rotterdam")]
[Tool Result: "Rotterdam: Low disruption risk (2% congestion)"]

Agent: Rotterdam currently shows a low disruption risk with 2% congestion levels...
```

---

## Tool Tips

### Do's 
- Use descriptive `[Description]` attributes
- Keep tool parameters simple (strings, numbers, dates)
- Handle errors gracefully within tools
- Return string results for debugging

### Don'ts 
- Don't return complex nested objects
- Don't make tools extremely slow (>5 seconds)
- Don't expose sensitive operations without approval gates
- Avoid tools with too many parameters (use domain models instead)

---

## Special Case: Approval Gate

Some operations require human approval:

```csharp
tools.Add(new ApprovalRequiredAIFunction(
    AIFunctionFactory.Create(
        RestrictedOfficerActions.FlagShipmentForDetention)));
```

This wraps a tool to require user confirmation before execution.

---

## Key Methods Used

| API | Purpose |
|-----|---------|
| `AIFunctionFactory.Create(method, instance)` | Wrap method as tool |
| `chatClient.AsAIAgent(tools: tools)` | Create agent with tools |
| `agent.RunAsync(prompt, session)` | Execute with tool access |
| `AgentResponse` | Contains response + tool calls made |

---

## When Tools Help

 **Use tools when:**
- Agent needs real-time data (prices, inventory, status)
- Complex calculations required
- Integration with external systems
- Domain-specific operations

 **Don't use tools when:**
- Simple Q&A without data lookup
- All answers available in context
- Tool latency would hurt UX

---

## Setup

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<resource>.openai.azure.com/",
    "DeploymentName": "gpt-4o-mini",
    "ApiKey": "your-key-or-managed-identity"
  }
}
```

---

## Run It

```bash
cd 08-agent-with-tools
dotnet run
```

Interact with the agent by typing prompts. The agent will automatically call tools as needed.

---

## Try Next

-  **Next Project:** [03-anti-pattern-without-session](../03-anti-pattern-without-session/README.md) - See why sessions matter
-  **Related:** [04-proper-session-multiturn](../04-proper-session-multiturn/README.md) - Tools + Sessions together
-  **Related:** [07-middleware-usage](../07-middleware-usage/README.md) - Monitor tool calls with middleware




