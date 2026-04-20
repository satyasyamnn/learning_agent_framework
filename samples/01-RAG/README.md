# RAG Samples (01) — Retrieval-Augmented Generation

Knowledge sharing examples for **Retrieval-Augmented Generation (RAG)** using the **Microsoft Agent Framework** (`Microsoft.Agents.AI` v1.1.0) in **.NET 10 / C# 13**. Both samples share the same customs domain knowledge base so retrieval strategies can be compared side by side.

---

## Samples

| Sample | Project | Concept | Key Feature |
| ------- | ------- | --------- | ------------ |
| 01 | `01-customs-rag-embeddings` | 🧭 Embedding-Based RAG | Semantic vector retrieval using in-memory vector store |
| 02 | `02-customs-rag-sqlserver-2025` | 🗄️ SQL Vector RAG | Semantic vector retrieval using SQL Server 2025 |

---

## What Was Introduced

- Added an **embedding-based customs RAG** sample using Azure OpenAI embeddings and in-memory vector search.
- Added a **SQL Server 2025 RAG** sample that stores embeddings in a SQL `vector` column and retrieves matches with `VECTOR_DISTANCE(...)`.
- Added support for configurable embedding settings via `AzureOpenAI:EmbeddingEndpoint`, `AzureOpenAI:EmbeddingDeploymentName`, and `AzureOpenAI:EmbeddingApiKey`.

---

## RAG-Specific NuGet Packages

| Package | Version | Used for |
| ------- | ------- | ---------- |
| `Microsoft.Extensions.AI.OpenAI` | 10.4.0 | OpenAI/Azure OpenAI extension helpers used by RAG samples |
| `Microsoft.Extensions.VectorData.Abstractions` | 10.1.0 | Vector data contracts used in embedding-based RAG |
| `Microsoft.SemanticKernel.Connectors.InMemory` | 1.74.0-preview | In-memory vector store for semantic retrieval |
| `Microsoft.Data.SqlClient` | 6.1.1 | SQL Server 2025 vector storage and retrieval |

> These are **in addition** to the common packages listed in the root `README.md`.

---

## Sample 01: Customs RAG with Embeddings (🧭 Semantic Vector Retrieval)

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

## Sample 02: Customs RAG with SQL Server 2025 (🗄️ SQL Vector Retrieval)

**Pattern:** Embedding-based Retrieval-Augmented Generation using SQL Server 2025 vector storage

| Detail | Value |
| ------- | ------- |
| Project | `02-customs-rag-sqlserver-2025` |
| Agent | `CustomsSqlServerRagAgent` |
| Key API | `CAST(@embedding AS vector(1536))`, `VECTOR_DISTANCE('cosine', ...)`, `Microsoft.Data.SqlClient` |
| Behavior | Persists embeddings in SQL Server 2025 and retrieves nearest customs snippets before answering |

This sample demonstrates semantic RAG with a database-backed vector store. It keeps the same customs domain and agent workflow as the in-memory embedding sample, but stores embeddings in SQL Server 2025 and queries them with vector distance functions.

**Solution approach:** The sample creates a database and table if needed, embeds each customs knowledge entry, writes the embeddings into a SQL `vector(1536)` column, and uses cosine distance ordering to fetch the most relevant rows for the user question. The retrieval path is also exposed as an agent tool so the model can fetch grounding context on demand.

### Configuration

Set a SQL Server 2025 connection string before running:

```powershell
$env:SQLSERVER2025_CONNECTION_STRING = "Server=localhost,1433;Database=CustomsRag2025;User ID=sa;Password=Your_password123;Encrypt=False;TrustServerCertificate=True"
```

### Run

```bash
dotnet run --project samples/01-RAG/02-customs-rag-sqlserver-2025/02-customs-rag-sqlserver-2025
```

---

## Planned Advanced Samples

Additional projects planned for future RAG patterns:

- Chunking and indexing strategies
- Hybrid retrieval (keyword + semantic)
- Re-ranking
- Multi-step retrieval workflows
- Grounding and citation validation
