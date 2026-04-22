#  Fundamentals 09: Agent Framework Skills

## Quick Context
This project demonstrates **Agent Framework Skills**a way to encapsulate domain knowledge, business logic, and reusable workflows. Skills can be defined inline using the fluent API or loaded from files, enabling modular, maintainable agent workflows.

**Point to Remember:** Skills organize complex agent behavior into reusable, composable units.

---

## Points to Consider

-  Create inline skills with `AgentInlineSkill`
-  Add resources (reference materials) to skills
-  Add scripts (executable functions) to skills
-  Use skills with agents through `AIContextProviders`
-  Combine multiple skills for complex workflows
-  Generate dynamic content in skills

---

## Main Ideas

### What is a Skill?

A skill packages:
- **Description:** What the skill does
- **Instructions:** How the agent should use it
- **Resources:** Reference data (documents, policies)
- **Scripts:** Executable functions (calculations, decisions)

---

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

Dynamic resources are generated each time they're accessed.

---

### 4. Add Executable Scripts

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

Scripts are functions the agent can invoke for calculations or decisions.

---

### 5. Attach Skills to Agent

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

## Folder Layout

```
09-agent-framework-skills/
 Program.cs              # Inline skills demo
 Skills/
    ClearancePacketSkill.cs  # Defined as fluent skill
    RoutingDecisionSkill.cs  # Another skill example
 appsettings.json        # Azure OpenAI config
 09-agent-framework-skills.csproj
```

---

## Sample Interaction

### User Query:
```
"What documents do I need for a shipment valued at $5,000 with 6% duty?"
```

### Agent Processing:
```
1. Recognizes question about clearance documentation
2. Activates "customs-clearance-packet" skill
3. Retrieves "required-documents" resource
4. Calls "estimate-duty" script with (5000, 6.0)
5. Integrates results into response
```

### Response:
```
Based on the clearance packet skill:

**Required Documents:**
 Commercial invoice (with USD pricing, not required for formal entry)
 Packing list with weights and carton IDs
 Bill of lading
 Entry summary/customs declaration
 Certificate of origin (not needed for routine imports)
 Import license (not required unless commodity is controlled)

**Duty Estimation:**
- Declared Value: $5,000 USD
- Duty Rate: 6.0%
- Estimated Duty: $300.00 USD
- Formal Entry Recommended:  YES (value  $2,500)

**Recommended Action:**
File formal entry with complete packet. You'll likely be processed in the green lane
if your origin has low-risk status.
```

---

## Combining Skills

### Combining Multiple Skills

```csharp
var skills = new[]
{
    ClearancePacketSkill,        // Document requirements
    RiskAssessmentSkill,         // Port/origin risk  
    DutyCalculationSkill,        // Tariff lookups
    ComplianceCheckSkill,        // Sanctions screening
};

var skillsProvider = new AgentSkillsProvider(skills);

var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    AIContextProviders = [skillsProvider]
});
```

The agent automatically selects relevant skills based on user queries.

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

## Where Skills Help

 **Domain Workflows:**
- Customs clearance procedures
- Supply chain optimization
- Risk assessment frameworks

 **Reference Materials:**
- Tariff schedules
- Policy documents
- Compliance checklists

 **Calculations:**
- Duty estimation
- Cost calculations
- Performance metrics

 **Decision Logic:**
- Green/amber/red lane routing
- Risk scoring
- Approval workflows

---

## Skills vs Tools vs Middleware

| Feature | Skills | Tools | Middleware |
|---------|--------|-------|------------|
| **Purpose** | Organize domain knowledge | Call external functions | Intercept requests |
| **Scope** | Knowledge + logic | Single function | Cross-cutting |
| **Reusability** | Across agents | In tools | In pipelines |
| **Complexity** | Can be complex workflows | Single operation | Typically simple |
| **Example** | Clearance workflow | GetWeather() | Logging |

---

## Folder Layout

```
09-agent-framework-skills/
 Program.cs              # Main entry
 Skills/
    ClearancePacketSkill.cs
    RiskAssessmentSkill.cs
    SkillFactory.cs
 Models/
    ShipmentData.cs
    ClearancePacket.cs
 appsettings.json
 09-agent-framework-skills.csproj
```

---

## Key Methods Used

| API | Purpose |
|-----|---------|
| `new AgentInlineSkill(name, desc, instructions)` | Create inline skill |
| `skill.AddResource(name, content, description)` | Add static reference |
| `skill.AddResource(name, generator, description)` | Add dynamic reference |
| `skill.AddScript(name, function, description)` | Add executable script |
| `new AgentSkillsProvider(skills)` | Wrap skills for agent |
| `AIContextProviders = [skillsProvider]` | Attach to agent |

---

## Setup

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<resource>.openai.azure.com/",
    "DeploymentName": "gpt-4o-mini",
    "ApiKey": "your-key-or-managed-identity"
  }
}
```

---

## Run It

```bash
cd 09-agent-framework-skills
dotnet run
```

Observe how the agent uses skills to answer complex operational questions.

---

## Extra: File-Based Skill Resources

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

## Try Next

-  **Next Project:** [10-csharp-file-script-runner](../10-csharp-file-script-runner/README.md) - File-based skills with C# scripts
-  **Related:** [01-agent-with-tools](../01-agent-with-tools/README.md) - Tools vs skills
-  **Related:** [06-middleware-usage](../06-middleware-usage/README.md) - Skills + middleware

---

## Practical Tips

 **Keep skills focused:** One domain concept per skill
 **Make instructions clear:** Agent needs to understand when to use skill
 **Use resources for reference:** Keep static data accessible
 **Keep scripts simple:** Complex logic belongs in tools
 **Document thoroughly:** Future maintainers will thank you




