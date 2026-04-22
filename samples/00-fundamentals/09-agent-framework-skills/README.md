# Fundamentals 09: Agent Framework Skills

## Overview

This project demonstrates **Agent Framework Skills** — a way to encapsulate domain knowledge, business logic, and reusable workflows. Skills can be defined inline using the fluent API or loaded from files, enabling modular, maintainable agent behavior.

> **Key Idea:** Skills organize complex agent behavior into reusable, composable units.

---

## What You Will Learn

- Create inline skills with `AgentInlineSkill`
- Add static and dynamic resources (reference materials) to skills
- Add scripts (executable functions) to skills
- Attach skills to agents via `AIContextProviders`
- Combine multiple skills for complex workflows

---

## What Is a Skill?

A skill packages four things together:

| Component | Purpose |
|-----------|---------|
| **Description** | What the skill does |
| **Instructions** | How the agent should use it |
| **Resources** | Reference data (documents, policies) |
| **Scripts** | Executable functions (calculations, decisions) |

---

## Building a Skill

### 1. Define an Inline Skill

```csharp
var clearancePacketSkill = new AgentInlineSkill(
    name: "customs-clearance-packet",
    description: "Guide review of a customs clearance packet, including required documents, " +
                 "routing policy, and duty estimation.",
    instructions: """
        Use this skill when the user asks whether a shipment is document-ready,
        what paperwork is required, or how much duty is likely due.

        Workflow:
        1. Load this skill before answering procedural questions.
        2. Read the relevant resource when the user asks about document requirements.
        3. Run the estimate-duty script when the user provides declared value and duty rate.
        4. Keep the answer operational: missing documents, likely duty, and what to do next.
        """)
    .AddResource(...)  // Add reference materials
    .AddScript(...)    // Add executable scripts
```

---

### 2. Add Static Resources

Static resources provide fixed reference content the agent can read.

```csharp
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
```

---

### 3. Add Dynamic Resources

Dynamic resources are re-generated each time the agent accesses them.

```csharp
.AddResource(
    "lane-selection-policy",
    () => $$"""
        Lane selection policy snapshot (generated {{DateTime.UtcNow:O}}):

        - Green lane: complete packet, low-risk origin, no restricted-party flags
        - Amber lane: document gaps, value anomalies, moderate compliance concerns
        - Red lane: missing control documents, licensing concerns, sanctions hits, high-risk origin

        Last Updated: {{DateTime.UtcNow:O}}
        """,
    "Dynamic reference for how the customs team routes packets for review.")
```

---

### 4. Add Executable Scripts

Scripts are typed functions the agent can invoke for calculations or decisions.

```csharp
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
    "Estimate customs duty from declared value and duty rate percent.")
```

---

### 5. Attach Skills to an Agent

```csharp
var skillsProvider = new AgentSkillsProvider(
    skills: new[] { clearancePacketSkill },
    new AgentContextOptions { /* ... */ });

AIAgent agent = azureClient
    .GetChatClient(deploymentName)
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "CustomsSkillsAgent",
        ChatOptions = new()
        {
            Instructions = "You are a customs operations copilot. Use available skills to help.",
        },
        AIContextProviders = [skillsProvider],  // Attach skills here
    });
```

---

## Combining Multiple Skills

The agent automatically selects the relevant skill based on user queries.

```csharp
var skills = new[]
{
    ClearancePacketSkill,    // Document requirements
    RiskAssessmentSkill,     // Port/origin risk
    DutyCalculationSkill,    // Tariff lookups
    ComplianceCheckSkill,    // Sanctions screening
};

var skillsProvider = new AgentSkillsProvider(skills);

var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    AIContextProviders = [skillsProvider]
});
```

---

## Sample Interaction

**User Query:**
```
"What documents do I need for a shipment valued at $5,000 with 6% duty?"
```

**Agent Processing:**
1. Recognizes question about clearance documentation
2. Activates `customs-clearance-packet` skill
3. Retrieves `required-documents` resource
4. Calls `estimate-duty` script with `(5000, 6.0)`
5. Integrates results into a single response

**Agent Response:**
```
Required Documents:
  ✔ Commercial invoice (with USD pricing)
  ✔ Packing list with weights and carton IDs
  ✔ Bill of lading
  ✔ Entry summary / customs declaration
  ✔ Certificate of origin (if preferential treatment is claimed)
  ✔ Import license (only if commodity is controlled)

Duty Estimation:
  Declared Value:            $5,000.00 USD
  Duty Rate:                 6.0%
  Estimated Duty:            $300.00 USD
  Formal Entry Recommended:  YES (value ≥ $2,500)

Recommended Action:
  File formal entry with a complete packet. You will likely be processed
  in the green lane if your origin has low-risk status.
```

---

## Skills vs Tools vs Middleware

| Feature | Skills | Tools | Middleware |
|---------|--------|-------|------------|
| **Purpose** | Organize domain knowledge | Call external functions | Intercept requests |
| **Scope** | Knowledge + logic | Single function | Cross-cutting concerns |
| **Reusability** | Across agents | Within tool calls | In pipelines |
| **Complexity** | Can be complex workflows | Single operation | Typically simple |
| **Example** | Clearance workflow | `GetWeather()` | Logging |

---

## Key API Reference

| API | Purpose |
|-----|---------|
| `new AgentInlineSkill(name, desc, instructions)` | Create an inline skill |
| `skill.AddResource(name, content, description)` | Add a static resource |
| `skill.AddResource(name, generator, description)` | Add a dynamic resource |
| `skill.AddScript(name, function, description)` | Add an executable script |
| `new AgentSkillsProvider(skills)` | Wrap skills for an agent |
| `AIContextProviders = [skillsProvider]` | Attach skills to an agent |

---

## Skill Starter Template

```csharp
public static AgentInlineSkill CreateMySkill()
{
    return new AgentInlineSkill(
        name: "my-skill-name",
        description: "What this skill helps with",
        instructions: """
            When to use this skill:
            - Situation 1
            - Situation 2

            How to use it:
            1. Step 1
            2. Step 2
            """)
        .AddResource(
            "resource-name",
            "Static reference content",
            "Why this resource matters")
        .AddResource(
            "dynamic-resource",
            () => $"Generated content at {DateTime.Now}",
            "This generates content dynamically")
        .AddScript(
            "calculation",
            (int input) => input * 2,
            "Does a calculation with the input");
}
```

---

## Extra: File-Based Skill Resources

Skills can also load resources from the filesystem at runtime.

```csharp
var policySkill = new AgentInlineSkill(
    name: "customs-policy",
    description: "Reference customs policies",
    instructions: "Use this skill for policy questions")
    .AddResource(
        "tariff-schedule",
        () => File.ReadAllText("policies/tariff-schedule.txt"),
        "Complete tariff schedule")
    .AddResource(
        "compliance-matrix",
        () => File.ReadAllText("policies/compliance-matrix.txt"),
        "Compliance requirements by origin");
```

---

## Practical Tips

- **Keep skills focused** — one domain concept per skill
- **Write clear instructions** — the agent needs to know when and how to use the skill
- **Use resources for reference data** — keep static knowledge accessible and named
- **Keep scripts simple** — complex logic belongs in tools
- **Document thoroughly** — future maintainers will thank you
