# PortCheck Project Instructions

This project runs with a harness workflow. Agents must follow the workflow contracts in `docs/workflow/`.

## Project Overview

- **Stack**: .NET 8 WPF Windows tray desktop app
- **Package**: `PortCheck`
- **Source**: `src/PortCheck/`
- **Specs**: `docs/spec/*.md`
- **Implementation Records**: `docs/implementation-records/*.md`
- **Workflow**: `docs/workflow/*.md`

## Document Authority

- `docs/spec/*` and `docs/workflow/*` are the canonical contracts.
- `docs/implementation-records/*` store planning history, phase documents, and verification state.
- Do not treat `docs/implementation-records/*` as the default source of truth when they conflict with canonical contracts.

## Request Router

Classify every user request into exactly one lane:

1. `Query` - answer from repository files only; do not change files
2. `Change` - any request that requires writing, refactoring, moving, deleting, or updating repository state; enter the harness workflow automatically
3. `Ambiguous` - ask the minimum question needed to decide between `Query` and `Change`

Rules:

- do not require the user to name a skill
- do not ask for confirmation when the request is already a clear `Change`
- if the request can be grounded in repository facts, ground it before asking
- if the request is materially branching, stop after the minimum clarification

## Phase Model

Harness Engineering uses these phases in order:

`Clarify -> Plan -> Develop -> Test -> Deliver -> Improve`

Use the phase documents under `docs/workflow/phases/` as the contract.

## Delegation Rules

Use agents only for bounded, independent work:

- exploration
- implementation
- QA

Leader responsibilities:

- keep the user-facing brief current
- assign bounded work with explicit ownership
- integrate results
- own final verification

Worker responsibilities:

- stay inside assigned scope
- do not widen scope silently
- report blockers, shared-file conflicts, or missing authority upward

## Execution Rules

- Keep diffs small, reviewable, and reversible.
- Prefer deletion over addition.
- Reuse existing patterns before introducing new ones.
- No new dependencies without explicit request.
- Keep the same task open until the requested scope is finished and verified.
- Do not deliver partial work as final.
- Never self-approve in the same active context; use independent review lanes.

## QA Defaults

This app has no browser login surface.

- Classify **API QA** as `no` unless the change adds an HTTP or remote API boundary.
- Classify **browser UI QA** as `no`; verify tray icon, popup, search, kill confirm, and keyboard shortcuts on Windows when UI behavior changes.
- **Sanity**: `dotnet build` under `src/PortCheck`; for kill behavior, use a Release publish or elevated shell and record elevation state in evidence.
- **Kill testing**: use disposable listeners only; do not terminate unrelated system processes.
