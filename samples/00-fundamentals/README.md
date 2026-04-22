# Agent Framework Fundamentals

Microsoft Agent Framework samples in .NET, arranged as a progressive from basic agent usage through sessions, structured output, middleware, tools, and skills.

## Code Flow Order

| Order | Folder | Purpose |
| --- | --- | --- |
| 01 | `01-simple-agent` | Basic single-turn agent |
| 02 | `02-simple-agent-openai` | Minimal OpenAI-based agent using `OpenAIClient` and `AsAIAgent()` |
| 03 | `03-anti-pattern-without-session` | Why multi-turn without `AgentSession` fails |
| 04 | `04-proper-session-multiturn` | Correct multi-turn usage with `AgentSession` |
| 05 | `05-structured-output` | Typed/structured responses |
| 06 | `06-reasoning-effort` | Reasoning-effort controls |
| 07 | `08-agent-with-tools` | Tool/function calling |
| 08 | `07-middleware-usage` | Middleware and interception |
| 09 | `09-agent-framework-skills` | Inline skills |
| 10 | `10-csharp-file-script-runner` | File-based skills with C# scripts |

## Before You Start

- .NET SDK 10
- Valid model credentials configured in appsettings.json (Azure / Open AI)