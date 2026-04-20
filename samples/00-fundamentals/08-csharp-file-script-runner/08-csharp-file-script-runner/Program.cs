#pragma warning disable MAAI001
using System.Text.Json;
using Fundamentals.Shared;
using Microsoft.Agents.AI;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var chatClient = FundamentalsAgentFactory.CreateChatClient(config);

var skillsPath = Path.Combine(AppContext.BaseDirectory, "skills");
var dutySkillRoot = Path.Combine(skillsPath, "csharp-duty-and-lane");

var csharpDutyAndLaneSkill = new AgentInlineSkill(
    name: "csharp-duty-and-lane",
    description: "Estimate customs duty and determine whether formal entry is likely for simple declaration checks.",
    instructions: """
        Use this skill for duty calculations or formal-entry threshold checks.
        Always run the estimate-duty script instead of calculating the result yourself.
        Read the entry-policy resource when the user asks why formal entry is or is not likely.
        """)
    .AddResource(
        "entry-policy",
        () => File.ReadAllText(Path.Combine(dutySkillRoot, "references", "entry-policy.md")),
        "Reference guidance for formal entry threshold checks.")
    .AddScript(
        "estimate-duty",
        (decimal declaredValueUsd, decimal dutyRatePercent) => CSharpFileSkillScriptRunner.RunDutyEstimateScript(dutySkillRoot, declaredValueUsd, dutyRatePercent),
        "Estimate customs duty by executing the C# estimate-duty.csx script from disk.");

var skillsProvider = new AgentSkillsProvider(csharpDutyAndLaneSkill);

AIAgent agent = chatClient
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
    Console.WriteLine($"> {prompt}");
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
    private static readonly ScriptOptions ScriptOptions = ScriptOptions.Default
        .AddReferences(typeof(object).Assembly, typeof(JsonSerializer).Assembly, typeof(Enumerable).Assembly)
        .AddImports("System", "System.Linq", "System.Collections.Generic", "System.Text.Json");


    public static Task<object?> RunAsync(
        string scriptPath,
        string scriptName,
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
        => RunScriptAsync(scriptPath, scriptName, arguments, cancellationToken);

    public static string RunDutyEstimateScript(string dutySkillRoot, decimal declaredValueUsd, decimal dutyRatePercent)
    {
        var scriptPath = Path.Combine(dutySkillRoot, "scripts", "estimate-duty.csx");

        var result = RunAsync(
            scriptPath,
            "estimate-duty",
            new Dictionary<string, object?>
            {
                ["declaredValueUsd"] = declaredValueUsd,
                ["dutyRatePercent"] = dutyRatePercent,
            },
            CancellationToken.None).GetAwaiter().GetResult();

        return result?.ToString() ?? "(no output)";
    }

    private static async Task<object?> RunScriptAsync(
        string scriptPath,
        string scriptName,
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(scriptPath))
        {
            return $"Error: Script file not found: {scriptPath}";
        }

        if (!string.Equals(Path.GetExtension(scriptPath), ".csx", StringComparison.OrdinalIgnoreCase))
        {
            return $"Error: Unsupported script extension '{Path.GetExtension(scriptPath)}'. This sample only allows .csx scripts.";
        }

        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine("\n📋 [Script Execution Details]");
        Console.WriteLine($"  📁 Script File: {scriptPath}");
        Console.WriteLine($"  📄 Script Name: {scriptName}");

        string code = await File.ReadAllTextAsync(scriptPath, cancellationToken);

        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine("\n  📝 Script Content:");
        foreach (var line in code.Split(Environment.NewLine))
        {
            Console.WriteLine($"     {line}");
        }

        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine("\n  📥 Arguments Passed:");
        foreach (var arg in arguments)
        {
            Console.WriteLine($"     {arg.Key} = {arg.Value} ({arg.Value?.GetType().Name ?? "null"})");
        }

        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine("\n  ⚙️  Executing C# Script...");
        Console.ResetColor();

        var globals = new SkillScriptGlobals(scriptPath, scriptName, arguments);

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
    string ScriptPath,
    string ScriptName,
    IDictionary<string, object?> Args);
