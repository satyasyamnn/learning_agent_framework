#  Fundamentals 03: Anti-Pattern - Multi-Turn Without Session

## Quick Context
This project is a **deliberate anti-pattern** showing what happens when you try to maintain multi-turn conversations **without using sessions**. Each call to `RunAsync()` is independent  the agent has no memory of previous interactions.

**Point to Remember:** 
-  Understand the problem: LLM's are stateless by default 
-  There is no memory across turns. If a user comes back later, the agent has no idea who they are or what they previously asked
-  Even if we try to solve this by sending full history every time, we hit context limits.
-  No Persistence Across Restarts

**What Happens Without Memory Management**

“Without proper memory handling, agents exhibit bad behavior:

- Users repeat themselves every time
- Agents forget tool outputs mid-task
- Costs keep increasing
- No audit trail or debugging capability

The agent feels unreliable

---

## What Goes Wrong

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
Turn 2: User Message  New ChatCompletion Request  Fresh Context AGAIN
         
         Agent Response (still no awareness of Turn 1)         
         Response discarded 


### Why?
Each `RunAsync()` call creates a **fresh conversation** with just the agent's instructions. There's no history passed to the model, so the agent has **no context** from Turn 1.

---

## Why It Breaks

1. **No Memory:** Each `RunAsync()` is independent
2. **No History:** Previous messages aren't sent to the model
3. **No Context:** Agent can't retain or recall user information