# 🧠 Fundamentals 06: Reasoning Effort Controls

## Overview
This project demonstrates how to tune **reasoning effort levels** in agent responses. Different problems require different levels of thinking: simple questions need minimal reasoning, while complex analysis requires extended thought.

**Key Learning:** Control reasoning depth to balance cost, latency, and quality.

---

## What You'll Learn

- ✅ Use baseline (default) reasoning for standard tasks
- ✅ Set minimal reasoning for fast, simple responses
- ✅ Enable high reasoning for complex analysis
- ✅ Monitor reasoning tokens and cost implications
- ✅ Choose appropriate reasoning levels for your use case

---

## Core Concepts

### Reasoning Levels in OpenAI Models

```
┌─────────────────────────────────────────────┐
│      ChatReasoningEffortLevel Options        │
├─────────────────────────────────────────────┤
│ Baseline (Default)                          │
│ - Standard model reasoning behavior         │
│ - Balanced cost/quality                     │
│ - Best for general tasks                    │
│                                             │
│ Minimal                                     │
│ - Fastest responses                         │
│ - Lowest cost                               │
│ - For simple Q&A                            │
│                                             │
│ High                                        │
│ - Extended thinking enabled                 │
│ - Best for complex reasoning                │
│ - Highest cost & latency                    │
│ - More reasoning tokens used                │
└─────────────────────────────────────────────┘
```

---

### 1. Baseline Reasoning (Default)

```csharp
ChatClientAgent agent = azureOpenAIClient
    .GetChatClient(deploymentName)
    .AsAIAgent(
        name: "CustomsReasoningBaseline",
        instructions: "You are a customs clearance operations expert. Give concise and practical guidance.");

AgentResponse response = await agent.RunAsync(
    "For customs shipment CSH-9021 entering Germany from Singapore, " +
    "identify likely inspection focus areas and recommend a fast-track action plan. " +
    "Return in max 35 words.");

Console.WriteLine(response.Text);
response.WriteTokenUsageToConsole("Baseline");

// Output:
// Response: Focus on electronics certifications and origin verification...
// ✓ Baseline | Input: 45 tokens | Output: 28 tokens | Total: 73 tokens
```

**When to use:** Default choice for most tasks.

---

### 2. Minimal Reasoning

```csharp
ChatClientAgent agent = azureOpenAIClient
    .GetChatClient(deploymentName)
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "CustomsReasoningMinimal",
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a customs clearance operations expert. Give concise guidance.",
            RawRepresentationFactory = _ => new ChatCompletionOptions
            {
                ReasoningEffortLevel = ChatReasoningEffortLevel.Minimal
            }
        }
    });

AgentResponse response = await agent.RunAsync(customsQuestion);
response.WriteTokenUsageToConsole("Minimal");

// Output:
// ✓ Minimal | Input: 45 tokens | Output: 22 tokens | Total: 67 tokens
//
// ✅ Faster response, fewer tokens, simpler reasoning
```

**When to use:** 
- Simple factual questions
- Quick lookups
- When speed matters more than depth

---

### 3. High Reasoning

```csharp
// Using Responses API for detailed reasoning insights
var completionOptions = new ChatCompletionOptions
{
    ReasoningEffortLevel = ChatReasoningEffortLevel.High,
    Temperature = 1.0f  // Required for high reasoning
};

var response = await azureOpenAIClient
    .GetChatClient(deploymentName)
    .CompleteChatAsync(
        new ChatMessage[] { new UserChatMessage(customsQuestion) },
        completionOptions);

// Access reasoning content
var reasoning = response.Content
    .OfType<ChatCompletionTokenLogprob>()
    .FirstOrDefault();

Console.WriteLine($"Reasoning Summary: {reasoning?.ReasoningSummary}");
response.WriteTokenUsageToConsole("High Reasoning");

// Output:
// ✓ High Reasoning | Input: 45 tokens | Output: 187 tokens | Total: 232 tokens
//
// ⚠️ More tokens, longer latency, but deeper analysis
```

**When to use:**
- Complex multi-step problems
- Strategic decisions
- When quality is critical
- Trade compliance analysis

---

## Project Structure

```
05-reasoning-effort/
├── Program.cs              # 3 reasoning level demonstrations
├── appsettings.json        # Azure OpenAI config
└── 05-reasoning-effort.csproj
```

---

## Example Output Comparison

### Input Query:
```
"For customs shipment CSH-9021 entering Germany from Singapore, 
 identify likely inspection focus areas and recommend a fast-track action plan. 
 Return in max 35 words."
```

### Baseline Response:
```
Electronics shipments face enhanced scrutiny for compliance certifications.
Focus on import licenses and origin verification. Fast-track: pre-clear
documentation, use trusted broker.
```
**Tokens: 73 | Latency: ~500ms**

---

### Minimal Response:
```
Check import licenses and origin docs. Use trusted broker for fast-track.
```
**Tokens: 67 | Latency: ~250ms** (40% faster, fewer tokens)

---

### High Reasoning Response:
```
[Extended thinking process...]

Inspection Focus Areas:
1. Electronics origin verification (Singapore → Germany trade pattern)
2. Import licensing compliance for EU electronics directives
3. CITES compliance if sourcing includes natural materials
4. Dual-use technology screening (unlikely but checked)
5. Trade agreement benefits verification

Fast-Track Recommendation:
- Pre-file comprehensive origin documentation
- Obtain valid import license 60 days before shipment
- Use customs broker with high-compliance history
- Consider AEO (Authorized Economic Operator) status
```
**Tokens: 232 | Latency: ~2000ms** (4x slower, more comprehensive)

---

## Reasoning Effort Comparison

| Factor | Minimal | Baseline | High |
|--------|---------|----------|------|
| **Speed** | ⚡ Fastest | ⚡⚡ Medium | 🐢 Slowest |
| **Cost** | 💰 Lowest | 💰💰 Medium | 💰💰💰 Highest |
| **Output Tokens** | ~22 | ~28 | ~187 |
| **Depth** | Shallow | Balanced | Deep |
| **Best For** | Simple Q&A | General tasks | Complex analysis |
| **Example** | "What's the HS code?" | "Assess the shipment" | "Optimize clearance strategy" |

---

## Cost Implications

```
Baseline: 73 tokens @ $0.000005/token = ~$0.00037
Minimal:  67 tokens @ $0.000005/token = ~$0.00034 (8% savings)
High:    232 tokens @ $0.00002/token = ~$0.00464 (12x more expensive)

For 1000 queries:
- Minimal:  $340  (baseline)
- Baseline: $370
- High:    $4640  (strategic decisions only!)
```

---

## Decision Tree: Which Level to Use?

```
                    ┌─ Is it a quick lookup?
                    │  YES → Minimal ✓
                    │
Start Question ─────┤
                    │
                    │  NO → Is it a standard task?
                    │  YES → Baseline ✓
                    │
                    └─ Is it complex/high-stakes?
                       YES → High ✓
```

---

## Real-World Examples

### Use Minimal:
```csharp
// ✅ Quick reference lookups
"What is the HS code for electronics?"
"When is the port open?"
"Show me the tariff rate for shoes"
```

### Use Baseline:
```csharp
// ✅ Standard operational tasks
"Assess this shipment for compliance"
"What documents are required?"
"Estimate duty and processing time"
```

### Use High:
```csharp
// ✅ Complex strategic decisions
"Design an optimal customs clearance strategy"
"Analyze trade patterns to find cost savings"
"Create a risk mitigation plan for high-value shipments"
```

---

## Key APIs

| API | Purpose |
|-----|---------|
| `ChatReasoningEffortLevel.Minimal` | Fast, simple reasoning |
| `ChatReasoningEffortLevel.Baseline` | Default balanced reasoning |
| `ChatReasoningEffortLevel.High` | Extended thinking |
| `response.WriteTokenUsageToConsole()` | Monitor cost/latency |
| `ReasoningSummary` | Get reasoning explanation |

---

## Configuration

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<resource>.openai.azure.com/",
    "DeploymentName": "gpt-4-turbo",
    "ResponsesModel": "gpt-4-turbo",
    "ApiKey": "your-key-or-managed-identity"
  }
}
```

**Important:** Use models with reasoning support (gpt-4 series).

---

## Running the Project

```bash
cd 05-reasoning-effort
dotnet run
```

Observe the token counts and latency differences across reasoning levels.

---

## Best Practices

✅ **Start with Baseline:** Default choice for most tasks
✅ **Profile Before Optimizing:** Measure tokens/latency first
✅ **Use Minimal for UI:** Keep user-facing latency low
✅ **Reserve High for Batch:** Run expensive reasoning off-hours
✅ **Monitor Costs:** High reasoning can add significant expense

---

## Advanced: Adaptive Reasoning

```csharp
async Task<AgentResponse> RunWithAdaptiveReasoning(string question, AIAgent agent)
{
    // Start with baseline estimate
    var baselineResponse = await agent.RunAsync(question);
    
    // If response seems incomplete, upgrade to high reasoning
    if (baselineResponse.Text.Length < 50)
    {
        // Retry with high reasoning
        var improvedAgent = CreateAgentWithHighReasoning();
        return await improvedAgent.RunAsync(question);
    }
    
    return baselineResponse;
}
```

---

## Next Steps

- 👉 **Next Project:** [06-middleware-usage](../06-middleware-usage/README.md) - Monitor and intercept agent operations
- 🔗 **Related:** [04-structured-output](../04-structured-output/README.md) - Get reliable structured responses
- 🔗 **Related:** [03-proper-session-multiturn](../03-proper-session-multiturn/README.md) - Multi-turn with reasoning

