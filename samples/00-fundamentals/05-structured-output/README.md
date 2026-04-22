#  Fundamentals 05: Structured Output

## Quick Context
This project demonstrates how to get **strongly-typed, structured responses** from agents using JSON schemas. Instead of free-form text, agents return data that matches a predefined schema, enabling type-safe processing and validation.

**Point to Remember:** Structured output enables reliable, machine-readable agent responses.

## The Problem with LLM Responses

Let's say we're building a automation system. You ask an AI agent to assess based on the domain context, it returns beautiful English text describing risk levels, required documents, and duties. But now you need to:

- ✗ Parse that text programmatically — format is unpredictable
- ✗ Fragile parsing
- ✗ Validate the data — no type safety
- ✗ Feed it into your database or downstream system

**Traditional approach = brittle, error-prone text parsing.**

## Why Structured Output Helps

 - **Type Safety:** Compile-time error detection
 - **Validation:** Schema ensures correct format
 - **Parsing:** No manual JSON string manipulation
 - **Integration:** Direct use in downstream code
 - **Reliability:** Guaranteed response format
 - **Documentation:** Schema is self-documenting

## Key Methods Used

| API | Purpose |
|-----|---------|
| `ChatResponseFormat.ForJsonSchema<T>()` | Define schema from class |
| `agent.RunAsync<T>(prompt)` | Execute with typed output |
| `response.Output` | Deserialized typed object |
| `JsonSerializer.Deserialize<T>()` | Manual parsing fallback |


## Method Comparison

| Method | Type Safety | Ease | Streaming |  
|--------|-------------|------|-----------| 
| **ResponseFormat** | Partial | Medium |  Yes |  
| **RunAsync<T>** |  Full |  Easy |  No | 
| **Streaming** | Partial | Medium |  Yes |  

--- 

## Steps

### 1. Define a Response Schema

Define as class to the **exact structure** of responses the agent returns. For example, 

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

### 2. - Step 2 Configure response mapping to the agent
 
#### Method 1: Using ResponseFormat

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

#### Method 2: Using RunAsync<T> (Type-Safe)

```csharp
AIAgent agent = chatClient.AsAIAgent(
    name: "CustomsTypedOutputAgent",
    instructions: "Return only valid JSON matching the requested schema.");

// Automatic deserialization! 
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

### Method 3: Streaming with Structured Output

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
 
## Sample Output

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