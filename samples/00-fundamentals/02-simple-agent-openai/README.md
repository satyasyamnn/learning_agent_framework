# Fundamentals 02: Simple Agent with OpenAI / Change the AgentProvider

## Quick Context
This sample demonstrates a minimal Microsoft Agent Framework agent that uses OpenAI directly through the official OpenAI .NET SDK.

It focuses on the integration path from:
- `OpenAIClient` and OpenAI model selection
- to `AsIChatClient()` adapter conversion
- to `AsAIAgent()` execution in Agent Framework

This is a practical bridge sample if you want to use OpenAI-hosted models while keeping Agent Framework patterns.

---

## Setup

Set these in `appsettings.json`: This is where we typically store things like: 
 - model configuration
 -  API keys
 - endpoints

```json
{
  "OpenAi": {    
    "ApiKey": "",
    "DeploymentName": "gpt-4o-mini"
  }
}
```
---

## Points to Consider

- How to read OpenAI configuration from environment variables
- How to create a `ChatClient` from `OpenAIClient`
- Why `AsIChatClient()` is required before calling `AsAIAgent()`
- How to run both full-response and streaming-response agent calls

## How It Works

### 1. Create Agent from OpenAI Client

`Program.cs` creates an OpenAI chat client and adapts it for Agent Framework:

```csharp
var openAIClient = new OpenAIClient(apiKey);
var chatClient = openAIClient.GetChatClient(modelName);

AIAgent agent = chatClient
        .AsIChatClient()
        .AsAIAgent(
                name: "OpenAISimpleAgent",
                instructions: "You are a concise supply chain assistant.");
```

### 2. Run Non-Streaming and Streaming

- `RunAsync()` returns a complete response payload.
- `RunStreamingAsync()` yields incremental response updates as they arrive.

Using both in one sample helps you compare UX and implementation trade-offs.