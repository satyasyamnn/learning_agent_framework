---
name: csharp-duty-and-lane
description: Customs helper skill that estimates duty and suggests handling path for simple declaration checks.
---

Use this skill when the user asks for duty estimates, basic customs cost checks, or whether a declaration might require formal handling.

## Scripts

### estimate-duty
**Purpose:** Calculate estimated duty from declared value and duty rate.

**Parameters:**
- `declaredValueUsd` (number): The declared value in USD
- `dutyRatePercent` (number): The duty rate as a percentage (e.g., 6.5 for 6.5%)

**Returns:** JSON object containing:
- `declaredValueUsd`: The input declared value
- `dutyRatePercent`: The input duty rate
- `estimatedDutyUsd`: Calculated duty amount (rounded to 2 decimals)
- `formalEntryLikely`: Boolean indicating if value >= $2,500 (requires formal entry)

**Example:** For $84,500 at 6.5%, returns estimated duty of $5,492.50
