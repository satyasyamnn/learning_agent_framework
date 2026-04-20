# Customs RAG with SQL Server 2025

This sample mirrors `01-customs-rag-embeddings` but stores and queries embeddings in **SQL Server 2025** instead of an in-memory vector store.

## What it demonstrates

- Reuses the same customs knowledge base and agent pattern as the embedding sample.
- Generates embeddings with Azure OpenAI.
- Creates a SQL Server 2025 database and `dbo.CustomsKnowledge` table if they do not exist.
- Persists embeddings into a SQL `vector(1536)` column.
- Retrieves top semantic matches with `VECTOR_DISTANCE('cosine', ...)` before answering.
- Exposes the SQL retrieval path as an agent tool for grounded customs answers.

## Prerequisites

- SQL Server 2025 with vector support enabled.
- The shared `appsettings.json` configured for Azure OpenAI chat and embedding deployments.
- A connection string supplied via the `SQLSERVER2025_CONNECTION_STRING` environment variable.

Example PowerShell setup:

```powershell
$env:SQLSERVER2025_CONNECTION_STRING = "Server=localhost,1433;Database=CustomsRag2025;User ID=sa;Password=Your_password123;Encrypt=False;TrustServerCertificate=True"
```

## Run

```bash
dotnet run --project samples/01-RAG/02-customs-rag-sqlserver-2025/02-customs-rag-sqlserver-2025
```

## Notes

- The sample recreates the table contents on startup so each run starts from a known dataset.
- If you switch to an embedding model with a different dimension, update the `EmbeddingDimensions` constant in `Program.cs` to match the model output and SQL vector column size.