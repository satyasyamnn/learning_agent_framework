using System.ComponentModel;

internal sealed class CustomsSqlSearchTool(SqlVectorKnowledgeStore knowledgeStore)
{
    [Description("Searches SQL Server 2025 semantic customs knowledge and returns top matching snippets.")]
    public Task<List<string>> SearchSqlVectorStore(
        [Description("Customs question or keywords to search for.")] string question,
        [Description("Maximum number of snippets to return (1-8). Defaults to 5.")] int topK = 5)
    {
        return knowledgeStore.SearchAsync(question, topK);
    }
}
