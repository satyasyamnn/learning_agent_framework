# RAG Samples (01) — Retrieval-Augmented Generation

Knowledge sharing examples for **Retrieval-Augmented Generation (RAG)** using the **Microsoft Agent Framework** (`Microsoft.Agents.AI` v1.1.0) in **.NET 10 / C# 13**. Both samples share the same customs domain knowledge base so retrieval strategies can be compared side by side.

---

## Samples

| Sample | Project | Concept | Key Feature |
| ------- | ------- | --------- | ------------ |
| 01 | `00-customs-rag-basic` | 📚 Tool-Based RAG | Local knowledge retrieval with grounding citations (`[KB-xxx]`) |
| 02 | `01-customs-rag-embeddings` | 🧭 Embedding-Based RAG | Semantic vector retrieval using in-memory vector store |

---

## What Was Introduced

- Added a **basic customs RAG** sample using a retrieval tool (`RetrieveCustomsKnowledge`) and token-overlap ranking.
- Added an **embedding-based customs RAG** sample using Azure OpenAI embeddings and in-memory vector search.
- Added support for configurable embedding settings via `AzureOpenAI:EmbeddingEndpoint`, `AzureOpenAI:EmbeddingDeploymentName`, and `AzureOpenAI:EmbeddingApiKey`.

---

## RAG-Specific NuGet Packages

| Package | Version | Used for |
| ------- | ------- | ---------- |
| `Microsoft.Extensions.AI.OpenAI` | 10.4.0 | OpenAI/Azure OpenAI extension helpers used by RAG samples |
| `Microsoft.Extensions.VectorData.Abstractions` | 10.1.0 | Vector data contracts used in embedding-based RAG |
| `Microsoft.SemanticKernel.Connectors.InMemory` | 1.74.0-preview | In-memory vector store for semantic retrieval |

> These are **in addition** to the common packages listed in the root `README.md`.

---

## Sample 01: Customs RAG Basic (📚 Agent + Retrieval Tool)

**Pattern:** Basic Retrieval-Augmented Generation using a local customs knowledge base and a retrieval tool

| Detail | Value |
| ------- | ------- |
| Project | `00-customs-rag-basic` |
| Agent | `CustomsRagAgent` |
| Key API | `AIFunctionFactory.Create(RetrieveCustomsKnowledge)` |
| Behavior | Retrieves top customs snippets and answers with grounding citations like `[KB-001]` |

This sample demonstrates a lightweight RAG pattern in the Microsoft Agent Framework without external vector infrastructure. It uses a local knowledge corpus, token-overlap ranking, and a tool call to provide grounded answers for customs questions on documents, HS code classification, duty basics, sanctions, and dual-use checks.

**Solution approach:** The implementation keeps retrieval intentionally simple. It builds a small in-memory customs knowledge base, ranks candidate snippets with token overlap, and exposes retrieval through a tool the agent can call before answering.

The flow is: user question → retrieval tool selects top snippets → agent answers using only grounded context and returns citation-style references such as `[KB-001]`. This makes the example easy to understand without introducing embeddings or external search infrastructure.

### Run

```bash
dotnet run --project samples/01-RAG/00-customs-rag-basic/00-customs-rag-basic
```

---

## Sample 02: Customs RAG with Embeddings (🧭 Semantic Vector Retrieval)

**Pattern:** Embedding-based Retrieval-Augmented Generation using semantic vector search

| Detail | Value |
| ------- | ------- |
| Project | `01-customs-rag-embeddings` |
| Agent | `CustomsEmbeddingRagAgent` |
| Key API | `GetEmbeddingClient(...).AsIEmbeddingGenerator()`, `InMemoryVectorStore`, `VectorSearchAsync(...)` |
| Behavior | Retrieves semantically similar customs snippets using embeddings before answering |

This sample demonstrates semantic RAG with vector retrieval. It builds embeddings for customs knowledge records, stores them in an in-memory vector index, and retrieves top semantic matches for grounded responses.

**Solution approach:** This version extends the same customs domain with semantic retrieval. It generates embeddings for each knowledge record, stores them in an in-memory vector store, and performs similarity search so retrieval works even when the user wording does not exactly match the stored text.

The flow is: build embeddings → upsert records into the vector index → run vector search for the user query → pass the matched snippets into the agent for grounded answering. This sample shows the shape of a semantic RAG pipeline while still keeping the infrastructure local and inspectable.

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

### Configuration for Embeddings

The embedding client can be configured separately from the chat model in `appsettings.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "DeploymentName": "gpt-4o",
    "ApiKey": "<your-api-key>",
    "EmbeddingEndpoint": "https://<your-resource>.openai.azure.com/",
    "EmbeddingDeploymentName": "text-embedding-3-large",
    "EmbeddingApiKey": "<your-api-key>"
  }
}
```

If `EmbeddingEndpoint` is omitted, the same endpoint as the chat model is used.

### Run

```bash
dotnet run --project samples/01-RAG/01-customs-rag-embeddings/01-customs-rag-embeddings
```

---

## Planned Advanced Samples

Additional projects planned for future RAG patterns:

- Chunking and indexing strategies
- Hybrid retrieval (keyword + semantic)
- Re-ranking
- Multi-step retrieval workflows
- Grounding and citation validation
