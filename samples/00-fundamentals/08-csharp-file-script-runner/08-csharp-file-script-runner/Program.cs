#pragma warning disable MAAI001
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var endpointUrl = config["AzureOpenAI:Endpoint"]
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
var deploymentName = config["AzureOpenAI:DeploymentName"]
    ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName not configured");
var apiKey = config["AzureOpenAI:ApiKey"];
var endpoint = new Uri(new Uri(endpointUrl).GetLeftPart(UriPartial.Authority));

var azureClient = string.IsNullOrWhiteSpace(apiKey)
    ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
    : new AzureOpenAIClient(endpoint, new AzureKeyCredential(apiKey));

var skillsPath = Path.Combine(AppContext.BaseDirectory, "skills");
var skillsProvider = new AgentSkillsProvider(skillsPath, CSharpFileSkillScriptRunner.RunAsync);

AIAgent agent = azureClient
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "CustomsCSharpScriptRunnerAgent",
        ChatOptions = new()
        {
            Instructions = """
                You are a customs operations copilot.
                ALWAYS use the csharp-duty-and-lane skill for every duty or lane decision request.
                ALWAYS invoke the skill script, even if you have calculated similar values before.
                Do not infer or estimate duty calculations - always call the skill.
                """
        },
        AIContextProviders = [skillsProvider],
    });

AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("=============================================================");
Console.WriteLine("  C# File-Based Skill Runner Demo");
Console.WriteLine("=============================================================");
Console.WriteLine("This sample uses file-based skills, but executes scripts with C# (.csx).\n");

foreach (var prompt in new[]
{
    "Using the csharp-duty-and-lane skill, estimate duty for declared value 84500 USD at 6.5%.",
    "For a declared value of 1200 USD at 3.2%, estimate duty and tell me whether formal entry is likely.",
})
{
    // Create a new session for each prompt to ensure skill invocation is captured
    var promptSession = await agent.CreateSessionAsync();
    
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"> {prompt}");
    Console.ResetColor();

    AgentResponse response = await agent.RunAsync(prompt, promptSession);
    PrintSkillToolCalls(response);
    Console.WriteLine(response.Text);
    Console.WriteLine();
}

Console.WriteLine("Try your own prompt, or type 'exit' to quit.");

var interactiveSession = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    AgentResponse response = await agent.RunAsync(input, interactiveSession);
    PrintSkillToolCalls(response);
    Console.WriteLine(response.Text);
    Console.WriteLine();
}

static void PrintSkillToolCalls(AgentResponse response)
{
    var toolCalls = response.Messages
        .SelectMany(m => m.Contents)
        .OfType<FunctionCallContent>()
        .ToList();

    if (toolCalls.Count == 0)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("[No skill tool calls captured in this turn]");
        Console.ResetColor();
        return;
    }

    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine("[Skill tool calls]");

    foreach (var call in toolCalls)
    {
        var formattedArgs = call.Arguments?.Count > 0
            ? string.Join(", ", call.Arguments.Select(a => $"{a.Key}={a.Value}"))
            : "no-args";

        Console.WriteLine($"- {call.Name}({formattedArgs})");
    }

    Console.ResetColor();
}

internal static class CSharpFileSkillScriptRunner
{
    private static readonly ScriptOptions ScriptOptions = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default
        .AddReferences(typeof(object).Assembly, typeof(JsonSerializer).Assembly, typeof(Enumerable).Assembly)
        .AddImports("System", "System.Linq", "System.Collections.Generic", "System.Text.Json");

    public static async Task<object?> RunAsync(
        AgentFileSkill skill,
        AgentFileSkillScript script,
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(script.FullPath))
        {
            return $"Error: Script file not found: {script.FullPath}";
        }

        if (!string.Equals(Path.GetExtension(script.FullPath), ".csx", StringComparison.OrdinalIgnoreCase))
        {
            return $"Error: Unsupported script extension '{Path.GetExtension(script.FullPath)}'. This sample only allows .csx scripts.";
        }

        // Log: Script execution initiated
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine("\n📋 [Script Execution Details]");
        Console.WriteLine($"  📁 Script File: {script.FullPath}");
        Console.WriteLine($"  📄 Script Name: {script.Name}");

        string code = await File.ReadAllTextAsync(script.FullPath, cancellationToken);

        // Log: Show script content
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine("\n  📝 Script Content:");
        foreach (var line in code.Split(Environment.NewLine))
        {
            Console.WriteLine($"     {line}");
        }

        var args = arguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Log: Show passed arguments
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine("\n  📥 Arguments Passed:");
        foreach (var arg in args)
        {
            Console.WriteLine($"     {arg.Key} = {arg.Value} ({arg.Value?.GetType().Name ?? "null"})");
        }

        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine("\n  ⚙️  Executing C# Script...");
        Console.ResetColor();

        var globals = new SkillScriptGlobals(skill, script, args);

        var result = await CSharpScript.EvaluateAsync<object?>(
            code,
            ScriptOptions,
            globals,
            typeof(SkillScriptGlobals),
            cancellationToken);

        // Log: Show result
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine($"  ✅ Execution Result:");
        Console.WriteLine($"     {result ?? "(no output)"}");
        Console.ResetColor();
        Console.WriteLine();

        return result ?? "(no output)";
    }
}

public sealed record SkillScriptGlobals(
    AgentFileSkill Skill,
    AgentFileSkillScript Script,
    IDictionary<string, object?> Args);
