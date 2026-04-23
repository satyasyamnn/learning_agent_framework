#  Fundamentals 10: C# File-Based Skill Script Runner

[<- Back to Fundamentals Index](../README.md#code-flow-order)

## Quick Context
This project demonstrates how to use **file-based skills** (`SKILL.md` files) executed by a **C# script runner** (`.csx`). This enables non-Python environments to implement skills with executable scripts in C#, without requiring Python or external dependencies.

**Point to Remember:** Skills can be loaded from files and executed dynamically using C# scripting.

---

## Points to Consider

-  Structure and load `SKILL.md` files
-  Create C# `.csx` script files for skill implementation
-  Use `AgentSkillsProvider` to load file-based skills
-  Execute scripts dynamically with Roslyn scripting
-  Combine file-based and inline skills
-  Handle script parameters and return values

---

## Main Ideas

### 1. File-Based Skill Structure

Skills are organized in a dedicated folder:

```
skills/
 csharp-duty-and-lane/
    SKILL.md              # Skill metadata and documentation
    estimate-duty.csx     # Executable C# script
 risk-assessment/
     SKILL.md
     assess-risk.csx
```

---

### 2. SKILL.md Format

```markdown
# Customs Duty and Lane Selection Skill

## What It Does
Estimates customs duty and recommends processing lane (green/amber/red).

## How to Use It
Use this skill when:
- Calculating duty from declared value
- Determining processing lane based on risk
- Providing duty and compliance guidance

## Script List

### estimate-duty
**Purpose:** Calculate duty from declared value and rate
**Parameters:**
- `declaredValue` (decimal): Declared value in USD
- `dutyRate` (decimal): Duty rate as percentage (e.g., 6.5)
**Returns:** JSON with duty estimate and formal entry recommendation
```

---

### 3. Executable C# Script (.csx)

```csharp
// File: estimate-duty.csx
public class DutyEstimate
{
    public decimal DeclaredValueUsd { get; set; }
    public decimal DutyRatePercent { get; set; }
    public decimal EstimatedDutyUsd { get; set; }
    public bool FormalEntryRecommended { get; set; }
}

// Main script execution
var declaredValueUsd = Parameters.declaredValueUsd;  // From skill invocation
var dutyRatePercent = Parameters.dutyRate;

var estimate = new DutyEstimate
{
    DeclaredValueUsd = declaredValueUsd,
    DutyRatePercent = dutyRatePercent,
    EstimatedDutyUsd = Math.Round(
        declaredValueUsd * (dutyRatePercent / 100m), 2),
    FormalEntryRecommended = declaredValueUsd >= 2500m
};

return System.Text.Json.JsonSerializer.Serialize(estimate);
```

---

### 4. Load Skills from Files

```csharp
var skillsPath = Path.Combine(
    AppContext.BaseDirectory, 
    "skills");  // Directory containing SKILL.md files

var skillsProvider = new AgentSkillsProvider(
    skillsPath,
    CSharpFileSkillScriptRunner.RunAsync);  // C# script executor

AIAgent agent = azureClient
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "CustomsCSharpScriptRunnerAgent",
        ChatOptions = new()
        {
            Instructions = "Use skill scripts for duty math and decisions.",
        },
        AIContextProviders = [skillsProvider],
    });
```

---

### 5. Execute Skill Scripts

```csharp
AgentSession session = await agent.CreateSessionAsync();

// User asks agent to use a skill
string userQuery = "Using the csharp-duty-and-lane skill, " +
                   "estimate duty for declared value 84,500 USD at 6.5%.";

AgentResponse response = await agent.RunAsync(userQuery, session);

// Output:
// Skill invoked  estimate-duty.csx runs  Result returned
// "Estimated Duty: $5,492.50
//  Formal Entry Recommended: Yes"
```

---

## Key Methods Used

| API | Purpose |
|-----|---------|
| `new AgentSkillsProvider(path, executor)` | Load skills from directory |
| `CSharpFileSkillScriptRunner.RunAsync()` | Execute .csx script |
| `CSharpScript.Create()` | Roslyn script creation |
| `Script.RunAsync()` | Execute compiled script |


---

## Execution Trace

```
User Query: "Estimate duty for $84,500 at 6.5%"
    
Agent identifies "csharp-duty-and-lane" skill
    
AgentSkillsProvider finds skills/csharp-duty-and-lane/SKILL.md
    
CSharpFileSkillScriptRunner.RunAsync() invoked
    
estimate-duty.csx loaded from disk
    
Roslyn compiles script:
  - Creates ScriptGlobals context
  - Injects parameters
  - Compiles to IL
    
Script executes:
  - EstimatedDuty = 84500 * 0.065 = 5,492.50
  - FormalEntry = 84500 >= 2500 = true
    
Result serialized to JSON:
  {
    "declaredValueUsd": 84500,
    "dutyRatePercent": 6.5,
    "estimatedDutyUsd": 5492.50,
    "formalEntryRecommended": true
  }
    
Agent incorporates result into response
    
Response sent to user
```

---

## Practical Tips

 **Organize skills logically:** Group related scripts
 **Document thoroughly:** SKILL.md is your API
 **Handle errors gracefully:** Scripts should return sensible errors
 **Keep scripts focused:** One calculation per script
 **Version your scripts:** Update SKILL.md when changing behavior 