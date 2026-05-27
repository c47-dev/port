# Improve

## Purpose

Use this phase for evidence-backed workflow improvement after repeated phase friction is observed, improving the quality for **next session llm agent**.

## Required Actions

- collect every phase traces from completed work
- diagnose every code bug and agent runtime in terminal, MCP, skills execution repeated friction or failure patterns
- base on the action, identify which phases, which workflow documents and lines lead to that direction
- rewrite the relevant workflow authority file so future agents are redirected before repeating the same error, check `Rewrite Rules` section for detail
- keep any raw trace log as evidence only; do not treat logs or sidecar context files as the improvement
- include the error track directly in the workflow authority file that owns the behavior
- validate proposed patches before publishing
- publish validated workflow changes into the correct authority file

## Important Note

- Improve must change the future execution path by rewriting the narrowest workflow authority file that owns the failed behavior.
- Improve phase **is not** a reporting phase OR complete when the agent only writes a log, report, memory, or sidecar runtime context.

## Rewrite Rules
- write the corrected future-run rule into the relevant `docs/workflow/**.md` authority file
- place the error track next to the corrected rule so the next agent sees why the rule exists
- keep raw trace evidence only as supporting evidence, not as the read-back mechanism
- do not publish a separate `.omx/state/*` runtime-context file as the primary fix
- do not use Improve to weaken mandatory API QA, dual review, delivery, or ambiguity-stop rules

## Performance Loop

| Step | Authority |
| --- | --- |
| trace | `docs/workflow/performance-loop/trace-capture.md` |
| diagnose | `docs/workflow/performance-loop/diagnosis.md` |
| patch | `docs/workflow/performance-loop/workflow-patch.md` |
| validate | `docs/workflow/performance-loop/validation.md` |
| publish | `docs/workflow/performance-loop/publish.md` |

## Required Output
Listing each improved issue in following structure
a. source traces reviewed
b. issue classification
c. file self-healing update
-
And write down
1. proposed workflow patch, if any
2. validation result
3. publish status

## Guardrail

The improve phase must not weaken mandatory API QA, dual review, delivery, or ambiguity-stop rules.
