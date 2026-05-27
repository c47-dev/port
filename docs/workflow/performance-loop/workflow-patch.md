# Workflow Patch

## Purpose

Convert validated diagnoses into specific workflow patch proposals.

## Data Contract

```ts
interface WorkflowPatch {
  patch_id: string;
  source_diagnoses: string[];
  target_files: string[];
  rationale: string;
  validation_required: string[];
  status: "proposed" | "validated" | "rejected" | "published";
}
```

## Rules

- every patch must point to source diagnoses
- every patch must name target files
- patches must be small, reviewable, and reversible
- patches must preserve existing mandatory verification gates
- patches based on one task require explicit user approval before publishing

## Required Output

1. patch id
2. source diagnoses
3. target files
4. rationale
5. validation required
6. proposed status
