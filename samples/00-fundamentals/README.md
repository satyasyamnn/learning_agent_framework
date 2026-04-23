# Agent Framework Fundamentals

Microsoft Agent Framework samples in .NET, arranged as a progressive from basic agent usage through sessions, structured output, middleware, tools, and skills.

## Code Flow Order

| Order | Folder | Project README | Purpose |
| --- | --- | --- | --- |
| 01 | `01-simple-agent` | [Open](./01-simple-agent/README.md) | Basic single-turn agent |
| 02 | `02-simple-agent-openai` | [Open](./02-simple-agent-openai/README.md) | Minimal OpenAI-based agent using `OpenAIClient` and `AsAIAgent()` |
| 03 | `03-anti-pattern-without-session` | [Open](./03-anti-pattern-without-session/README.md) | Why multi-turn without `AgentSession` fails |
| 04 | `04-proper-session-multiturn` | [Open](./04-proper-session-multiturn/README.md) | Correct multi-turn usage with `AgentSession` |
| 05 | `05-structured-output` | [Open](./05-structured-output/README.md) | Typed/structured responses |
| 06 | `06-reasoning-effort` | [Open](./06-reasoning-effort/README.md) | Reasoning-effort controls |
| 07 | `07-agent-with-tools` | [Open](./07-agent-with-tools/README.md) | Tool/function calling |
| 08 | `08-middleware-usage` | [Open](./08-middleware-usage/README.md) | Middleware and interception |
| 09 | `09-agent-framework-skills` | [Open](./09-agent-framework-skills/README.md) | Inline skills |
| 10 | `10-csharp-file-script-runner` | [Open](./10-csharp-file-script-runner/README.md) | File-based skills with C# scripts |

## Pre-Requisites

- .NET SDK 10
- Valid models deployed to Azure / Open AI
- Valid model credentials configured in appsettings.json (Azure / Open AI)