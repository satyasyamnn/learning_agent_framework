# Fundamentals 06: Reasoning Effort Controls

[<- Back to Fundamentals Index](../README.md#code-flow-order)

Control reasoning depth to balance cost, latency, and quality.

---

## What is LLM Reasoning?

Large Language Models (LLMs) don’t just generate answers instantly—they often perform internal reasoning before responding. They perform internal **chain-of-thought reasoning** before responding — thinking step-by-step before producing the final answer. This reasoning is hidden from the output but still consumes tokens and affects cost.

### Token Types

| Token Type | Visible? | Billed? | Description |
| ---------- | -------- | ------- | ----------- |
| Input | Yes | Yes | What you send to the model |
| Output | Yes | Yes | What the model returns |
| Reasoning | No | Yes | Internal "thinking" steps |

---

## Reasoning Levels

| Level | Speed | Cost | Reasoning Tokens | Best For | Example Use Cases |
| ----- | ----- | ---- | --------------- | -------- | ----------------- |
| **Minimal** | Fastest | Lowest | Few | Simple Q&A, quick lookups | "What is the HS code for electronics?", "When does the port close?", "Show tariff rate for shoes" |
| **Baseline** | Medium | Medium | Moderate | General tasks, standard operations | "Assess this shipment for compliance", "What documents are required?", "Estimate duty and processing time" |
| **High** | Slowest | Highest | Many | Complex analysis, high-stakes decisions | "Design an optimal clearance strategy", "Analyze trade patterns for cost savings", "Create a risk mitigation plan" |

---

## Decision Guide

When deciding which level to use, consider the nature of the task:

1. **Quick lookup or simple fact?** → Use `Minimal`
2. **Standard task or general analysis?** → Use `Baseline` (Default)
3. **Complex, high-stakes, or multi-step problem?** → Use `High`

---

## Code Examples

### Baseline (Default)

```csharp
ChatClientAgent agent = azureOpenAIClient
    .GetChatClient(deploymentName)
    .AsAIAgent(
        name: "CustomsReasoningBaseline",
        instructions: "You are a customs clearance operations expert.");

AgentResponse response = await agent.RunAsync(customsQuestion);
response.WriteTokenUsageToConsole("Baseline");
// Baseline | Input: 45 | Output: 28 | Total: 73 tokens
```

### Minimal

```csharp
ChatClientAgent agent = azureOpenAIClient
    .GetChatClient(deploymentName)
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "CustomsReasoningMinimal",
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a customs clearance operations expert.",
            RawRepresentationFactory = _ => new ChatCompletionOptions
            {
                ReasoningEffortLevel = ChatReasoningEffortLevel.Minimal
            }
        }
    });

AgentResponse response = await agent.RunAsync(customsQuestion);
response.WriteTokenUsageToConsole("Minimal");
// Minimal | Input: 45 | Output: 22 | Total: 67 tokens
```

### High

```csharp
var response = await azureOpenAIClient
    .GetChatClient(deploymentName)
    .CompleteChatAsync(
        new ChatMessage[] { new UserChatMessage(customsQuestion) },
        new ChatCompletionOptions
        {
            ReasoningEffortLevel = ChatReasoningEffortLevel.High,
            Temperature = 1.0f  // Required for high reasoning
        });

Console.WriteLine(response.Content[0].Text);
response.WriteTokenUsageToConsole("High Reasoning");
// High Reasoning | Input: 45 | Output: 187 | Total: 232 tokens
```