# Agent Framework Fundamentals

This folder contains the foundational Microsoft Agent Framework samples in .NET, arranged as a progressive session flow from basic agent usage through sessions, structured output, middleware, tools, and skills.

## Session Flow

| Order | Folder | Purpose |
| --- | --- | --- |
| 01 | `01-simple-agent` | Basic single-turn agent |
| 02 | `02-simple-agent-openai` | Minimal OpenAI-based agent using `OpenAIClient` and `AsAIAgent()` |
| 03 | `03-anti-pattern-without-session` | Why multi-turn without `AgentSession` fails |
| 04 | `04-proper-session-multiturn` | Correct multi-turn usage with `AgentSession` |
| 05 | `05-structured-output` | Typed/structured responses |
| 06 | `06-reasoning-effort` | Reasoning-effort controls |
| 07 | `07-middleware-usage` | Middleware and interception |
| 08 | `08-agent-with-tools` | Tool/function calling |
| 09 | `09-agent-framework-skills` | Inline skills |
| 10 | `10-csharp-file-script-runner` | File-based skills with C# scripts |

## Suggested Order

1. [01-simple-agent/README.md](01-simple-agent/README.md)
2. [02-simple-agent-openai/README.md](02-simple-agent-openai/README.md)
3. Continue in numeric order through 10

## Before You Start

- .NET SDK 10
- Valid model credentials configured via environment variables used by each sample

## How to Run a Sample

```bash
cd <sample-folder>/<project-folder>
dotnet run
```

Example:

```bash
cd 04-proper-session-multiturn/04-proper-session-multiturn
dotnet run
```

## Quick Notes

- Folder names and links are aligned so `01-simple-agent` is the baseline sample and `02-simple-agent-openai` is the OpenAI variant.



