# Trace Capture

## Purpose

Capture phase-level evidence so workflow performance can be improved from real task history.

## Data Contract

```ts
type HarnessPhase =
  | "clarify"
  | "plan"
  | "develop"
  | "test"
  | "deliver"
  | "improve";

type PhaseVerdict = "pass" | "fail" | "blocked";

interface PhaseTrace {
  task_id: string;
  phase: HarnessPhase;
  target_system?: string;
  governing_contract?: string;
  workflow_docs_read: string[];
  actions: string[];
  outputs: string[];
  verdict: PhaseVerdict;
  blockers: string[];
  rework_count: number;
  elapsed_ms?: number;
}
```

## Capture Rules

- capture traces at phase boundaries
- include only evidence needed to diagnose workflow performance
- do not store secrets, credentials, or sensitive payloads
- record blockers explicitly instead of inferring intent

## Required Output

1. task id
2. phase
3. governing contract, if known
4. actions and outputs
5. verdict
6. blockers
7. rework count
