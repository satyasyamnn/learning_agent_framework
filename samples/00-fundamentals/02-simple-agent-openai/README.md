# Fundamentals 02: Simple Agent with OpenAI / Change the AgentProvider

[<- Back to Fundamentals Index](../README.md#code-flow-order)

## Quick Context
This sample demonstrates a minimal Microsoft Agent Framework agent that uses OpenAI directly through the official OpenAI .NET SDK.

It focuses on the integration path from:
- `OpenAIClient` and OpenAI model selection
- to `AsIChatClient()` adapter conversion
- to `AsAIAgent()` execution in Agent Framework

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

## How to create Agent 

-  Create an AI agent from a ChatClient using OpenAIClient
-  Add instructions to guide agent behavior
-  Execute single-turn agent interactions
-  Handle both streaming and non-streaming responses

---

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