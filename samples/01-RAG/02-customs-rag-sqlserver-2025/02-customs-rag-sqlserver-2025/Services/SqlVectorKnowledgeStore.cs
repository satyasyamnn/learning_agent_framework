using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.AI;

internal sealed class SqlVectorKnowledgeStore(
    string connectionString,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    int embeddingDimensions)
{
    public static async Task<bool> SupportsVectorTypeAsync(string sqlConnectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(sqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string commandText = "SELECT VECTOR_DISTANCE('cosine', CAST('[0.0,0.0]' AS vector(2)), CAST('[0.0,0.0]' AS vector(2)));";
        await using var command = new SqlCommand(commandText, connection);

        try
        {
            await command.ExecuteScalarAsync(cancellationToken);
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }

    public static async Task CreateDatabaseIfNeededAsync(string sqlConnectionString, CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder(sqlConnectionString);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            throw new InvalidOperationException("The SQL Server connection string must include a Database value.");
        }

        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var commandText = $"IF DB_ID(N'{EscapeSqlLiteral(databaseName)}') IS NULL EXEC(N'CREATE DATABASE [{EscapeSqlIdentifier(databaseName)}]');";
        await using var command = new SqlCommand(commandText, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task EnsureSchemaAsync(
        string sqlConnectionString,
        int embeddingDimensions,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(sqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        var commandText = $"""
IF OBJECT_ID(N'dbo.CustomsKnowledge', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomsKnowledge
    (
        Reference nvarchar(32) NOT NULL PRIMARY KEY,
        Title nvarchar(200) NOT NULL,
        Content nvarchar(max) NOT NULL,
        Region nvarchar(32) NOT NULL,
        RiskLevel nvarchar(32) NOT NULL,
        Embedding vector({embeddingDimensions}) NOT NULL
    );
END;
ELSE IF EXISTS
(
    SELECT 1
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.CustomsKnowledge')
      AND c.name = N'Embedding'
      AND t.name <> N'vector'
)
BEGIN
    DROP TABLE dbo.CustomsKnowledge;

    CREATE TABLE dbo.CustomsKnowledge
    (
        Reference nvarchar(32) NOT NULL PRIMARY KEY,
        Title nvarchar(200) NOT NULL,
        Content nvarchar(max) NOT NULL,
        Region nvarchar(32) NOT NULL,
        RiskLevel nvarchar(32) NOT NULL,
        Embedding vector({embeddingDimensions}) NOT NULL
    );
END;
""";

        await using var command = new SqlCommand(commandText, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SeedAsync(IReadOnlyList<CustomsKnowledgeEntry> entries, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var deleteCommand = new SqlCommand("DELETE FROM dbo.CustomsKnowledge;", connection, (SqlTransaction)transaction))
        {
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var count = 0;
        foreach (var entry in entries)
        {
            count++;
            Console.Write($"\rSeeding SQL vector records: {count}/{entries.Count}");

            var embeddingVectorLiteral = await GenerateEmbeddingVectorLiteralAsync(entry.GetEmbeddingInput(), cancellationToken);

            var commandText = $"""
INSERT INTO dbo.CustomsKnowledge (Reference, Title, Content, Region, RiskLevel, Embedding)
VALUES (@reference, @title, @content, @region, @riskLevel, CAST(@embedding AS vector({embeddingDimensions})));
""";

            await using var insertCommand = new SqlCommand(commandText, connection, (SqlTransaction)transaction);
            insertCommand.Parameters.AddWithValue("@reference", entry.Reference);
            insertCommand.Parameters.AddWithValue("@title", entry.Title);
            insertCommand.Parameters.AddWithValue("@content", entry.Content);
            insertCommand.Parameters.AddWithValue("@region", entry.Region);
            insertCommand.Parameters.AddWithValue("@riskLevel", entry.RiskLevel);
            insertCommand.Parameters.Add("@embedding", SqlDbType.NVarChar, -1).Value = embeddingVectorLiteral;

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        Console.WriteLine();
    }

    public async Task<List<string>> SearchAsync(string question, int topK = 5, CancellationToken cancellationToken = default)
    {
        var queryVector = await GenerateEmbeddingVectorAsync(question, cancellationToken);
        var results = new List<string>();

        var embeddingVectorLiteral = ToSqlVectorLiteral(queryVector);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var commandText = $"""
SELECT TOP (@topK)
    Reference,
    Title,
    Content,
    Region,
    RiskLevel
FROM dbo.CustomsKnowledge
ORDER BY VECTOR_DISTANCE('cosine', Embedding, CAST(@embedding AS vector({embeddingDimensions})));
""";

        await using var command = new SqlCommand(commandText, connection);
        command.Parameters.AddWithValue("@topK", Math.Clamp(topK, 1, 8));
        command.Parameters.Add("@embedding", SqlDbType.NVarChar, -1).Value = embeddingVectorLiteral;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add($"[{reader.GetString(0)}] {reader.GetString(1)} (Region: {reader.GetString(3)}, Risk: {reader.GetString(4)}) - {reader.GetString(2)}");
        }

        return results;
    }

    private async Task<float[]> GenerateEmbeddingVectorAsync(string input, CancellationToken cancellationToken)
    {
        var embeddings = await embeddingGenerator.GenerateAsync([input], cancellationToken: cancellationToken);
        var vector = embeddings[0].Vector.ToArray();
        if (vector.Length < embeddingDimensions)
        {
            throw new InvalidOperationException($"Embedding vector length {vector.Length} is smaller than configured embedding dimension {embeddingDimensions}.");
        }

        if (vector.Length == embeddingDimensions)
        {
            return vector;
        }

        var normalized = new float[embeddingDimensions];
        Array.Copy(vector, normalized, embeddingDimensions);
        return normalized;
    }

    private async Task<string> GenerateEmbeddingVectorLiteralAsync(string input, CancellationToken cancellationToken)
    {
        var vector = await GenerateEmbeddingVectorAsync(input, cancellationToken);
        return ToSqlVectorLiteral(vector);
    }

    private static string ToSqlVectorLiteral(ReadOnlySpan<float> values)
    {
        var builder = new StringBuilder(values.Length * 12);
        builder.Append('[');

        for (var index = 0; index < values.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(values[index].ToString("G9", CultureInfo.InvariantCulture));
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string EscapeSqlIdentifier(string identifier) => identifier.Replace("]", "]]", StringComparison.Ordinal);

    private static string EscapeSqlLiteral(string literal) => literal.Replace("'", "''", StringComparison.Ordinal);
}
