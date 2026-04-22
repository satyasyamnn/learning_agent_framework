#  Fundamentals 03: Anti-Pattern - Multi-Turn Without Session

## Quick Context
This project is a **deliberate anti-pattern** showing what happens when you try to maintain multi-turn conversations **without using sessions**. Each call to `RunAsync()` is independent  the agent has no memory of previous interactions.

**Point to Remember:** Why `AgentSession` is essential for context-aware conversations.

---

## Points to Consider

-  Understand the problem: stateless agent interactions
-  See why agents lose context without sessions
-  Recognize the pattern to avoid in production
-  Contrast with proper session-based approach

---

## What Goes Wrong

### Turn 1: User introduces themselves
```csharp
AgentResponse response1 = await agent.RunAsync("My name is Alice. I manage customs clearance for electronics imports through Rotterdam.");
// Agent: "Nice to meet you, Alice. I can help with customs clearance..."
```

### Turn 2: User asks agent to recall their name
```csharp
AgentResponse response2 = await agent.RunAsync("What is my name?");
//  Agent: "I don't have that information in our conversation..."
```

### Why?
Each `RunAsync()` call creates a **fresh conversation** with just the agent's instructions. There's no history passed to the model, so the agent has **no context** from Turn 1.

---

## How This Fails

```
Turn 1: User Message  New ChatCompletion Request  Fresh Context
         
         Agent Response (instruction-only, no history)
         
         Response discarded!

Turn 2: User Message  New ChatCompletion Request  Fresh Context AGAIN
         
         Agent Response (still no awareness of Turn 1)
         
         Response discarded again!

Turn 3: User Message  Same pattern repeats...
```

---

## Main Ideas

### What NOT to Do

```csharp
//  ANTI-PATTERN: No session management
AIAgent agent = chatClient.AsAIAgent(
    instructions: "Remember important details...",
    name: "ContextAgent");

AgentResponse response1 = await agent.RunAsync("My name is Alice...");
AgentResponse response2 = await agent.RunAsync("What's my name?");  // Agent won't know!
AgentResponse response3 = await agent.RunAsync("And my role?");      // Lost all context!
```

The instructions say "Remember important details" but there's **no mechanism to persist history**.

---

## Folder Layout

```
03-anti-pattern-without-session/
 Program.cs              # Anti-pattern demo (4 turns, agent loses context)
 appsettings.json        # Azure OpenAI config
 03-anti-pattern-without-session.csproj
```

---

## Sample Output

```
=== Anti-Pattern: Multi-Turn without Session ===
  WARNING: This example demonstrates an ANTI-PATTERN!

>>> Turn 1: User shares their logistics profile
> My name is Alice. I manage customs clearance...
Agent: Nice to meet you, Alice! I can help with...

>>> Turn 2: User asks agent to recall details (PROBLEM: Agent won't remember!)
> What is my name?
Agent: I don't have information about your name in our current conversation...

>>> Turn 3: User provides more information
> Our baseline customs clearance time is 30 hours...
Agent: That's helpful to know...

>>> Turn 4: User asks about previous info (PROBLEM: Agent has lost all context!)
> Can you summarize what you know about my role and clearance target?
Agent: I don't have enough context to provide a detailed summary...
```

---

## Why It Breaks

1. **No Memory:** Each `RunAsync()` is independent
2. **No History:** Previous messages aren't sent to the model
3. **No Context:** Agent can't retain or recall user information
4. **Poor UX:** Users feel like the agent has amnesia

---

## What To Do Instead

Use `AgentSession` to maintain conversation history:

```csharp
//  CORRECT: Using AgentSession
AgentSession session = await agent.CreateSessionAsync();

AgentResponse response1 = await agent.RunAsync("My name is Alice...", session);
AgentResponse response2 = await agent.RunAsync("What's my name?", session);  //  Agent remembers!
AgentResponse response3 = await agent.RunAsync("And my role?", session);      //  Full context!
```

---

## Where This Shows Up

Watch out for these signs:
-  Multiple `RunAsync()` calls without a session parameter
-  Comments like "Remember what I said earlier"
-  Using `RunAsync()` in a loop without `AgentSession`
-  Treating agent as if it has memory without sessions

---

## Quick Takeaways

| Aspect | Without Session () | With Session () |
|--------|----------------------|-------------------|
| **Memory** | None  starts fresh each turn | Full conversation history |
| **Context** | Only instructions + current message | Instructions + all previous turns |
| **Use Case** | Single questions only | Multi-turn conversations |
| **User Experience** | Feels like agent has amnesia | Natural, continuous conversation |
| **Token Usage** | Lower per turn (no history) | Higher per turn (includes history) |

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
cd 03-anti-pattern-without-session
dotnet run
```

Observe how the agent loses context between turns.

---

## Try Next

-  **Next Project:** [04-proper-session-multiturn](../04-proper-session-multiturn/README.md) - **See the correct pattern!**
-  **Related:** [07-middleware-usage](../07-middleware-usage/README.md) - Monitor session interactions

---

## Helpful Links

- [Microsoft Agent Framework Documentation](https://github.com/microsoft/agents)
- [AgentSession API Reference](../README.original.md)




