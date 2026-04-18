---
name: csharp-duty-and-lane
description: Customs helper skill that estimates duty and suggests handling path for simple declaration checks.
---

Use this skill when the user asks for duty estimates, basic customs cost checks, or whether a declaration might require formal handling.

Workflow:
1. Load this skill.
2. Run `scripts/estimate-duty.csx` when declared value and duty rate are present.
3. Explain estimated duty and whether formal entry is likely.
