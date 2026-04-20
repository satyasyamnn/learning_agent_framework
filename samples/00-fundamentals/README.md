# 🎓 Agent Framework Fundamentals

**Fundamentals (00)** –  Learning about building agents using **Microsoft Agent Framework** in .NET 10 / C# 13. This folder contains **9  sample projects** that helps building agents

---

## 📚 Learning Path Overview

All projects are in the `/samples/00-fundamentals/` folder. Start from **Project 1** and progress sequentially to build deep understanding.

| # | Project | Concept |
|---|---------|---------|
| **1** | [00-simple-agent](#-project-1-simple-agent) | 🤖 Basic Agent Creation |
| **2** | [01-agent-with-tools](#-project-2-agent-with-tools) | 🔧 Agent + Tools |
| **3** | [02-anti-pattern-without-session](#-project-3-anti-pattern-without-session) | ⚠️ Session Anti-Pattern |
| **4** | [03-proper-session-multiturn](#-project-4-proper-session-multiturn) | 💾 Multi-Turn Sessions |
| **5** | [04-structured-output](#-project-5-structured-output) | 📋 Structured Output |
| **6** | [05-reasoning-effort](#-project-6-reasoning-effort) | 🧠 Reasoning Controls |
| **7** | [06-middleware-usage](#-project-7-middleware-usage) | 🛡️ Middleware & Monitoring |
| **8** | [07-agent-framework-skills](#-project-8-agent-framework-skills) | 🧰 Skills (Inline) |
| **9** | [08-csharp-file-script-runner](#-project-9-csharp-file-script-runner) | 🧾 Skills (File-Based) |

---

## 🚀 Quick Start

### 1. Prerequisites
- .NET 10 SDK
- Azure OpenAI resource (or use DefaultAzureCredential with managed identity)
- Azure CLI (for authentication)

### 2. Get Running in 2 Minutes
```bash
# Navigate to first project
cd samples/00-fundamentals/00-simple-agent/00-simple-agent

# Configure (if needed)
cp ../../../shared/appsettings/appsettings.json .

# Run
dotnet run
```

### 3. Explore All Projects
```bash
# Try any project
cd samples/00-fundamentals/{project-name}/{project-name}
dotnet run
```

---

## 📖 Project Details

### 🤖 Project 1: Simple Agent

**Location:** `00-simple-agent/`  
**Focus:** Minimal setup, single-turn interactions  
**Time:** 10 minutes  
**Difficulty:** ⭐ Beginner

👉 [Full README](00-simple-agent/README.md)

**What You'll Learn:**
- Create an agent from a ChatClient
- Add instructions to guide behavior
- Execute single-turn interactions
- Handle streaming responses

**Key Code:**
```csharp
AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful supply chain assistant.",
    name: "BasicAgent");

AgentResponse response = await agent.RunAsync("Your question here");
Console.WriteLine(response.Text);
```

**When to use:** Quick one-off questions, no conversation history needed

---

### 🔧 Project 2: Agent with Tools

**Location:** `01-agent-with-tools/`  
**Focus:** Function calling, tool registration, external data  
**Time:** 15 minutes  
**Difficulty:** ⭐⭐ Beginner

👉 [Full README](01-agent-with-tools/README.md)

**What You'll Learn:**
- Register tools using `AIFunctionFactory`
- Enable function calling
- Combine multiple tool classes
- Handle tool results

**Key Code:**
```csharp
List<AITool> tools = methods
    .Select(m => AIFunctionFactory.Create(m, instance))
    .Cast<AITool>()
    .ToList();

AIAgent agent = chatClient
    .AsAIAgent(instructions: "...", tools: tools)
    .AsBuilder()
    .Use(ToolCallingMiddleware)
    .Build();
```

**When to use:** Agent needs real-time data, calculations, or external integrations

---

### ⚠️ Project 3: Anti-Pattern Without Session

**Location:** `02-anti-pattern-without-session/`  
**Focus:** Understand why sessions are critical  
**Time:** 10 minutes  
**Difficulty:** ⭐⭐ Beginner

👉 [Full README](02-anti-pattern-without-session/README.md)

**What You'll Learn:**
- Why stateless interactions lose context
- The problem with calling `RunAsync()` repeatedly without sessions
- How agents lose memory between turns
- Why this pattern fails in production

**Key Code (What NOT to Do):**
```csharp
// ❌ ANTI-PATTERN: No session
AgentResponse r1 = await agent.RunAsync("My name is Alice.");
AgentResponse r2 = await agent.RunAsync("What's my name?");  // Agent won't know!
```

**When to use:** Educational example showing common mistake

---

### 💾 Project 4: Proper Multi-Turn with AgentSession

**Location:** `03-proper-session-multiturn/`  
**Focus:** Conversation history, context retention, token tracking  
**Time:** 20 minutes  
**Difficulty:** ⭐⭐ Beginner

👉 [Full README](03-proper-session-multiturn/README.md)

**What You'll Learn:**
- Create and manage `AgentSession`
- Execute multi-turn conversations with full context
- Track token usage per turn
- Serialize sessions for persistence
- Stream responses with session state

**Key Code (The CORRECT Way):**
```csharp
// ✅ CORRECT: Using AgentSession
AgentSession session = await agent.CreateSessionAsync();

AgentResponse r1 = await agent.RunAsync("My name is Alice.", session);
AgentResponse r2 = await agent.RunAsync("What's my name?", session);  // ✅ Remembers!

// Token tracking
r1.WriteTokenUsageToConsole("Turn 1");
// Output: ✓ Turn 1 | Input: 54 tokens | Output: 68 tokens | Total: 122 tokens
```

**When to use:** Multi-turn conversations, chatbots, dialogue systems

---

### 📋 Project 5: Structured Output

**Location:** `04-structured-output/`  
**Focus:** Type-safe responses, JSON schemas, deserialization  
**Time:** 15 minutes  
**Difficulty:** ⭐⭐⭐ Intermediate

👉 [Full README](04-structured-output/README.md)

**What You'll Learn:**
- Define response schemas as C# classes
- Use `ChatResponseFormat.ForJsonSchema<T>()`
- Automatic deserialization with `RunAsync<T>()`
- Get compile-time type safety
- Stream structured responses

**Key Code:**
```csharp
// Define response type
public class CustomsClearanceAssessment
{
    public string ShipmentId { get; set; }
    public string RiskLevel { get; set; }
    public decimal EstimatedDutyUsd { get; set; }
}

// Get typed response
AgentResponse<CustomsClearanceAssessment> response = 
    await agent.RunAsync<CustomsClearanceAssessment>("Assess shipment...");

// Direct access to typed object
var riskLevel = response.Output.RiskLevel;  // ✅ Type-safe!
```

**When to use:** API responses, data processing, machine-readable output

---

### 🧠 Project 6: Reasoning Effort Controls

**Location:** `05-reasoning-effort/`  
**Focus:** Tuning reasoning depth, cost optimization, quality control  
**Time:** 15 minutes  
**Difficulty:** ⭐⭐⭐ Intermediate

👉 [Full README](05-reasoning-effort/README.md)

**What You'll Learn:**
- Use baseline (default) reasoning
- Enable minimal reasoning for speed
- Use high reasoning for complex analysis
- Monitor reasoning tokens and cost
- Choose appropriate levels for tasks

**Key Code:**
```csharp
// Minimal reasoning (fast, cheap)
var agent1 = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    ChatOptions = new ChatOptions
    {
        RawRepresentationFactory = _ => new ChatCompletionOptions
        {
            ReasoningEffortLevel = ChatReasoningEffortLevel.Minimal
        }
    }
});

// High reasoning (deep thinking, expensive)
var agent2 = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    ChatOptions = new ChatOptions
    {
        RawRepresentationFactory = _ => new ChatCompletionOptions
        {
            ReasoningEffortLevel = ChatReasoningEffortLevel.High
        }
    }
});
```

**Comparison:**
| Level | Speed | Cost | Use For |
|-------|-------|------|---------|
| Minimal | ⚡ Fast | 💰 Low | Quick Q&A |
| Baseline | ⚡⚡ Medium | 💰💰 Medium | Standard tasks |
| High | 🐢 Slow | 💰💰💰 High | Complex analysis |

**When to use:** Complex workflows needing strategic decisions

---

### 🛡️ Project 7: Middleware Usage

**Location:** `06-middleware-usage/`  
**Focus:** Request/response interception, logging, validation, monitoring  
**Time:** 20 minutes  
**Difficulty:** ⭐⭐⭐ Intermediate

👉 [Full README](06-middleware-usage/README.md)

**What You'll Learn:**
- Implement chat client middleware
- Implement function calling middleware
- Stack multiple middleware layers
- Log operations without changing core code
- Validate inputs and outputs
- Monitor performance

**Key Code:**
```csharp
// Define middleware
async Task<ChatResponse> LoggingMiddleware(
    ChatMessage[] messages,
    ChatOptions options,
    Func<ChatMessage[], ChatOptions, Task<ChatResponse>> next)
{
    var stopwatch = Stopwatch.StartNew();
    Console.WriteLine($"[Request] {messages.Last().Content}");
    
    var response = await next(messages, options);  // Call next layer
    
    stopwatch.Stop();
    Console.WriteLine($"[Response] {response.Message.Content}");
    Console.WriteLine($"[Latency] {stopwatch.ElapsedMilliseconds}ms");
    
    return response;
}

// Apply middleware
var agent = chatClient
    .AsIChatClient()
    .AsBuilder()
    .Use(LoggingMiddleware)
    .BuildAIAgent(...);
```

**When to use:** Monitoring, audit logging, security validation, rate limiting

---

### 🧰 Project 8: Agent Framework Skills

**Location:** `07-agent-framework-skills/`  
**Focus:** Inline skills, resources, scripts, modular knowledge  
**Time:** 20 minutes  
**Difficulty:** ⭐⭐⭐ Intermediate

👉 [Full README](07-agent-framework-skills/README.md)

**What You'll Learn:**
- Create inline skills with fluent API
- Add resources (reference materials)
- Add scripts (executable functions)
- Use skills with agents
- Combine multiple skills

**Key Code:**
```csharp
var clearanceSkill = new AgentInlineSkill(
    name: "customs-clearance-packet",
    description: "Guide customs clearance review",
    instructions: "Use this skill for document and duty questions")
    .AddResource(
        "required-documents",
        "Commercial invoice, packing list, bill of lading...",
        "Reference: Required documents checklist")
    .AddScript(
        "estimate-duty",
        (decimal value, decimal rate) => value * (rate / 100m),
        "Calculate estimated duty");

var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    AIContextProviders = [new AgentSkillsProvider([clearanceSkill])]
});
```

**When to use:** Complex domain workflows, reference materials, calculations

---

### 🧾 Project 9: C# File-Based Skill Scripts

**Location:** `08-csharp-file-script-runner/`  
**Focus:** File-based skills, dynamic script loading, C# scripting  
**Time:** 20 minutes  
**Difficulty:** ⭐⭐⭐ Intermediate

👉 [Full README](08-csharp-file-script-runner/README.md)

**What You'll Learn:**
- Load skills from `SKILL.md` files
- Execute C# `.csx` scripts dynamically
- Structure skill files on disk
- Run skills without Python dependency
- Use Roslyn for script compilation

**File Structure:**
```
skills/
├── csharp-duty-and-lane/
│   ├── SKILL.md           # Skill metadata
│   └── estimate-duty.csx  # Executable script
```

**SKILL.md Format:**
```markdown
# Customs Duty Skill
## Description
Estimate customs duty...
## Scripts
### estimate-duty
- Calculates duty from value and rate
```

**C# Script (.csx):**
```csharp
var declaredValue = Parameters.declaredValue;
var dutyRate = Parameters.dutyRate;
var estimatedDuty = declaredValue * (dutyRate / 100m);
return JsonSerializer.Serialize(new { estimatedDuty });
```

**When to use:** Maintainable workflows, non-Python environments, dynamic scripts

---

## 🎯 Recommended Learning Paths

### Path 1: Beginner to Core (1-2 Hours)
```
Start → Project 1 (Simple) 
      → Project 2 (Tools) 
      → Project 3 (Anti-Pattern Warning)
      → Project 4 (Sessions - CORE)
```
✅ After this, you can build basic multi-turn agents with tools.

### Path 2: Add Advanced Features (Next 1-2 Hours)
```
Path 1 Completed
      → Project 5 (Structured Output)
      → Project 6 (Reasoning)
      → Project 7 (Middleware)
```
✅ Add type-safety, cost optimization, and monitoring.

### Path 3: Complex Workflows (Next 1-2 Hours)
```
Path 2 Completed
      → Project 8 (Inline Skills)
      → Project 9 (File-Based Skills)
```
✅ Build maintainable, modular domain workflows.

---

## 📊 Architecture Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    01 - Simple Agent (Baseline)                 │
└─────────────────┬───────────────────────────────────────────────┘
                  │
     ┌────────────┴────────────┐
     ▼                         ▼
┌──────────────┐        ┌──────────────┐
│ 02 - Tools   │        │ 03 - Session │ (Anti-Pattern)
└──────────────┘        └──────────────┘
     │                         │
     └────────────┬────────────┘
                  ▼
         ┌──────────────────────┐
         │ 04 - Multi-Turn Sess │ ⭐ CORE PATTERN
         └────────┬─────────────┘
                  │
     ┌────────────┼────────────┐
     ▼            ▼            ▼
┌─────────┐ ┌──────────┐ ┌────────────┐
│ Struct  │ │ Reasoning│ │ Middleware │
│ Output  │ │ Effort   │ │ Monitoring │
└─────────┘ └──────────┘ └────────────┘
     │            │            │
     └────────────┼────────────┘
                  ▼
         ┌──────────────────────┐
         │ Skills (Inline/File) │ - Advanced Workflows
         └──────────────────────┘
```

---

## 🔑 Key Concepts Summary

| Concept | Project | Key Takeaway |
|---------|---------|--------------|
| **Agents** | 1, 2 | Wrap ChatClient for AI features |
| **Tools** | 2, 7 | Enable function calling |
| **Sessions** | 3, 4 | Maintain conversation history |
| **Structured Output** | 5 | Get typed, reliable responses |
| **Reasoning Effort** | 6 | Balance cost, latency, quality |
| **Middleware** | 7 | Intercept requests/responses |
| **Skills** | 8, 9 | Encapsulate domain knowledge |

---

## 📁 Repository Structure

```
learning_agent_framework/
├── samples/
│   └── 00-fundamentals/           ← You are here
│       ├── README.md              ← Master README (this file)
│       ├── README.original.md     ← Original README (preserved)
│       ├── 00-simple-agent/
│       │   ├── README.md          ← Project README
│       │   └── 00-simple-agent/
│       ├── 01-agent-with-tools/
│       │   ├── README.md
│       │   └── 01-agent-with-tools/
│       ├── 02-anti-pattern-without-session/
│       │   ├── README.md
│       │   └── ...
│       ├── 03-proper-session-multiturn/
│       │   ├── README.md
│       │   └── ...
│       ├── 04-structured-output/
│       │   ├── README.md
│       │   └── ...
│       ├── 05-reasoning-effort/
│       │   ├── README.md
│       │   └── ...
│       ├── 06-middleware-usage/
│       │   ├── README.md
│       │   └── ...
│       ├── 07-agent-framework-skills/
│       │   ├── README.md
│       │   └── ...
│       ├── 08-csharp-file-script-runner/
│       │   ├── README.md
│       │   └── ...
│       └── Shared/
│           └── TokenUsageConsoleExtensions.cs
├── shared/
│   └── appsettings/
│       └── appsettings.json       ← Share config across projects
├── README.md                       ← Main repo README
└── SupplyChainCustoms.AgentFramework.slnx  ← Solution file
```

---

## 🛠️ Configuration Setup

### Option 1: Use appsettings.json
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<resource>.openai.azure.com/",
    "DeploymentName": "gpt-4o-mini",
    "ApiKey": "your-key-here-or-leave-empty"
  }
}
```

### Option 2: Managed Identity (No API Key)
Leave `ApiKey` empty. The code will use:
```csharp
new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
```

Requires: `az login` and appropriate Azure RBAC roles.

---

## 📚 NuGet Packages Used

All projects use these core packages:

```xml
<PackageReference Include="Microsoft.Agents.AI" Version="1.1.0" />
<PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.1.0" />
<PackageReference Include="Microsoft.Agents.AI.Workflows" Version="1.1.0" />
<PackageReference Include="Azure.AI.OpenAI" Version="2.1.0" />
<PackageReference Include="Microsoft.Extensions.AI" Version="10.4.0" />
```

---

## 🤝 Common Patterns

### Pattern 1: Basic Agent Loop
```csharp
AIAgent agent = chatClient.AsAIAgent("Your instructions");
while (true)
{
    var response = await agent.RunAsync(Console.ReadLine());
    Console.WriteLine(response.Text);
}
```

### Pattern 2: Multi-Turn with Session
```csharp
AgentSession session = await agent.CreateSessionAsync();
while (true)
{
    var response = await agent.RunAsync(input, session);
    Console.WriteLine(response.Text);
}
```

### Pattern 3: Tools + Session + Middleware
```csharp
var agent = chatClient
    .AsIChatClient()
    .AsBuilder()
    .Use(LoggingMiddleware)
    .BuildAIAgent(tools: customTools);

var session = await agent.CreateSessionAsync();
// ... use with session
```

---

## ✅ Troubleshooting

### Q: "AzureOpenAI:Endpoint not configured"
**A:** Check `appsettings.json` exists and has valid Endpoint value.

### Q: "Authentication failed"
**A:** Either set `ApiKey` in appsettings.json or run `az login`.

### Q: "Model deployment not found"
**A:** Check `AzureOpenAI:DeploymentName` matches your Azure OpenAI deployment.

### Q: "Token limit exceeded"
**A:** Session history is too long. Trim old messages or create new session.

---

## 📖 Next Resources

- **Microsoft Agents Framework:** https://github.com/microsoft/agents
- **Azure OpenAI:** https://learn.microsoft.com/azure/ai-services/openai/
- **Semantic Kernel:** https://github.com/microsoft/semantic-kernel

---

## 🎓 Completion Checklist

After working through all projects:

- [ ] Understand basic agent creation and single-turn interactions
- [ ] Know how to register and call tools
- [ ] Can identify why sessions are critical for multi-turn
- [ ] Build context-aware, stateful conversations
- [ ] Get type-safe structured output from agents
- [ ] Optimize cost with reasoning effort controls
- [ ] Implement logging and monitoring with middleware
- [ ] Create modular workflows with skills
- [ ] Load and execute file-based scripts

---

## 📝 License & Attribution

These samples are part of the Microsoft Agent Framework learning initiative.  
Original README preserved as `README.original.md`.

---

## 🚀 Ready to Begin?

👉 **Start with Project 1:** [Simple Agent](00-simple-agent/README.md)

Each project has a detailed README with:
- Core concepts explained
- Code snippets
- Best practices
- Common patterns
- Next steps

Happy learning! 🎉

