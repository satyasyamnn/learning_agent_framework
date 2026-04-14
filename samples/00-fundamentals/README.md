# 00-Fundamentals - Microsoft Agentic Framework Learning Path

This folder contains four progressive examples designed to teach you the fundamentals of the Microsoft Agentic Framework. Start with Example 1 and progress through each example in order to build your understanding.

## 📚 Learning Path

### Example 1: Simple Agent
**Folder:** `00-simple-agent/`

**What You'll Learn:**
- How to create a basic AI agent from a chat client
- Single-turn agent invocations (non-streaming and streaming)
- Using agent instructions to guide behavior
- When you DON'T need sessions

**Key Concepts:**
```csharp
// Convert a chat client to an agent
AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful assistant...",
    name: "BasicAgent");

// Single-turn invocation - no session
string response = await agent.RunAsync("Your question here");

// Streaming API
await foreach (var update in agent.RunStreamingAsync("Your question here"))
{
    Console.Write(update);
}
```

**Use Cases:**
- Simple Q&A scenarios
- Independent queries that don't need context
- Stateless API endpoints
- One-off information requests

**Run It:**
```bash
cd 00-simple-agent/00-simple-agent
dotnet run
```

---

### Example 2: Agent with Tools
**Folder:** `01-agent-with-tools/`

**What You'll Learn:**
- Adding tools/functions to extend agent capabilities
- Using `[Description]` attributes for function and parameter documentation
- How the agent intelligently selects and uses tools
- Building a travel assistant with weather, conversion, and info tools

**Key Concepts:**
```csharp
[Description("Get the weather for a location.")]
static string GetWeather([Description("The city name")] string city)
    => /* implementation */;

AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a travel assistant...",
    tools: [AIFunctionFactory.Create(GetWeather)]);

// Agent automatically decides when to use the tool
string response = await agent.RunAsync("What's the weather in Paris?");
```

**Best Practices:**
- Use clear, descriptive names for functions
- Document parameters with `[Description]` to guide LLM usage
- Keep tool implementations simple and deterministic
- Group related tools together

**Use Cases:**
- Information retrieval (weather, stock prices, etc.)
- Calculations and transformations
- API integrations
- Database queries
- External system interactions

**Run It:**
```bash
cd 01-agent-with-tools/01-agent-with-tools
dotnet run
```

---

### Example 3: Anti-Pattern - Missing Session for Multi-Turn
**Folder:** `02-anti-pattern-without-session/`

**What You'll Learn:**
- ❌ What NOT to do with multi-turn conversations
- Why calling `agent.RunAsync()` multiple times without a session loses context
- The problems with stateless multi-turn attempts
- Why this pattern degrades user experience

**Key Problem:**
```csharp
// ❌ ANTI-PATTERN: Multiple calls without session
string response1 = await agent.RunAsync("My name is Alice");
string response2 = await agent.RunAsync("What's my name?"); 
// Agent won't remember! Each call is independent.
```

**Problems Demonstrated:**
- Agent loses context between calls
- No conversation history maintained
- Each call starts fresh
- Agent appears to have no memory
- Poor user experience

**Why This Example Matters:**
Understanding what NOT to do is just as important as knowing what to do. This example shows the consequences of missing sessions for multi-turn scenarios, setting up the correct solution in Example 4.

**Run It:**
```bash
cd 02-anti-pattern-without-session/02-anti-pattern-without-session
dotnet run
```

---

### Example 4: Proper Multi-Turn with AgenticSession
**Folder:** `03-proper-session-multiturn/`

**What You'll Learn:**
- ✓ The correct way to implement multi-turn conversations
- Creating and using `AgenticSession` to maintain context
- How AgenticSession maintains message history
- Serializing and deserializing sessions for persistence
- When and why to use sessions

**Key Concepts:**
```csharp
// ✓ PROPER PATTERN: Use AgenticSession
AgentSession session = await agent.CreateSessionAsync();

string response1 = await agent.RunAsync("My name is Alice", session);
string response2 = await agent.RunAsync("What's my name?", session);
// Agent remembers! Session maintains context.

// Sessions can be persisted
var serialized = await agent.SerializeSessionAsync(session);
// Save to database or file...

// Later, restore and continue
var restored = await agent.DeserializeSessionAsync(serialized);
string response3 = await agent.RunAsync("Tell me about myself", restored);
```

**Benefits of Sessions:**
- Full conversation history maintained
- Agent context across multiple turns
- Sessions can be serialized for persistence
- Works with both streaming and non-streaming APIs
- Allows independent sessions per user

**Use Cases:**
- Chatbots and interactive dialogs
- Customer support conversations
- Multi-turn problem solving
- Educational tutoring systems
- Any scenario requiring conversation continuity

**Best Practices:**
- Create one session per conversation/user
- Always pass the session to every `RunAsync()` call
- Serialize sessions for persistence in production
- Different users should have different sessions
- Consider session timeout/cleanup strategies

**Run It:**
```bash
cd 03-proper-session-multiturn/03-proper-session-multiturn
dotnet run
```

---

## 🎯 Quick Decision Guide

### Choose Example 1 (Simple Agent) When:
- You need a one-time answer to a question
- Building a stateless API endpoint
- Simple information lookup
- No conversation history needed

### Choose Example 2 (Agent with Tools) When:
- Your agent needs to access external data/systems
- You want the agent to call specific functions
- Building domain-specific capabilities
- Need real-time calculations or integrations

### Avoid Example 3 (Anti-Pattern) When:
- Actually building your application!
- It's here for educational purposes only
- Shows pitfalls to avoid

### Choose Example 4 (Proper Session) When:
- Building a chatbot or conversation interface
- User experience depends on context awareness
- Multi-turn interactions required
- You need to remember user information
- Building customer support systems

---

## 🔗 Progression Flow

```
Simple Agent (Example 1)
         ↓
         ├─→ Add Tools (Example 2)
         │   
         └─→ Multi-turn Conversations?
             ├─→ ❌ What NOT to do (Example 3)
             │
             └─→ ✓ Correct Way (Example 4)
```

---

## 🛠️ Configuration

Before running any examples, update your `appsettings.json` in each folder:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "DeploymentName": "gpt-4o",
    "ApiKey": "<your-api-key>"
  }
}
```

Or use environment variables:
```bash
$env:AZURE_OPENAI_ENDPOINT="https://..."
$env:AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o"
```

---

## 📖 Key Concepts Reference

### AIAgent
- Primary interface for interacting with AI models
- Created from a chat client using `.AsAIAgent()`
- Supports both streaming and non-streaming invocations
- Optional: can include tools and instructions

### AgentSession
- Maintains conversation history and context
- Created via `agent.CreateSessionAsync()`
- Passed to each `RunAsync()` call for continuity
- Can be serialized/deserialized for persistence

### AIFunctionFactory
- Creates tools from methods with Description attributes
- Enables agent to call functions automatically
- Requires method documentation for LLM understanding

### Streaming vs Non-Streaming
- Non-streaming: `await agent.RunAsync(prompt)` - waits for complete response
- Streaming: `await foreach (var update in agent.RunStreamingAsync(prompt))` - receives response segments

---

## 🚀 Next Steps

After mastering fundamentals:
1. Combine multiple sessions for multi-user scenarios
2. Add memory components (see advanced samples)
3. Build workflows with multiple agents
4. Implement persistent session storage
5. Create production-ready chatbot applications

---

## 📚 Related Resources

- [Microsoft Agent Framework Documentation](https://github.com/microsoft/agent-framework)
- [01-get-started folder](../01-supply-chain/) - Real-world examples
- [Azure OpenAI Documentation](https://learn.microsoft.com/azure/ai-services/openai/)

---

## ⚠️ Important Notes

1. **DefaultAzureCredential**: Examples use this for convenience in development. In production, use more specific credentials (ManagedIdentityCredential, etc.)

2. **Configuration**: Each example reads from `appsettings.json`. Ensure it's copied to the output directory.

3. **Errors**: If you get "not found" errors, ensure your Azure OpenAI endpoint and deployment name are correct.

4. **Rate Limiting**: Be cautious with streaming examples in loops - monitor API usage.

---

## 💡 Pro Tips

- Experiment with different instructions in Example 1 to see how they affect behavior
- In Example 2, try adding more tools and observe how the agent prioritizes them
- Example 3 is educational - compare its output to Example 4's when asking for recall
- Example 4's serialization feature is crucial for production chatbots

---

Happy learning! Start with Example 1 and progress through each example to build your expertise. 🎓
