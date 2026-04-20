# 📋 Fundamentals 05: Structured Output

## Overview
This project demonstrates how to get **strongly-typed, structured responses** from agents using JSON schemas. Instead of free-form text, agents return data that matches a predefined schema, enabling type-safe processing and validation.

**Key Learning:** Structured output enables reliable, machine-readable agent responses.

---

## What You'll Learn

- ✅ Define JSON schemas for structured output
- ✅ Use `ChatResponseFormat.ForJsonSchema<T>()` for response formatting
- ✅ Deserialize agent responses to strongly-typed objects
- ✅ Use `RunAsync<T>()` for automatic deserialization
- ✅ Combine streaming with structured output

---

## Core Concepts

### 1. Define a Response Schema

```csharp
public class CustomsClearanceAssessment
{
    public string ShipmentId { get; set; }
    public string Destination { get; set; }
    public string RiskLevel { get; set; }  // Low, Medium, High
    public List<string> RequiredDocuments { get; set; }
    public decimal EstimatedDutyUsd { get; set; }
    public string RecommendedAction { get; set; }
}
```

This class defines the **exact structure** of responses the agent returns.

### 2. Method 1: Using ResponseFormat

```csharp
AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    Name = "CustomsStructuredOutputAgent",
    ChatOptions = new()
    {
        Instructions = "Return only valid JSON matching the schema.",
        ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat
            .ForJsonSchema<CustomsClearanceAssessment>()
    }
});

AgentResponse response = await agent.RunAsync(
    "Assess shipment CSH-3017 to Germany with HS code 854231, " +
    "declared value 125000 USD, duty rate 4.2%");

// response.Text is valid JSON string
string json = response.Text;
var assessment = JsonSerializer.Deserialize<CustomsClearanceAssessment>(json);

Console.WriteLine($"Risk Level: {assessment.RiskLevel}");
Console.WriteLine($"Estimated Duty: ${assessment.EstimatedDutyUsd}");
```

---

### 3. Method 2: Using RunAsync<T> (Type-Safe)

```csharp
AIAgent agent = chatClient.AsAIAgent(
    name: "CustomsTypedOutputAgent",
    instructions: "Return only valid JSON matching the requested schema.");

// Automatic deserialization! ✅
AgentResponse<CustomsClearanceAssessment> response = 
    await agent.RunAsync<CustomsClearanceAssessment>(
        "Assess shipment CSH-4002 to UAE with HS code 870899, " +
        "declared value 98000 USD, duty rate 5.0%");

// No manual parsing needed
var assessment = response.Output;
Console.WriteLine($"Risk: {assessment.RiskLevel}");
Console.WriteLine($"Documents: {string.Join(", ", assessment.RequiredDocuments)}");
```

**Advantage:** Type-safe, no manual deserialization, compile-time checking.

---

### 4. Method 3: Streaming with Structured Output

```csharp
AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "CustomsStreamingStructuredAgent",
    ChatOptions = new()
    {
        Instructions = "Return only valid JSON.",
        ResponseFormat = ChatResponseFormat.ForJsonSchema<CustomsClearanceAssessment>()
    }
});

// Stream JSON structure incrementally
await foreach (var chunk in agent.RunStreamingAsync(
    "Assess shipment CSH-5001 to Canada..."))
{
    Console.Write(chunk);  // Prints JSON chunks as they arrive
}
```

---

## Project Structure

```
04-structured-output/
├── Program.cs              # 3 structured output methods
├── Models/
│   └── CustomsClearanceAssessment.cs  # Response schema definition
├── appsettings.json        # Azure OpenAI config
└── 04-structured-output.csproj
```

---

## Example Response Schema

```csharp
public class CustomsClearanceAssessment
{
    public string ShipmentId { get; set; }
    public string Destination { get; set; }
    public string HsCode { get; set; }
    public string RiskLevel { get; set; }
    public List<string> RequiredDocuments { get; set; }
    public decimal EstimatedDutyUsd { get; set; }
    public string RecommendedAction { get; set; }
}
```

### Generated JSON Schema:
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "shipmentId": { "type": "string" },
    "destination": { "type": "string" },
    "hsCode": { "type": "string" },
    "riskLevel": { "type": "string" },
    "requiredDocuments": { "type": "array", "items": { "type": "string" } },
    "estimatedDutyUsd": { "type": "number" },
    "recommendedAction": { "type": "string" }
  },
  "required": ["shipmentId", "destination", "riskLevel"]
}
```

---

## Example Output

### Input:
```
"Assess shipment CSH-3017 to Germany with HS code 854231, 
 declared value $125,000, duty rate 4.2%"
```

### Output (Strongly Typed):
```csharp
var assessment = new CustomsClearanceAssessment
{
    ShipmentId = "CSH-3017",
    Destination = "Germany",
    HsCode = "854231",
    RiskLevel = "Low",
    RequiredDocuments = new() 
    { 
        "Commercial Invoice", 
        "Packing List", 
        "Bill of Lading",
        "Certificate of Origin"
    },
    EstimatedDutyUsd = 5250.00m,  // 125000 * 0.042
    RecommendedAction = "Proceed with clearance. Green lane processing recommended."
};
```

---

## Three Methods Comparison

| Method | Type Safety | Ease | Streaming | Use When |
|--------|-------------|------|-----------|----------|
| **ResponseFormat** | Partial | Medium | ✅ Yes | Manual control needed |
| **RunAsync<T>** | ✅ Full | ✅ Easy | ❌ No | Standard structured output |
| **Streaming** | Partial | Medium | ✅ Yes | Real-time feedback needed |

---

## Benefits of Structured Output

✅ **Type Safety:** Compile-time error detection
✅ **Validation:** Schema ensures correct format
✅ **Parsing:** No manual JSON string manipulation
✅ **Integration:** Direct use in downstream code
✅ **Reliability:** Guaranteed response format
✅ **Documentation:** Schema is self-documenting

---

## When to Use Structured Output

### ✅ Use for:
- **Domain Models:** Assessment results, shipment data
- **API Responses:** Returns to frontend clients
- **Data Processing:** Further analysis or storage
- **Validation:** Ensure response meets requirements
- **Automation:** Machine-readable decisions

### ❌ Don't use for:
- **Explanations:** Free-form reasoning texts
- **Conversations:** Natural back-and-forth dialogue
- **Streaming Analysis:** Long narrative responses
- **Debugging:** Agent thinking/trace logs

---

## Advanced: Complex Nested Structures

```csharp
public class ShipmentBatch
{
    public string BatchId { get; set; }
    public List<CustomsClearanceAssessment> Shipments { get; set; }
    public decimal TotalDutyUsd { get; set; }
    public Dictionary<string, int> DestinationCounts { get; set; }
}

// Use the same pattern
AgentResponse<ShipmentBatch> response = 
    await agent.RunAsync<ShipmentBatch>("Assess batch of 5 shipments...");
```

---

## Configuration

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<resource>.openai.azure.com/",
    "DeploymentName": "gpt-4o",
    "ApiKey": "your-key-or-managed-identity"
  }
}
```

**Note:** Use `gpt-4o` or `gpt-4-turbo` for structured output support.

---

## Key APIs

| API | Purpose |
|-----|---------|
| `ChatResponseFormat.ForJsonSchema<T>()` | Define schema from class |
| `agent.RunAsync<T>(prompt)` | Execute with typed output |
| `response.Output` | Deserialized typed object |
| `JsonSerializer.Deserialize<T>()` | Manual parsing fallback |

---

## Running the Project

```bash
cd 04-structured-output
dotnet run
```

Output shows three methods of getting structured responses, each producing the same typed object.

---

## Next Steps

- 👉 **Next Project:** [05-reasoning-effort](../05-reasoning-effort/README.md) - Control reasoning depth
- 🔗 **Related:** [01-agent-with-tools](../01-agent-with-tools/README.md) - Tools that return structured data
- 🔗 **Related:** [03-proper-session-multiturn](../03-proper-session-multiturn/README.md) - Structured output in sessions

---

## Best Practices

✅ **Keep schemas focused:** One response concept per class
✅ **Use descriptive names:** Properties should be self-documenting
✅ **Include nullability:** Mark optional fields appropriately
✅ **Add JsonPropertyName:** For complex field mappings
✅ **Version schemas:** Plan for evolution

---

## Example with Session

```csharp
AgentSession session = await agent.CreateSessionAsync();

// Turn 1: Get structured assessment
var response1 = await agent.RunAsync<CustomsClearanceAssessment>(
    "Analyze shipment CSH-1001", session);

// Turn 2: Follow-up with more context
var response2 = await agent.RunAsync<CustomsClearanceAssessment>(
    "The sender is a trusted supplier. Reassess with that context.", session);

// Both responses are strongly typed ✅
Console.WriteLine($"Initial risk: {response1.Output.RiskLevel}");
Console.WriteLine($"Updated risk: {response2.Output.RiskLevel}");
```

