# Fundamentals 02: Simple Agent with OpenAI

## Quick Context
This sample demonstrates a minimal Microsoft Agent Framework agent that uses OpenAI directly through the official OpenAI .NET SDK.

It focuses on the integration path from:
- `OpenAIClient` and OpenAI model selection
- to `AsIChatClient()` adapter conversion
- to `AsAIAgent()` execution in Agent Framework

This is a practical bridge sample if you want to use OpenAI-hosted models while keeping Agent Framework patterns.

## Points to Consider

- How to read OpenAI configuration from environment variables
- How to create a `ChatClient` from `OpenAIClient`
- Why `AsIChatClient()` is required before calling `AsAIAgent()`
- How to run both full-response and streaming-response agent calls

## How It Works

### 1. Configure OpenAI

The sample expects:

```powershell
$env:OPENAI_API_KEY = "<your-openai-api-key>"
$env:OPENAI_CHAT_MODEL_NAME = "gpt-5.4-mini"
```

If `OPENAI_CHAT_MODEL_NAME` is not set, the sample defaults to `gpt-5.4-mini`.

### 2. Create Agent from OpenAI Client

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

### 3. Run Non-Streaming and Streaming

- `RunAsync()` returns a complete response payload.
- `RunStreamingAsync()` yields incremental response updates as they arrive.

Using both in one sample helps you compare UX and implementation trade-offs.

## Run It

```bash
cd 02-simple-agent-openai/02-simple-agent-openai
dotnet run
```

## Folder Layout

```text
02-simple-agent-openai/
    README.md
    02-simple-agent-openai/
        02-simple-agent-openai.csproj
        Program.cs
```

## Related Sessions

- Previous: [01-simple-agent](../01-simple-agent/README.md)
- Next: [03-anti-pattern-without-session](../03-anti-pattern-without-session/README.md)



