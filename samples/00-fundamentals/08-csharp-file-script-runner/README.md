# 🧾 Fundamentals 09: C# File-Based Skill Script Runner

## Overview
This project demonstrates how to use **file-based skills** (`SKILL.md` files) executed by a **C# script runner** (`.csx`). This enables non-Python environments to implement skills with executable scripts in C#, without requiring Python or external dependencies.

**Key Learning:** Skills can be loaded from files and executed dynamically using C# scripting.

---

## What You'll Learn

- ✅ Structure and load `SKILL.md` files
- ✅ Create C# `.csx` script files for skill implementation
- ✅ Use `AgentSkillsProvider` to load file-based skills
- ✅ Execute scripts dynamically with Roslyn scripting
- ✅ Combine file-based and inline skills
- ✅ Handle script parameters and return values

---

## Core Concepts

### 1. File-Based Skill Structure

Skills are organized in a dedicated folder:

```
skills/
├── csharp-duty-and-lane/
│   ├── SKILL.md              # Skill metadata and documentation
│   └── estimate-duty.csx     # Executable C# script
└── risk-assessment/
    ├── SKILL.md
    └── assess-risk.csx
```

---

### 2. SKILL.md Format

```markdown
# Customs Duty and Lane Selection Skill

## Description
Estimates customs duty and recommends processing lane (green/amber/red).

## Instructions
Use this skill when:
- Calculating duty from declared value
- Determining processing lane based on risk
- Providing duty and compliance guidance

## Scripts

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
// Skill invoked → estimate-duty.csx runs → Result returned
// "Estimated Duty: $5,492.50
//  Formal Entry Recommended: Yes"
```

---

## Project Structure

```
08-csharp-file-script-runner/
├── Program.cs              # Main entry with skill execution loop
├── skills/
│   ├── csharp-duty-and-lane/
│   │   ├── SKILL.md        # Skill definition and documentation
│   │   ├── estimate-duty.csx
│   │   └── recommend-lane.csx
│   └── risk-assessment/
│       ├── SKILL.md
│       └── assess-risk.csx
├── CSharpFileSkillScriptRunner.cs  # Roslyn-based script executor
├── appsettings.json        # Azure OpenAI config
└── 08-csharp-file-script-runner.csproj
```

---

## Example Interaction Flow

### 1. User Query:
```
"Using the csharp-duty-and-lane skill, estimate duty for $84,500 at 6.5%"
```

### 2. Agent Processing:
```
1. Agent recognizes skill reference "csharp-duty-and-lane"
2. Loads SKILL.md from disk
3. Identifies "estimate-duty" script needs to run
4. Prepares parameters: declaredValue=84500, dutyRate=6.5
```

### 3. Script Execution:
```
1. CSharpFileSkillScriptRunner loads estimate-duty.csx
2. Roslyn compiler compiles and executes the script
3. Parameters passed via injected context
4. Script calculates: 84500 * (6.5 / 100) = 5,492.50
5. Returns JSON result
```

### 4. Agent Response:
```
"Based on the duty calculation skill:

Declared Value: $84,500 USD
Duty Rate: 6.5%
**Estimated Duty: $5,492.50**

Formal Entry Recommended: ✅ YES (value > $2,500)

Processing Lane: Green Lane
- Complete documentation required
- No sanctions flags expected
- Proceed with standard clearance"
```

---

## Script Runner Implementation

```csharp
public class CSharpFileSkillScriptRunner
{
    public static async Task<string> RunAsync(
        string scriptPath,
        Dictionary<string, object> parameters)
    {
        // 1. Read .csx file
        var scriptContent = await File.ReadAllTextAsync(scriptPath);
        
        // 2. Create Roslyn script context with parameters
        var script = CSharpScript.Create(scriptContent, 
            globalsType: typeof(ScriptGlobals));
        
        // 3. Compile and execute
        var result = await script.RunAsync(
            new ScriptGlobals { Parameters = parameters });
        
        // 4. Return result as string
        return result.ReturnValue?.ToString() ?? "";
    }
}
```

---

## SKILL.md Template

```markdown
# Your Skill Name

## Description
Brief description of what this skill does.

## Instructions
When to use this skill and how the agent should apply it.

## Resources

### resource-name
Reference material for the agent.

## Scripts

### script-name-1
**Purpose:** What this script does
**Parameters:**
- `param1` (type): Description
- `param2` (type): Description
**Returns:** Description of return value

Example:
```csharp
// Usage example
```

### script-name-2
**Purpose:** Another script
**Parameters:**
- `x` (int): Input value
**Returns:** Processed result
```

---

## C# Script Template (.csx)

```csharp
#r "System.Text.Json"
using System.Text.Json;

public class ScriptGlobals
{
    public Dictionary<string, object> Parameters { get; set; }
}

// Your script logic here
public string ProcessData(dynamic parameters)
{
    var inputValue = parameters.input;
    var processed = inputValue * 2;
    return JsonSerializer.Serialize(new { result = processed });
}

// Entry point
var parameters = ((ScriptGlobals)globals).Parameters;
var result = ProcessData(parameters);
return result;
```

---

## Advantages Over Python Scripts

✅ **No Python Dependency:**
- Skills run on Windows machines without Python
- One runtime (C#/.NET)
- No cross-platform complexity

✅ **Type Safety:**
- C# is strongly typed
- Compile-time checks
- IDE support

✅ **Performance:**
- .NET JIT compilation
- Faster than Python scripts
- Native performance

✅ **Integration:**
- Direct access to .NET libraries
- Easy integration with host app
- Share types between agent and scripts

---

## Key APIs

| API | Purpose |
|-----|---------|
| `new AgentSkillsProvider(path, executor)` | Load skills from directory |
| `CSharpFileSkillScriptRunner.RunAsync()` | Execute .csx script |
| `CSharpScript.Create()` | Roslyn script creation |
| `Script.RunAsync()` | Execute compiled script |

---

## Comparison: Script Runners

| Feature | Python | PowerShell | C# (.csx) |
|---------|--------|-----------|-----------|
| **Dependency** | Python runtime | PowerShell | .NET/Roslyn |
| **Performance** | Slower | Medium | ✅ Fastest |
| **Integration** | Import modules | Cmdlets | Direct .NET |
| **Platform** | Cross-platform | Windows-focused | .NET Core |
| **Type Safety** | Weak | Weak | ✅ Strong |

---

## Configuration

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

## Directory Structure

Ensure this folder structure exists:

```
bin/
├── Debug/
│   ├── net10.0/
│   │   └── skills/           ← Skills directory (copy during build)
│   │       ├── csharp-duty-and-lane/
│   │       │   ├── SKILL.md
│   │       │   └── *.csx
│   │       └── ...
```

**Build Configuration:** Add to `.csproj`:
```xml
<ItemGroup>
  <CopyToOutputDirectory Include="skills/**" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

---

## Running the Project

```bash
cd 08-csharp-file-script-runner
dotnet run
```

Try these queries:
```
> Using the csharp-duty-and-lane skill, estimate duty for $50,000 at 4.5%
> For $1,200 at 3.2%, estimate duty and recommend entry type
> What's the recommended lane for a $150,000 shipment from Germany?
```

---

## Script Execution Trace

```
User Query: "Estimate duty for $84,500 at 6.5%"
    ↓
Agent identifies "csharp-duty-and-lane" skill
    ↓
AgentSkillsProvider finds skills/csharp-duty-and-lane/SKILL.md
    ↓
CSharpFileSkillScriptRunner.RunAsync() invoked
    ↓
estimate-duty.csx loaded from disk
    ↓
Roslyn compiles script:
  - Creates ScriptGlobals context
  - Injects parameters
  - Compiles to IL
    ↓
Script executes:
  - EstimatedDuty = 84500 * 0.065 = 5,492.50
  - FormalEntry = 84500 >= 2500 = true
    ↓
Result serialized to JSON:
  {
    "declaredValueUsd": 84500,
    "dutyRatePercent": 6.5,
    "estimatedDutyUsd": 5492.50,
    "formalEntryRecommended": true
  }
    ↓
Agent incorporates result into response
    ↓
Response sent to user
```

---

## Next Steps

- 👉 **Back to Fundamentals:** [README.md](#master-readme) - Overview of all projects
- 🔗 **Related:** [07-agent-framework-skills](../07-agent-framework-skills/README.md) - Inline skills
- 🔗 **Related:** [01-agent-with-tools](../01-agent-with-tools/README.md) - Tools vs skills

---

## Best Practices

✅ **Organize skills logically:** Group related scripts
✅ **Document thoroughly:** SKILL.md is your API
✅ **Handle errors gracefully:** Scripts should return sensible errors
✅ **Keep scripts focused:** One calculation per script
✅ **Version your scripts:** Update SKILL.md when changing behavior

