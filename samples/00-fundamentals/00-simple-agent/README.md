# 🤖 Fundamentals 01: Simple Agent

## Overview
This project demonstrates the **most basic agent setup** in the Microsoft Agent Framework. It shows how to create an AI agent with simple instructions and handle single-turn interactions without tools or sessions.

**Key Learning:** Minimal code needed to get an agent responding to queries.

---

## What You'll Learn

- ✅ Create an AI agent from a ChatClient
- ✅ Add instructions to guide agent behavior
- ✅ Execute single-turn agent interactions
- ✅ Handle both streaming and non-streaming responses

---

## Core Concepts

### 1. Agent Creation

```csharp
AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful supply chain and customs assistant...",
    name: "BasicAgent");
```

The `AsAIAgent()` extension wraps a ChatClient to add AI-specific features. The instructions parameter defines the agent's system prompt and behavior.

### 2. Single-Turn Interaction

```csharp
AgentResponse response = await agent.RunAsync("What are three common causes of delays at customs?");
Console.WriteLine(response.Text);
```

`RunAsync()` sends a single message to the agent and returns the complete response. No session or history is maintained.

### 3. Streaming Response

```csharp
await foreach (var update in agent.RunStreamingAsync("List best practices for reducing delivery disruptions."))
{
    Console.Write(update);
}
```

`RunStreamingAsync()` streams the response token-by-token for real-time feedback.

---

## Project Structure

```
00-simple-agent/
├── Program.cs           # Main entry point with example queries
├── appsettings.json     # Azure OpenAI configuration
└── 00-simple-agent.csproj
```

---

## Example Output

```
>>> Example 1: Single turn - non-streaming

Response: Common delays at customs checkpoints include:
1. Document discrepancies
2. Port congestion
3. Cargo inspection backlogs
```

---

## When to Use This Pattern

✅ **Use when:**
- You need a quick one-off query
- No conversation history needed
- Simple Q&A interactions

❌ **Don't use when:**
- You need multi-turn conversations
- User context needs to be remembered
- Complex workflows with tool calling

---

## Configuration

Set these in `appsettings.json`:
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<resource>.openai.azure.com/",
    "DeploymentName": "gpt-4o-mini",
    "ApiKey": "your-key-or-leave-empty-for-managed-identity"
  }
}
```

---

## Next Steps

- 👉 **Next Project:** [01-agent-with-tools](../01-agent-with-tools/README.md) - Add function calling and tools
- 🔗 **Related:** [03-proper-session-multiturn](../03-proper-session-multiturn/README.md) - Multi-turn with session memory

---

## Running the Project

```bash
cd 00-simple-agent
dotnet run
```

Expected output shows three example queries and their responses from the agent.

---

## Key APIs

| API | Purpose |
|-----|---------|
| `chatClient.AsAIAgent()` | Convert ChatClient to AIAgent |
| `agent.RunAsync()` | Execute single-turn agent interaction |
| `agent.RunStreamingAsync()` | Execute with streaming response |
| `AgentResponse` | Contains response text and metadata |

