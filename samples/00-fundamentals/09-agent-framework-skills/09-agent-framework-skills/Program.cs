#pragma warning disable MAAI001
using System.Text.Json;
using Fundamentals.Shared;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var chatClient = AiAgentFactory.CreateChatClient(config);

var clearancePacketSkill = new AgentInlineSkill(
    name: "customs-clearance-packet",
    description: "Guide review of a customs clearance packet, including required documents, routing policy, and duty estimation.",
    instructions: """
        Use this skill when the user asks whether a shipment is document-ready, what paperwork is required,
        or how much duty is likely due.

        Workflow:
        1. Load this skill before answering procedural questions.
        2. Read the relevant resource when the user asks about document requirements or lane-routing policy.
        3. Run the estimate-duty script when the user provides declared value and duty rate.
        4. Keep the answer operational: missing documents, likely duty, and what the broker should do next.
        """)
    .AddResource(
        "required-documents",
        """
        Required documents for a routine import customs packet:
        - Commercial invoice with seller, buyer, Incoterms, currency, and full line-item values
        - Packing list with package count, gross/net weight, and carton identifiers
        - Bill of lading or air waybill
        - Entry summary / customs declaration draft
        - Country of origin statement or certificate when preferential treatment is claimed
        - Import license or permit when the commodity is controlled

        Escalate if HS classification, valuation basis, or consignee identity is unclear.
        """,
        "Reference checklist for validating a routine customs packet.")
    .AddResource(
        "lane-selection-policy",
        () => $$"""
        Lane selection policy snapshot:
        - Green lane: complete packet, low-risk origin, no restricted-party or licensing flags
        - Amber lane: document gaps, value anomalies, or moderate compliance concerns
        - Red lane: missing control documents, licensing concerns, sanctions/restricted-party hit, or high-risk origin

        Generated: {{DateTime.UtcNow:O}}
        """,
        "Dynamic reference for how the customs team routes packets for review.")
    .AddScript(
        "estimate-duty",
        (decimal declaredValueUsd, decimal dutyRatePercent) =>
        {
            var estimatedDutyUsd = Math.Round(declaredValueUsd * (dutyRatePercent / 100m), 2);
            var formalEntryRecommended = declaredValueUsd >= 2500m;

            return JsonSerializer.Serialize(new
            {
                declaredValueUsd,
                dutyRatePercent,
                estimatedDutyUsd,
                formalEntryRecommended,
            });
        },
        "Estimate customs duty from declared value and duty rate percent.");

var riskTriageSkill = new AgentInlineSkill(
    name: "shipment-risk-triage",
    description: "Assess customs clearance risk and recommend a control lane for a shipment.",
    instructions: """
        Use this skill when the user asks for a customs risk assessment, lane recommendation,
        or escalation decision for a shipment.

        Workflow:
        1. Load this skill before making a risk decision.
        2. Read the risk-indicators resource to ground the assessment.
        3. Run the score-shipment-risk script when shipment facts are available.
        4. Explain the score, the lane recommendation, and the main drivers.
        """)
    .AddResource(
        "risk-indicators",
        """
        Customs risk indicators:
        - High-risk origin or transshipment route
        - Missing certificate of origin or licensing paperwork
        - HS codes associated with dual-use, controlled, or excise-sensitive goods
        - Restricted-party screening hit
        - Declared value inconsistent with commodity profile

        Lane guide:
        - 0-39: Green
        - 40-69: Amber
        - 70+: Red
        """,
        "Reference rubric for customs risk scoring.")
    .AddScript(
        "score-shipment-risk",
        (string originCountry, string hsCode, bool missingCertificateOfOrigin, bool requiresImportLicense, bool restrictedPartyHit) =>
        {
            var riskScore = 10;
            List<string> reasons = [];

            if (originCountry.Equals("Iran", StringComparison.OrdinalIgnoreCase) ||
                originCountry.Equals("North Korea", StringComparison.OrdinalIgnoreCase) ||
                originCountry.Equals("Russia", StringComparison.OrdinalIgnoreCase))
            {
                riskScore += 35;
                reasons.Add("High-risk origin or sanctions-sensitive routing");
            }

            if (hsCode.StartsWith("8542", StringComparison.OrdinalIgnoreCase) ||
                hsCode.StartsWith("8806", StringComparison.OrdinalIgnoreCase))
            {
                riskScore += 20;
                reasons.Add("Commodity family may require closer export/import control review");
            }

            if (missingCertificateOfOrigin)
            {
                riskScore += 20;
                reasons.Add("Missing certificate of origin");
            }

            if (requiresImportLicense)
            {
                riskScore += 25;
                reasons.Add("Import license required for this commodity");
            }

            if (restrictedPartyHit)
            {
                riskScore += 40;
                reasons.Add("Restricted-party screening hit");
            }

            var recommendedLane = riskScore >= 70 ? "Red" : riskScore >= 40 ? "Amber" : "Green";

            return JsonSerializer.Serialize(new
            {
                originCountry,
                hsCode,
                riskScore,
                recommendedLane,
                reasons,
            });
        },
        "Score shipment clearance risk and recommend Green, Amber, or Red lane.");

var fileSkillsPath = Path.Combine(AppContext.BaseDirectory, "skills");
var skillsProvider = new AgentSkillsProviderBuilder()
                     .UseSkill(clearancePacketSkill) 
                     .UseSkill(riskTriageSkill)  
                     .UseFileSkill(fileSkillsPath)
                     .UseFileScriptRunner(SubprocessScriptRunner.RunAsync)
                     .Build();

AIAgent agent = chatClient
    .AsIChatClient()
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "CustomsSkillsAgent",
        ChatOptions = new()
        {
            Instructions = """
                You are a customs clearance copilot.
                Use the registered Agent Skills whenever the user asks for customs procedures, document requirements,
                duty estimates, or lane recommendations. Keep answers concrete and operational.
                """
        },
            AIContextProviders = [skillsProvider],
    });

AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("=============================================================");
Console.WriteLine("  Customs Agent Skills Demo");
Console.WriteLine("=============================================================");
Console.WriteLine("Skills created in this sample:");
Console.WriteLine("- Inline: customs-clearance-packet, shipment-risk-triage");
Console.WriteLine("- File-based: customs-clearance-playbook");
Console.WriteLine();

var prompts = new[]
{
    "What documents must be present before filing customs entry?",
    "What documents are required to clear an electronics shipment into the US, and when would you route it to amber instead of green?",
    "Assess a shipment from Vietnam with HS code 8542.31, declared value 84500 USD, duty rate 6.5%, certificate of origin missing, no restricted-party hit, and no import license requirement.",
};

foreach (var prompt in prompts)
{
    Console.WriteLine($"> {prompt}");
    Console.WriteLine();
}

Console.WriteLine("Try your own prompt, or type 'exit' to quit.");

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    AgentResponse response = await agent.RunAsync(input, session);
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