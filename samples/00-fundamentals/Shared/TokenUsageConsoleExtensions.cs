using Microsoft.Agents.AI;

namespace Fundamentals.Shared;

internal static class TokenUsageConsoleExtensions
{
    public static void WriteTokenUsageToConsole(this AgentResponse response, string label = "Token Usage")
    {
        if (response.Usage is null)
        {
            Console.WriteLine($"[{label}] Input: n/a | Output: n/a | Reasoning: n/a");
            return;
        }

        Console.WriteLine(
            $"[{label}] Input: {response.Usage.InputTokenCount} | Output: {response.Usage.OutputTokenCount} | Reasoning: {response.Usage.ReasoningTokenCount} | Total: {response.Usage.TotalTokenCount} ");
    }

    public static async Task<AgentResponse> WriteStreamingResponseAndTokenUsageToConsoleAsync(
        this IAsyncEnumerable<AgentResponseUpdate> updates,
        string label = "Streaming Token Usage")
    {
        AgentResponse response = await updates.ToAgentResponseAsync();
        Console.WriteLine(response.Text);
        response.WriteTokenUsageToConsole(label);
        return response;
    }
}