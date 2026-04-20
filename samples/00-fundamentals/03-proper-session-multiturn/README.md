# 💾 Fundamentals 04: Proper Multi-Turn with AgentSession

## Overview
This project demonstrates the **correct pattern for multi-turn conversations** using `AgentSession`. The agent maintains full conversation history, allowing it to remember context across multiple interactions and provide coherent, context-aware responses.

**Key Learning:** Sessions are essential for stateful, context-aware agent interactions.

---

## What You'll Learn

- ✅ Create and manage `AgentSession` for conversation history
- ✅ Execute multi-turn interactions with full context
- ✅ Monitor token usage per turn with `WriteTokenUsageToConsole()`
- ✅ Serialize and persist sessions for recovery
- ✅ Stream responses while maintaining session state

---

## Core Concepts

### 1. Create a Session

```csharp
AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful supply chain and customs assistant. Remember important operational details...",
    name: "ContextAwareAgent");

// Create a session - maintains conversation history
AgentSession session = await agent.CreateSessionAsync();
```

`AgentSession` holds conversation history and manages state across turns.

### 2. Execute Multi-Turn Interactions

```csharp
// Turn 1: User introduces themselves
AgentResponse response1 = await agent.RunAsync(
    "My name is Alice. I lead customs operations through Rotterdam and Singapore.", 
    session);
Console.WriteLine($"Agent: {response1.Text}");

// Turn 2: Agent remembers Alice
AgentResponse response2 = await agent.RunAsync(
    "What is my name?",  // ✅ Agent will remember!
    session);
Console.WriteLine($"Agent: {response2.Text}");

// Turn 3: Building on previous context
AgentResponse response3 = await agent.RunAsync(
    "Our baseline clearance time is 30 hours; my goal is under 24 hours.",
    session);

// Turn 4: Agent recalls everything
AgentResponse response4 = await agent.RunAsync(
    "Summarize what you know about my role, ports, and clearance target.",
    session);  // ✅ Full context available!
```

---

### 3. Token Usage Tracking

```csharp
response1.WriteTokenUsageToConsole("Turn 1");

// Output:
// ✓ Turn 1 | Input: 45 tokens | Output: 62 tokens | Total: 107 tokens
```

Monitor token consumption per turn for cost optimization.

### 4. Streaming with Session State

```csharp
await agent.RunStreamingAsync(
    "Give me three practical weekly actions to improve document quality.",
    session)
    .WriteStreamingResponseAndTokenUsageToConsoleAsync("Turn 6 (Streaming)");
```

Streaming works with sessions — tokens are still tracked.

### 5. Session Persistence

```csharp
// Serialize session for database/cache storage
var serializedSession = await agent.SerializeSessionAsync(session);

// In a real app: save serializedSession to Redis, SQL database, etc.
Console.WriteLine("✓ Session serialized successfully");

// Later, restore the session
var restoredSession = await agent.DeserializeSessionAsync(serializedSession);
AgentResponse nextResponse = await agent.RunAsync("Continue our discussion...", restoredSession);
```

Persist sessions to maintain conversations across app restarts.

---

## Project Structure

```
03-proper-session-multiturn/
├── Program.cs              # Multi-turn session demo with 6 turns
├── Shared/
│   └── TokenUsageConsoleExtensions.cs  # Helper to display token usage
├── appsettings.json        # Azure OpenAI config
└── 03-proper-session-multiturn.csproj
```

---

## Example Output

```
=== Proper Multi-Turn with AgentSession (Supply Chain Context) ===

>>> Turn 1: User shares logistics background
> My name is Alice. I lead customs operations for electronics imports through Rotterdam and Singapore.
Agent: I'm pleased to meet you, Alice...

✓ Turn 1 | Input: 54 tokens | Output: 68 tokens | Total: 122 tokens

>>> Turn 2: User asks agent to recall their name (SUCCESS: Agent remembers!)
> What is my name?
Agent: Your name is Alice. You lead customs operations...

✓ Turn 2 | Input: 89 tokens | Output: 42 tokens | Total: 131 tokens

>>> Turn 3: User provides more information
> Our baseline customs clearance time is 30 hours, and my goal is to get it under 24 hours this quarter.
Agent: That's an ambitious but achievable goal...

✓ Turn 3 | Input: 125 tokens | Output: 55 tokens | Total: 180 tokens

>>> Turn 4: User asks about previous info (SUCCESS: Agent recalls everything!)
> Can you summarize what you know about my role, ports, and clearance target?
Agent: Based on our conversation:
- Your name is Alice
- You manage electronics imports through Rotterdam and Singapore
- Your goal is reducing clearance from 30 to under 24 hours...

✓ Turn 4 | Input: 156 tokens | Output: 89 tokens | Total: 245 tokens
```

---

## Session State Flow

```
┌─────────────────────────────────────────────────────┐
│           User Starts Conversation                  │
│               (CreateSessionAsync)                  │
└──────────────────┬──────────────────────────────────┘
                   │
       ┌───────────▼───────────┐
       │   Session Created     │
       │ (empty chat history)  │
       └───────────┬───────────┘
                   │
       ┌───────────▼──────────────────────┐
       │  Turn 1: RunAsync(msg, session)  │
       │  - Message added to history      │
       │  - Response generated            │
       │  - Both stored in session        │
       └───────────┬──────────────────────┘
                   │
       ┌───────────▼──────────────────────┐
       │  Turn 2: RunAsync(msg, session)  │
       │  - Full history sent to model ✓  │
       │  - Agent has context             │
       │  - New exchange added            │
       └───────────┬──────────────────────┘
                   │
       ┌───────────▼──────────────────────┐
       │  Turn N: RunAsync(msg, session)  │
       │  - Complete conversation history │
       │  - Rich context available        │
       │  - Session continues...          │
       └──────────────────────────────────┘
```

---

## Key APIs

| API | Purpose |
|-----|---------|
| `agent.CreateSessionAsync()` | Create new session for conversation history |
| `agent.RunAsync(msg, session)` | Execute turn with session context |
| `agent.RunStreamingAsync(msg, session)` | Streaming with session state |
| `agent.SerializeSessionAsync(session)` | Persist session for storage |
| `agent.DeserializeSessionAsync(data)` | Restore session from storage |
| `response.WriteTokenUsageToConsole()` | Display token metrics |

---

## Best Practices

### Do's ✅
- **Always use sessions for multi-turn conversations**
- Serialize sessions for recovery after crashes
- Monitor token usage to manage costs
- Clear sessions when conversation ends
- Store sessions in Redis/database for scalability

### Don'ts ❌
- Don't create new agents for each turn
- Don't mix multiple users' messages in one session
- Don't forget to manage session lifecycle
- Don't assume all context is free (track tokens!)

---

## Session Lifecycle

```csharp
// 1. Create session
AgentSession session = await agent.CreateSessionAsync();

// 2. Multiple turns...
for (int i = 0; i < 10; i++)
{
    var response = await agent.RunAsync(userInput, session);
}

// 3. Optional: Persist session
var serialized = await agent.SerializeSessionAsync(session);
await database.SaveAsync(sessionId, serialized);

// 4. Optional: Restore later
var restored = await agent.DeserializeSessionAsync(serialized);

// 5. Continue or end
// - Continue: await agent.RunAsync(msg, restored)
// - End: Session can be garbage collected
```

---

## Configuration

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

## Running the Project

```bash
cd 03-proper-session-multiturn
dotnet run
```

You'll see 6 example turns demonstrating full context retention and token tracking.

---

## Common Patterns

### Pattern 1: Chat Loop
```csharp
AgentSession session = await agent.CreateSessionAsync();
while (true)
{
    string input = Console.ReadLine();
    var response = await agent.RunAsync(input, session);
    Console.WriteLine($"Agent: {response.Text}");
}
```

### Pattern 2: Session with Timeout
```csharp
var session = await agent.CreateSessionAsync();
var lastActivity = DateTime.UtcNow;

while ((DateTime.UtcNow - lastActivity).TotalMinutes < 30)
{
    var response = await agent.RunAsync(input, session);
    lastActivity = DateTime.UtcNow;
}
```

---

## Next Steps

- 👉 **Next Project:** [04-structured-output](../04-structured-output/README.md) - Get typed responses from agents
- 🔗 **Related:** [01-agent-with-tools](../01-agent-with-tools/README.md) - Combine sessions with tools
- 🔗 **Related:** [06-middleware-usage](../06-middleware-usage/README.md) - Monitor session interactions

---

## Comparison with Anti-Pattern

| Feature | Without Session (02) | With Session (04) |
|---------|---------------------|-------------------|
| Context Retention | ❌ None | ✅ Full history |
| Multi-Turn | ❌ Fails | ✅ Works perfectly |
| User Experience | ❌ Poor | ✅ Natural |
| Token Tracking | ✅ Simple | ✅ Per-turn tracking |
| Persistence | ❌ Lost on crash | ✅ Can serialize |

