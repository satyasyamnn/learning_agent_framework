# Microsoft Agent Framework — Supply Chain & Customs Samples

A knowledge sharing collection for building Agents using **Microsoft Agent Framework** (`Microsoft.Agents.AI` v1.1.0) in **.NET 10 / C# 13**, showcasing the full spectrum from single-agent tool use to complex multi-agent orchestration workflows across two real-world domains: **Supply Chain** and **Customs Clearance**.

---

## Fundamentals (00) — Agent Framework Basics

**Fundamentals** sharing knowledge about the Microsoft Agent Framework. These examples demonstrate concepts from foundational patterns to advanced techniques, offering context before exploring the domain-specific samples.

| Sample | Project | Concept | Key Feature |
| ------- | ------- | --------- | ------------ |
| 01 | `00-simple-agent` | 🤖 Basic Agent Creation | Single-turn interactions, no tools |
| 02 | `01-agent-with-tools` | 🔧 Agent + Tools | Function calling, tool registration |
| 03 | `02-anti-pattern-without-session` | ⚠️ Session Anti-Pattern | Why sessions are needed (educational) |
| 04 | `03-proper-session-multiturn` | 💾 Token Management in Sessions | Context persistence, token tracking per turn |
| 05 | `04-structured-output-customs` | 📋 Structured Output | JSON schema output with customs assessment |
| 06 | `05-reasoning-effort-customs` | 🧠 Reasoning Effort Controls | Baseline vs minimal vs high reasoning effort |

---

## NuGet Packages

| Package | Version | Used for |
| ------- | ------- | ---------- |
| `Microsoft.Agents.AI` | 1.1.0 | `AIAgent`, `AgentSession`, `AIFunctionFactory` |
| `Microsoft.Agents.AI.OpenAI` | 1.1.0 | `AsAIAgent()` extension on `ChatClient` |
| `Microsoft.Agents.AI.Workflows` | 1.1.0 | `Executor`, `WorkflowBuilder`, `InProcessExecution`, `IWorkflowContext` |
| `Microsoft.Agents.AI.Workflows.Generators` | 1.1.0 | Source generator for `[MessageHandler]` — **required** in all workflow projects |
| `Azure.AI.OpenAI` | 2.1.0 | `AzureOpenAIClient` |
| `Microsoft.Extensions.AI` | 10.4.0 | Shared chat abstractions and chat message types |
| `Microsoft.Extensions.Configuration.Json` | 9.0.4 | `appsettings.json` loading |

> **Important:** `Microsoft.Agents.AI.Workflows.Generators` must be referenced in every project that uses `[MessageHandler]`. Without it, the source generator does not run and `Executor` subclasses will fail to compile (`CS0534`).

---

## Framework Concepts — Overview Map

```mermaid
flowchart LR
    C1["🔧 Single Agent<br/>+ Tools"]
    C2["💾 Multi-Turn<br/>Session Memory"]
    C3["🔀 Multi-Agent<br/>Workflow"]
    C4["🌍 Domain-Specific<br/>Customs"]
    C5["🛂 Conditional<br/>Routing"]
    C6["📦 End-to-End<br/>Pipeline"]

    C1 --> C2 --> C3
    C3 -.-> C4 --> C5 --> C6

    style C1 fill:#bbdefb,stroke:#1565c0,stroke-width:2px,color:#000
    style C2 fill:#c8e6c9,stroke:#388e3c,stroke-width:2px,color:#000
    style C3 fill:#ffe0b2,stroke:#f57c00,stroke-width:2px,color:#000
    style C4 fill:#f8bbd0,stroke:#c2185b,stroke-width:2px,color:#000
    style C5 fill:#e1bee7,stroke:#6a1b9a,stroke-width:2px,color:#000
    style C6 fill:#b2dfdb,stroke:#00695c,stroke-width:2px,color:#000
```



### Fundamentals 01: Simple Agent (🤖 Basic Agent Creation)

**Pattern:** Basic agent creation and single-turn interactions

| Detail | Value |
| ------- | ------- |
| Project | `00-simple-agent` |
| Agent | `SimpleAgent` |
| Key API | `AsAIAgent()`, `RunAsync()` |
| Tools | None |

The most basic agent setup — create an agent with instructions and have a single conversation turn. Demonstrates the minimal code needed to get an agent responding to queries.

```csharp
var agent = azureOpenAI.GetChatClient(deployment)
    .AsAIAgent(instructions: "You are a helpful assistant.", name: "SimpleAgent");

var response = await agent.RunAsync("Hello, who are you?");
Console.WriteLine(response.Text);
```

---

### Fundamentals 02: Agent with Tools (🔧 Agent + Tools)

**Pattern:** Agent with function calling capabilities

| Detail | Value |
| ------- | ------- |
| Project | `01-agent-with-tools` |
| Agent | `ToolAgent` |
| Key API | `AIFunctionFactory.Create()`, `AsAIAgent(tools: ...)` |
| Tools | `GetWeather`, `ConvertTemperature`, `GetPopulation` |

Shows how to register tools with an agent, enabling function calling. The agent can call these tools to get real-time data or perform calculations before responding.

```csharp
var tools = new[]
{
    AIFunctionFactory.Create(GetWeather),
    AIFunctionFactory.Create(ConvertTemperature),
    AIFunctionFactory.Create(GetPopulation)
};

var agent = azureOpenAI.GetChatClient(deployment)
    .AsAIAgent(instructions: "...", name: "ToolAgent", tools: tools);

var response = await agent.RunAsync("What's the weather in Tokyo and its population?");
Console.WriteLine(response.Text);
```

---

### Fundamentals 03: Anti-Pattern Without Session (⚠️ Session Anti-Pattern)

**Pattern:** Demonstrates why sessions are needed and the consequences of stateless design

| Detail | Value |
| ------- | ------- |
| Project | `02-anti-pattern-without-session` |
| Agent | `StatelessAgent` |
| Key API | Multiple `RunAsync()` calls without session |
| Tools | None |

**Pattern demonstration** showing what happens when you try multi-turn conversations without session memory. Each call to `RunAsync()` is completely independent, so the agent has no memory of previous turns. This illustrates the importance of proper session management.

```csharp
var agent = azureOpenAI.GetChatClient(deployment)
    .AsAIAgent(instructions: "...", name: "StatelessAgent");

// Turn 1
var response1 = await agent.RunAsync("My name is Alice");
Console.WriteLine(response1.Text); // Agent acknowledges

// Turn 2 — Agent has no memory of Turn 1!
var response2 = await agent.RunAsync("What's my name?");
Console.WriteLine(response2.Text); // Agent doesn't know!
```

---

### Fundamentals 04: Proper Session Multi-Turn (💾 Token Management in Sessions)

**Pattern:** Correct multi-turn conversations with session persistence and token usage tracking

| Detail | Value |
| ------- | ------- |
| Project | `03-proper-session-multiturn` |
| Agent | `SessionAgent` |
| Key API | `CreateSessionAsync()`, `RunAsync(session)`, `SerializeSessionAsync()`, `TokenUsage` |
| Tools | None |

Demonstrates proper multi-turn conversations using `AgentSession` for context persistence with token usage tracking. The agent remembers information across turns, tracks cumulative token consumption (input/output/cache tokens), and can be serialized/deserialized for persistence. Each turn reports token metrics to understand API costs and quota management.

```csharp
var agent = azureOpenAI.GetChatClient(deployment)
    .AsAIAgent(instructions: "...", name: "SessionAgent");

var session = await agent.CreateSessionAsync();

// Turn 1
var response1 = await agent.RunAsync("My name is Alice", session);
Console.WriteLine(response1.Text);
Console.WriteLine($"Turn 1 — Input: {response1.InputTokenCount}, Output: {response1.OutputTokenCount}");

// Turn 2 — Agent remembers and tracks tokens!
var response2 = await agent.RunAsync("What's my name?", session);
Console.WriteLine(response2.Text); // Agent correctly says "Alice"
Console.WriteLine($"Turn 2 — Input: {response2.InputTokenCount}, Output: {response2.OutputTokenCount}");

// Serialize session for persistence
var json = await agent.SerializeSessionAsync(session);
```

---

### Fundamentals 05: Structured Output for Customs (📋 Structured Output)

**Pattern:** Structured JSON output via response schema, typed responses, and streaming assembly

| Detail | Value |
| ------- | ------- |
| Project | `04-structured-output-customs` |
| Agent | `CustomsStructuredOutputAgent` |
| Key API | `ChatResponseFormat.ForJsonSchema<T>()`, `RunAsync<T>()`, `RunStreamingAsync()` |
| Output Type | `CustomsClearanceAssessment` |

Shows how to constrain the model output to a strongly typed customs clearance schema. The sample demonstrates three patterns: response-format JSON text, generic typed output, and streaming output reassembly plus deserialization.

```csharp
var response = await agent.RunAsync<CustomsClearanceAssessment>(
    "Assess shipment CSH-3017 to Germany with HS code 854231 and duty rate 4.2%.");

Console.WriteLine(response.Result.RiskLevel);
Console.WriteLine(response.Result.EstimatedDutyUsd);
```

---

### Fundamentals 06: Reasoning Effort Controls (🧠 Reasoning Effort Controls)

**Pattern:** Reasoning effort configuration to optimize cost, latency, and quality trade-offs

| Detail | Value |
| ------- | ------- |
| Project | `05-reasoning-effort-customs` |
| Agent | `ReasoningEffortAgent` |
| Key API | `ChatClientAgentOptions`, `MaxCompletionTokens`, Reasoning effort levels |
| Modes | `Baseline`, `Minimal`, `High` |

Demonstrates how to switch reasoning effort levels (when using reasoning models) to balance complexity, cost, and latency. Lower effort uses faster heuristics; higher effort uses extended thinking for complex problems. This sample shows three configurations on the same customs assessment task, comparing token usage and reasoning depth.

```csharp
// Baseline reasoning (default, balanced)
var baselineOptions = new ChatClientAgentOptions { };
var baselineAgent = chatClient.AsAIAgent(instructions: "...", new ChatClientAgentOptions { });
var baselineResponse = await baselineAgent.RunAsync<CustomsAssessment>(prompt);
Console.WriteLine($"Baseline - Tokens: {baselineResponse.OutputTokenCount}");

// Minimal reasoning (faster, lower cost)
var minimalOptions = new ChatClientAgentOptions 
{ 
    MaxCompletionTokens = 1000 
};
var minimalAgent = chatClient.AsAIAgent(instructions: "...", minimalOptions);
var minimalResponse = await minimalAgent.RunAsync<CustomsAssessment>(prompt);
Console.WriteLine($"Minimal - Tokens: {minimalResponse.OutputTokenCount}");

// High reasoning (extended thinking, higher accuracy for complex decisions)
var highOptions = new ChatClientAgentOptions 
{ 
    MaxCompletionTokens = 16000 
};
var highAgent = chatClient.AsAIAgent(instructions: "...", highOptions);
var highResponse = await highAgent.RunAsync<CustomsAssessment>(prompt);
Console.WriteLine($"High - Tokens: {highResponse.OutputTokenCount}");

// Compare results and costs
Console.WriteLine($"Baseline risk score: {baselineResponse.Result.RiskScore}");
Console.WriteLine($"High reasoning risk score: {highResponse.Result.RiskScore}");
```

---

## RAG (01) — Retrieval-Augmented Generation

**RAG updates introduced in this repository**

| Sample | Project | Concept | Key Feature |
| ------- | ------- | --------- | ------------ |
| 01 | `00-customs-rag-basic` | 📚 Tool-Based RAG | Local knowledge retrieval with grounding citations (`[KB-xxx]`) |
| 02 | `01-customs-rag-embeddings` | 🧭 Embedding-Based RAG | Semantic vector retrieval using in-memory vector store |

### What Changed

- Added a **basic customs RAG** sample using a retrieval tool (`RetrieveCustomsKnowledge`) and token-overlap ranking.
- Added an **embedding-based customs RAG** sample using Azure OpenAI embeddings and in-memory vector search.
- Added support for configurable embedding settings via `AzureOpenAI:EmbeddingEndpoint`, `AzureOpenAI:EmbeddingDeploymentName`, and `AzureOpenAI:EmbeddingApiKey`.

### RAG-Specific Packages

| Package | Version | Used for |
| ------- | ------- | ---------- |
| `Microsoft.Extensions.AI.OpenAI` | 10.4.0 | OpenAI/Azure OpenAI extension helpers used by RAG samples |
| `Microsoft.Extensions.VectorData.Abstractions` | 10.1.0 | Vector data contracts used in embedding-based RAG |
| `Microsoft.SemanticKernel.Connectors.InMemory` | 1.74.0-preview | In-memory vector store for semantic retrieval |

### Sample 01: Customs RAG Basic (📚 Agent + Retrieval Tool)

**Pattern:** Basic Retrieval-Augmented Generation using a local customs knowledge base and a retrieval tool

| Detail | Value |
| ------- | ------- |
| Project | `00-customs-rag-basic` |
| Agent | `CustomsRagAgent` |
| Key API | `AIFunctionFactory.Create(RetrieveCustomsKnowledge)` |
| Behavior | Retrieves top customs snippets and answers with grounding citations like `[KB-001]` |

This sample demonstrates a lightweight RAG pattern in Microsoft Agent Framework without external vector infrastructure. It uses a local knowledge corpus, token-overlap ranking, and a tool call to provide grounded answers for customs questions on documents, HS code classification, duty basics, sanctions, and dual-use checks.

**Solution approach:** The implementation keeps retrieval intentionally simple. It builds a small in-memory customs knowledge base, ranks candidate snippets with token overlap, and exposes retrieval through a tool the agent can call before answering.

The flow is: user question -> retrieval tool selects top snippets -> agent answers using only grounded context and returns citation-style references such as `[KB-001]`. This makes the example easy to understand without introducing embeddings or external search infrastructure.

### Sample 02: Customs RAG with Embeddings (🧭 Semantic Vector Retrieval)

**Pattern:** Embedding-based Retrieval-Augmented Generation using semantic vector search

| Detail | Value |
| ------- | ------- |
| Project | `01-customs-rag-embeddings` |
| Agent | `CustomsEmbeddingRagAgent` |
| Key API | `GetEmbeddingClient(...).AsIEmbeddingGenerator()`, `InMemoryVectorStore`, `VectorSearchAsync(...)` |
| Behavior | Retrieves semantically similar customs snippets using embeddings before answering |

This sample demonstrates semantic RAG with vector retrieval. It builds embeddings for customs knowledge records, stores them in an in-memory vector index, and retrieves top semantic matches for grounded responses.

**Solution approach:** This version extends the same customs domain with semantic retrieval. It generates embeddings for each knowledge record, stores them in an in-memory vector store, and performs similarity search so retrieval works even when the user wording does not exactly match the stored text.

The flow is: build embeddings -> upsert records into the vector index -> run vector search for the user query -> pass the matched snippets into the agent for grounded answering. This sample shows the shape of a semantic RAG pipeline while still keeping the infrastructure local and inspectable.

```csharp
var embeddingGenerator = embeddingClient
    .GetEmbeddingClient(embeddingDeployment)
    .AsIEmbeddingGenerator();

var vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions
{
    EmbeddingGenerator = embeddingGenerator
});

var collection = vectorStore.GetCollection<Guid, CustomsVectorStoreRecord>("customs-knowledge");
```

---

## Architecture Overview

If you are viewing this in VS Code and the diagram is blank or not rendered, install the `Markdown Preview Mermaid Support` extension (`bierner.markdown-mermaid`) and use `Markdown: Open Preview to the Side`.

```mermaid
graph LR
    Libraries["Shared Libraries"]:::library
    AI["Azure OpenAI"]:::ai
    SC["Supply Chain"]:::section
    CU["Customs"]:::section

    OT[OrderTracking]:::supplychain
    DA[DisruptionAlert]:::supplychain
    SO[SupplierOrchestration]:::supplychain

    DR[DocumentReview]:::customs
    CC[ComplianceCheck]:::customs
    CO[ClearanceOrchestration]:::customs

    Libraries --> SC
    Libraries --> CU
    AI -.-> SC
    AI -.-> CU

    SC --> OT
    SC --> DA
    SC --> SO
    CU --> DR
    CU --> CC
    CU --> CO

    classDef library fill:#e3f2fd,stroke:#1e88e5,stroke-width:2px,color:#0d47a1,font-size:14px,font-weight:700
    classDef section fill:#f5f5f5,stroke:#424242,stroke-width:1px,color:#212121,font-size:13px,font-weight:700
    classDef supplychain fill:#fce4ec,stroke:#d81b60,stroke-width:2px,color:#880e4f,font-size:13px
    classDef customs fill:#e8f5e9,stroke:#2e7d32,stroke-width:2px,color:#1b5e20,font-size:13px
    classDef ai fill:#fff8e1,stroke:#f9a825,stroke-width:2px,color:#bf360c,font-size:14px,font-weight:700
```

---

## Samples

**Quick Reference — Which Concept Does Each Sample Demonstrate?**

| Sample | Project | Concept | Key Feature |
| ------- | ------- | --------- | ------------ |
| 1 | `SupplyChain.OrderTracking` | 🔧 Single Agent + Tools | Streaming API calls |
| 2 | `SupplyChain.DisruptionAlert` | 💾 Multi-Turn Session Memory | Context persistence across turns |
| 3 | `SupplyChain.SupplierOrchestration` | 🔀 Multi-Agent Workflow | Executor orchestration + human gate |
| 4 | `Customs.DocumentReview` | 🌍 Single Agent + Domain Tools | Structured review workflow |
| 5 | `Customs.ComplianceCheck` | 🛢️ Conditional Routing + Human Gate | Dynamic routing based on risk score |
| 6 | `Customs.ClearanceOrchestration` | 📦 End-to-End Pipeline | 6-stage pipeline with tracing |


## Solution Structure

```text
learning_agent_framework/
├── SupplyChainCustoms.AgentFramework.slnx
│
├── shared/
│   ├── SharedModels/
│   │   └── SharedModels/
│   │       ├── SupplyChain/
│   │       │   ├── Shipment.cs       — Shipment, ShipmentEvent, ShipmentStatus
│   │       │   ├── Supplier.cs       — Supplier, SupplierDisruption, DisruptionSeverity
│   │       │   └── Order.cs          — Order, OrderLine, OrderStatus
│   │       └── Customs/
│   │           ├── Shipment.cs       — CustomsShipment, CustomsLine
│   │           ├── TradeDocument.cs  — TradeDocument, DocumentField, DocumentType
│   │           └── ComplianceResult.cs — ComplianceResult, FlagSeverity
│   │
│   └── MockDataServices/
│       └── MockDataServices/
│           ├── SupplyChain/
│           │   ├── MockShipmentService.cs  — TRK-001…005 (1 delayed, 1 in-transit)
│           │   ├── MockSupplierService.cs  — SUP-001…005 (SUP-002 disrupted: factory fire)
│           │   └── MockOrderService.cs     — 4 open orders
│           └── Customs/
│               ├── MockCustomsShipmentService.cs  — CSH-3001…3004 (3004: sanctioned)
│               ├── MockDocumentService.cs          — docs with intentional missing fields
│               └── MockTariffService.cs            — HS codes, duty rates, sanctioned countries
│
└── samples/
    ├── 02-supply-chain/
    │   ├── SupplyChain.OrderTracking/
    │   ├── SupplyChain.DisruptionAlert/
    │   └── SupplyChain.SupplierOrchestration/
    │       ├── Program.cs    — workflow wiring
    │       └── Executors.cs  — 3 executor classes
    │
    └── 03-customs/
        ├── Customs.DocumentReview/
        ├── Customs.ComplianceCheck/
        │   ├── Program.cs    — workflow + conditional edges
        │   └── Executors.cs  — 5 executor classes
        └── Customs.ClearanceOrchestration/
            ├── Program.cs    — 6-stage linear pipeline
            └── Executors.cs  — 6 executor classes + ClearanceContext
```

---

### Sample 1: Order Tracking (🔧 Single Agent + Tools)

**Pattern:** Single agent with domain tools, streaming output

| Detail | Value |
| ------- | ------- |
| Project | `SupplyChain.OrderTracking` |
| Agent | `OrderTrackingAgent` |
| Key API | `agent.RunStreamingAsync(query)` |
| Tools | `GetShipmentStatus`, `GetDelayedShipments`, `GetOrdersBySupplier`, `FlagDelayedShipment` |

The agent answers supply chain queries by calling registered tools backed by `MockShipmentService` and `MockOrderService`. Responses stream token-by-token via `IAsyncEnumerable<AgentResponseUpdate>`.

```csharp
var agent = azureOpenAI.GetChatClient(deployment)
    .AsAIAgent(instructions: "...", name: "OrderTrackingAgent",
        tools: [AIFunctionFactory.Create(GetShipmentStatus), ...]);

await foreach (var update in agent.RunStreamingAsync(query))
    Console.Write(update.Text);
```

---

### Sample 2: Disruption Alert (💾 Multi-Turn Session Memory)

**Pattern:** Multi-turn conversation with session persistence and serialization

| Detail | Value |
| ------- | ------- |
| Project | `SupplyChain.DisruptionAlert` |
| Agent | `DisruptionAlertAgent` |
| Key API | `CreateSessionAsync`, `SerializeSessionAsync`, `DeserializeSessionAsync` |
| Tools | `GetActiveDisruptions`, `GetSupplierProfile`, `FindAlternativeSuppliers`, `GetAffectedOrders`, `GetInTransitShipments` |

Demonstrates **cross-turn memory**: the agent recalls which supplier was discussed in Turn 1 when answering Turn 3. Includes a session serialization round-trip that simulates saving to a database and resuming mid-conversation.

```mermaid
sequenceDiagram
    participant U as User
    participant A as DisruptionAlertAgent
    participant S as AgentSession

    U->>A: Turn 1 — "What disruptions are active?"
    A->>S: persist turn
    U->>A: Turn 2 — "What orders do we have with them?"
    A->>S: uses Turn 1 context
    Note over A,S: SerializeSessionAsync → JSON
    Note over A,S: DeserializeSessionAsync → resume
    U->>A: Turn 5 — "What did we decide?"
    A->>S: recalls full prior context
```

---

### Sample 3: Supplier Orchestration (🔀 Multi-Agent Workflow)

**Pattern:** Multi-agent workflow — three specialised executors with human approval gate

| Detail | Value |
| ------- | ------- |
| Project | `SupplyChain.SupplierOrchestration` |
| Trigger | `DisruptionEvent` (SUP-002 factory fire, $1,700 at risk, 5-day deadline) |
| Executors | `SupplierEvaluatorExecutor` → `NegotiationExecutor` → `ErpUpdateExecutor` |
| Human gate | Console prompt in `NegotiationExecutor` — approve/reject contract switch |
| Output | `YieldOutputAsync` → `run.OutgoingEvents.OfType<WorkflowOutputEvent>()` |

```mermaid
flowchart LR
    DE["🚨 Disruption<br/>Event"]
    E1["🔍 Supplier<br/>Evaluator"]
    E2["💬 Negotiation<br/>& Gate"]
    E3["📤 ERP<br/>Update"]

    DE --> E1 --> E2 --> E3

    style DE fill:#ffebee,stroke:#c62828,stroke-width:2px,color:#000
    style E1 fill:#fff3e0,stroke:#e65100,stroke-width:2px,color:#000
    style E2 fill:#fce4ec,stroke:#c2185b,stroke-width:2px,color:#000
    style E3 fill:#e0f2f1,stroke:#00695c,stroke-width:2px,color:#000
```

---

### Sample 4: Document Review (🌍 Single Agent + Domain Tools)

**Pattern:** Single agent with document-domain tools, GO/NO-GO recommendations

| Detail | Value |
| ------- | ------- |
| Project | `Customs.DocumentReview` |
| Agent | `DocumentReviewAgent` |
| Key API | `agent.RunStreamingAsync(query)` |
| Tools | `ListDocumentsForShipment`, `ReviewDocumentFields`, `ValidateHsCode`, `CheckDocumentCompleteness`, `GetShipmentDetails` |

The agent performs a structured 5-step review: document presence → field completeness → HS code validation → cross-document discrepancies → GO/NO-GO recommendation. Uses mock shipments CSH-3001 (complete) and CSH-3002 (gyroscopes, dual-use concern).

---

### Sample 5: Compliance Check (🛢️ Conditional Routing + Human Gate)

**Pattern:** Workflow with conditional edge routing and human officer review gate

| Detail | Value |
| ------- | ------- |
| Project | `Customs.ComplianceCheck` |
| Executors | `SanctionsScreeningExecutor` → `RestrictedGoodsExecutor` → `RiskScoringExecutor` → *(conditional)* → `TariffCalculationExecutor` |
| Routing | `risk > 6` routes through `HumanReviewExecutor`; `risk ≤ 6` skips directly to tariff calc |
| Test data | CSH-3001 (low risk, auto-cleared), CSH-3002 (medium), CSH-3004 (sanctioned, high risk) |

```mermaid
flowchart TD
    S["📦 Customs<br/>Shipment"]
    E1["🌍 Sanctions<br/>Screening"]
    E2["⚠️ Restricted<br/>Goods Check"]
    E3["📊 Risk<br/>Scoring"]
    E4["👤 Human<br/>Review Gate"]
    E5["💰 Tariff<br/>Calculation"]

    S --> E1 --> E2 --> E3
    E3 -->|"risk > 6"| E4 --> E5
    E3 -->|"risk ≤ 6"| E5

    style S fill:#e3f2fd,stroke:#1565c0,stroke-width:2px,color:#000
    style E1 fill:#fff3e0,stroke:#e65100,stroke-width:2px,color:#000
    style E2 fill:#ffebee,stroke:#c62828,stroke-width:2px,color:#000
    style E3 fill:#f3e5f5,stroke:#6a1b9a,stroke-width:2px,color:#000
    style E4 fill:#fce4ec,stroke:#c2185b,stroke-width:2px,color:#000
    style E5 fill:#e0f2f1,stroke:#00695c,stroke-width:2px,color:#000
```

```csharp
// Conditional edge — explicit type parameter required for inference
.AddEdge<ComplianceContext>(bRiskScoring, bHumanReview,
    condition: msg => msg != null && msg.RequiresHumanReview)
.AddEdge<ComplianceContext>(bRiskScoring, bTariffCalc,
    condition: msg => msg != null && !msg.RequiresHumanReview)
```

---

### Sample 6: Clearance Orchestration (📦 End-to-End Pipeline)

**Pattern:** Six-executor end-to-end pipeline with timestamp tracing, shared `ClearanceContext`

| Detail | Value |
| ------- | ------- |
| Project | `Customs.ClearanceOrchestration` |
| Data flow | `CustomsShipment` → `ClearanceContext` → ... → final certificate |
| Executors | 6 in sequence (see diagram below) |
| Tracing | `Trace.Step(step, shipmentId)` prints `[HH:mm:ss.fff]` timestamps |
| Output | `StatusExecutor` emits formal clearance certificate via `YieldOutputAsync` |

```mermaid
graph LR
    IN["📦 Shipment<br/>Pending"]
    E1["📋 Document<br/>Collection"]
    E2["🏷️ HS Code<br/>Classification"]
    E3["🛡️ Compliance<br/>Screening"]
    E4["💰 Duty<br/>Calculation"]
    E5["📝 Filing<br/>Submission"]
    E6["✅ Final<br/>Status"]
    OUT["📜 Clearance<br/>Certificate"]

    IN --> E1 --> E2 --> E3 --> E4 --> E5 --> E6 --> OUT

    style IN fill:#4a90e2,stroke:#003d99,stroke-width:3px,color:#fff
    style E1 fill:#7b68ee,stroke:#4c2a85,stroke-width:2px,color:#fff
    style E2 fill:#f39c12,stroke:#c87f0a,stroke-width:2px,color:#fff
    style E3 fill:#e74c3c,stroke:#a93226,stroke-width:2px,color:#fff
    style E4 fill:#1abc9c,stroke:#117a65,stroke-width:2px,color:#fff
    style E5 fill:#27ae60,stroke:#1a5f39,stroke-width:2px,color:#fff
    style E6 fill:#9b59b6,stroke:#6c3483,stroke-width:2px,color:#fff
    style OUT fill:#2ecc71,stroke:#1e8449,stroke-width:3px,color:#fff
```

The `ClearanceContext` record accumulates state across all six executors:

```csharp
record ClearanceContext(
    CustomsShipment Shipment,
    bool DocumentsComplete, List<string>? MissingDocs,
    bool ClassificationPassed, List<string>? ClassificationIssues,
    bool CompliancePassed, int RiskScore, List<string>? ComplianceFlags,
    decimal TotalDuty, decimal TotalVat, List<string>? DutyBreakdown,
    bool Filed, string? DeclarationRef
);
```

---

## Key Framework Patterns

### Creating an Agent

```csharp
// From any ChatClient (Azure OpenAI, OpenAI, etc.)
var agent = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey))
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a ...",
        name: "MyAgent",
        tools: [AIFunctionFactory.Create(MyTool)]
    );
```

### Tool Registration

```csharp
[Description("Retrieves the current status for a shipment.")]
ShipmentStatus GetStatus(
    [Description("The tracking number, e.g. TRK-001")] string trackingNumber)
{ ... }

// Register with the agent
AIFunctionFactory.Create(GetStatus)
```

### Streaming vs. Non-Streaming

```csharp
// Streaming — for user-facing output
await foreach (var update in agent.RunStreamingAsync(prompt, session))
    Console.Write(update.Text);

// Non-streaming — inside executor handlers
var response = await agent.RunAsync(prompt);
Console.WriteLine(response.Text);  // use .Text, not .ToString()
```

### Multi-Turn Sessions

```csharp
AgentSession session = await agent.CreateSessionAsync();

// Each turn automatically uses prior context
await foreach (var update in agent.RunStreamingAsync(turn1, session)) ...
await foreach (var update in agent.RunStreamingAsync(turn2, session)) ...

// Persist and restore
var json    = await agent.SerializeSessionAsync(session);
var resumed = await agent.DeserializeSessionAsync(json);
```

### Executor + Workflow Pattern

```csharp
// 1. Define executor (partial class required for source generator)
internal sealed partial class MyExecutor(AIAgent agent) : Executor("MyStep")
{
    [MessageHandler]
    private async ValueTask<OutputType> HandleAsync(InputType input, IWorkflowContext wf)
    {
        var response = await agent.RunAsync(prompt);
        await wf.YieldOutputAsync("result summary", CancellationToken.None);
        return new OutputType(...);
    }
}

// 2. Bind and wire
var bStep1 = step1.BindExecutor();   // → ExecutorBinding
var bStep2 = step2.BindExecutor();

var workflow = new WorkflowBuilder("MyWorkflow")
    .BindExecutor(bStep1)            // → WorkflowBuilder (fluent)
    .BindExecutor(bStep2)
    .AddEdge(bStep1, bStep2)
    .Build();

// 3. Run
var run = await InProcessExecution.Default.RunAsync(workflow, input);
foreach (var evt in run.OutgoingEvents.OfType<WorkflowOutputEvent>())
    Console.WriteLine(evt.Data);
```

### Conditional Edges

```csharp
// Explicit type parameter required — C# cannot infer T from pattern-matching lambdas
.AddEdge<MyContext>(bSource, bHighRiskTarget,
    condition: msg => msg != null && msg.RiskScore > 6)
.AddEdge<MyContext>(bSource, bLowRiskTarget,
    condition: msg => msg != null && msg.RiskScore <= 6)
```

---

## Known Patterns & Troubleshooting

### ChatResponseFormat Disambiguation

When a project imports both `OpenAI.Chat` and `Microsoft.Extensions.AI`, the `ChatResponseFormat` type becomes ambiguous. **Always use the fully qualified type** to avoid compilation errors:

```csharp
// ❌ Ambiguous — will not compile
var response = await agent.RunAsync<MyOutputType>(prompt, 
    new ChatClientAgentOptions 
    { 
        ResponseFormat = ChatResponseFormat.ForJsonSchema<MyOutputType>()
    });

// ✅ Correct — use full namespace
var response = await agent.RunAsync<MyOutputType>(prompt,
    new ChatClientAgentOptions
    {
        ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema<MyOutputType>()
    });
```

### Reasoning Effort Configuration

In `Microsoft.Agents.AI.OpenAI v1.1.0`, custom `ChatOptions` for reasoning effort must be passed via the `AsAIAgent(ChatClientAgentOptions)` overload. Named parameters like `options: ChatOptions(...)` are **not available**:

```csharp
// ❌ Will fail — no 'options:' parameter exists
var agent = chatClient.AsAIAgent(
    instructions: "...",
    options: new ChatOptions { ... }
);

// ✅ Correct — use ChatClientAgentOptions
var agent = chatClient.AsAIAgent(
    instructions: "...",
    new ChatClientAgentOptions
    {
        ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema<T>(),
        // other options here
    }
);
```

### Composing Multiple Tool Sets

When registering tools from multiple sources (e.g., reflected methods from different classes), **create each tool sequence independently and concatenate**, rather than chaining `Select()` over existing `AIFunction` objects:

```csharp
// ❌ Will fail — type inference and delegate mismatch
var tools = toolSet1
    .Concat(AIFunctionFactory.Create(tool2Method))
    .Select(t => t);

// ✅ Correct — create each sequence separately
var toolsFromClass1 = new[] { AIFunctionFactory.Create(method1), ... };
var toolsFromClass2 = new[] { AIFunctionFactory.Create(method2), ... };
var allTools = toolsFromClass1.Concat(toolsFromClass2).ToArray();

var agent = chatClient.AsAIAgent(
    instructions: "...",
    tools: allTools
);
```

---

## Prerequisites & Setup

### Requirements

- .NET 10 SDK
- Azure OpenAI resource with a deployed model (e.g. `gpt-4o`)

### Configuration

Each sample project needs an `appsettings.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "DeploymentName": "gpt-4o",
    "ApiKey": "<your-api-key>"
  }
}
```

Or set environment variables: `AzureOpenAI__Endpoint`, `AzureOpenAI__DeploymentName`, `AzureOpenAI__ApiKey`.

### Build

```bash
dotnet build SupplyChainCustoms.AgentFramework.slnx
```

### Run a Sample

```bash
# Sample 7 — basic customs RAG
dotnet run --project samples/01-RAG/00-customs-rag-basic/00-customs-rag-basic

# Sample 8 — embeddings-based customs RAG
dotnet run --project samples/01-RAG/01-customs-rag-embeddings/01-customs-rag-embeddings

# Sample 1 — single agent, streaming
dotnet run --project samples/02-supply-chain/SupplyChain.OrderTracking/SupplyChain.OrderTracking

# Sample 2 — multi-turn session
dotnet run --project samples/02-supply-chain/SupplyChain.DisruptionAlert/SupplyChain.DisruptionAlert

# Sample 3 — workflow (will prompt for human approval)
dotnet run --project samples/02-supply-chain/SupplyChain.SupplierOrchestration/SupplyChain.SupplierOrchestration

# Sample 4 — customs document review
dotnet run --project samples/03-customs/Customs.DocumentReview/Customs.DocumentReview

# Sample 5 — compliance check with conditional routing (may prompt for officer approval)
dotnet run --project samples/03-customs/Customs.ComplianceCheck/Customs.ComplianceCheck

# Sample 6 — full clearance orchestration pipeline
dotnet run --project samples/03-customs/Customs.ClearanceOrchestration/Customs.ClearanceOrchestration
```

---

## Mock Data Reference

### Supply Chain Shipments

| ID | Status | Route | Notes |
| ---- | -------- | ------- | ------- |
| TRK-001-2025 | InTransit | Shanghai → Rotterdam | Normal |
| TRK-002-2025 | Delayed | Shenzhen → Los Angeles | Weather delay |
| TRK-003-2025 | InTransit | Mumbai → Hamburg | On time |
| TRK-004-2025 | AtPort | Taipei → New York | Customs hold |
| TRK-005-2025 | Delivered | Seoul → Sydney | Complete |

### Supply Chain Suppliers

| ID | Name | Reliability | Notes |
| ---- | ------ | ------------ | ------- |
| SUP-001 | TechParts Asia | 88/100 | Active, Electronics |
| SUP-002 | GlobalTech Components | — | **DISRUPTED** — factory fire |
| SUP-003 | EuroComponents GmbH | 91/100 | Active, best alternative |
| SUP-004 | AmeriParts Inc | 78/100 | Active |
| SUP-005 | AsiaManufacturing Co | 82/100 | Active |

### Customs Shipments

| ID | Importer | Origin | Risk | Notes |
| ---- | ---------- | -------- | ------ | ------- |
| CSH-3001 | TechImport UK Ltd | CN | Low | Standard electronics — clean |
| CSH-3002 | Precision Instruments | DE | Medium | Gyroscopes — dual-use concern |
| CSH-3003 | Apparel Imports Ltd | IN | Low | Textiles — routine |
| CSH-3004 | Industrial Machinery Co | IR | High | **Sanctioned origin**, restricted goods |

### Sanctioned Countries (MockTariffService)

`IR` (Iran), `KP` (North Korea), `SY` (Syria), `CU` (Cuba)
