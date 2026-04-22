#  Fundamentals 01: Simple Agent

## Quick Context
This project demonstrates the **most basic agent setup** in the Microsoft Agent Framework. It shows how to create an AI agent with simple instructions and handle single-turn interactions without tools or sessions.

---

## Setup

Set these in `appsettings.json`: This is where we typically store things like: 
 - model configuration
 -  API keys
 - endpoints

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<resource>.openai.azure.com/",
    "DeploymentName": "gpt-4o-mini",
    "ApiKey": "your-key-or-leave-empty-for-managed-identity"
  }
}
```

Use `DefaultAzureCredential` — works with az login locally and Managed Identity in production. No secrets in code `ApiKey` is only for DEMO purpose.


---

## Key Methods Used

| API | Purpose |
|-----|---------|
| `chatClient.AsAIAgent()` | Convert ChatClient to AIAgent |
| `agent.RunAsync()` | Execute single-turn agent interaction |
| `agent.RunStreamingAsync()` | Execute with streaming response |
| `AgentResponse` | Contains response text and metadata |


## Main Ideas

### Points to Consider

-  Create an AI agent from a ChatClient
-  Add instructions to guide agent behavior
-  Execute single-turn agent interactions
-  Handle both streaming and non-streaming responses

---


### 1. Agent Creation

```csharp
AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful supply chain and customs assistant...",
    name: "BasicAgent");
```

- Instructions: This is essentially the system prompt, we define the agent’s behavior—in this case
- Name: Just a logical identifier—BasicAgent

At this stage we defined the personality and purpose of our agent. The `AsAIAgent()` extension wraps a ChatClient to add AI-specific features. The instructions parameter defines the agent's system prompt and behavior.

### 2. Single-Turn Interaction

```csharp
AgentResponse response = await agent.RunAsync("What are three common causes of delays at customs?");
Console.WriteLine(response.Text);
```

`RunAsync()` sends a single message to the agent and returns the complete response. 

The question is about: What are three common causes of delays at customs?

This is a standard single-turn interaction:

- We send a prompt
- The agent processes it
- We get a complete response back

### 3. Streaming Response

```csharp
await foreach (var update in agent.RunStreamingAsync("List best practices for reducing delivery disruptions."))
{
    Console.Write(update);
}
```

`RunStreamingAsync()` streams the response token-by-token for real-time feedback.

Instead of waiting for the full response, we:

- Receive tokens incrementally
- Print them as they arrive

This is especially useful for:

- Chat applications
- Real-time UX
- Long responses

It makes the interaction feel much more responsive and interactive.”

---
