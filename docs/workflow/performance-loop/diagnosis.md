# Diagnosis

## Purpose

Classify repeated phase friction before proposing workflow changes.

## Data Contract

```ts
interface PhaseDiagnosis {
  task_id: string;
  phase: HarnessPhase;
  issue_type:
    | "ambiguity"
    | "missing-spec"
    | "stale-spec"
    | "path-conflict"
    | "qa-gap"
    | "slow-tooling"
    | "repeated-failure"
    | "review-finding";
  evidence: string[];
  proposed_change_boundary: "agents" | "workflow" | "spec" | "skill" | "tooling";
}
```

## Rules

- diagnose from captured trace evidence
- prefer repeated evidence over single-task observations
- classify uncertainty as a blocker instead of guessing intent
- do not propose workflow changes that weaken mandatory QA, code review, or delivery gates

## Required Output

1. issue type
2. affected phase
3. evidence
4. proposed change boundary
5. confidence or blocker
