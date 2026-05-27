# Plan

## Purpose

Use this phase after the request is clear enough to proceed.

## Required Actions

You are in planning phase only. Do not write code.

- identify the target system and primary changed surface
- locate the governing `docs/spec/*` file
- locate the governing `docs/workflow/*` file when the change is workflow-only or QA-process-only
- identify secondary specs only if they directly constrain the change
- read the governing contract before implementation decisions
- extract the concrete delivery rules that control correctness
- classify whether API QA, browser UI QA, both, or neither are required
- choose the OMX execution surface that matches the later phase
- classify the change as `Small`, `Medium`, or `Large`
- determine whether one executable phase is sufficient or whether multiple ordered phases are required

## Change Scale Classification

Classify every change as exactly one of:

- `Small`
  - one primary changed surface
  - one ownership boundary
  - no schema or workflow restructuring
  - one verification cycle is sufficient
- `Medium`
  - multiple files or subcomponents within a tightly related surface
  - one or two ownership boundaries
  - contract expansion remains locally verifiable
  - one phase is still sufficient
- `Large`
  - multiple changed surfaces or ownership boundaries
  - schema, API, UI, workflow, or migration work must be sequenced
  - later work depends on earlier work becoming explicit and verified
  - separate phase-level verification cycles are required

If the change matches two or more `Large` signals, treat it as `Large`.

## Planning Artifact Rule

- `Small` or `Medium`
  - produce one combined PRD + TDD document for the change
- `Large`
  - do not collapse the entire initiative into one combined PRD + TDD document
  - create one global planning/implementation document in `docs/implementation-records`
  - create one phase document per executable phase in `docs/implementation-records`
  - each phase document must contain its own PRD, TDD, validation plan, test and verify contract, and completion criteria
  - the global document must track phase order, dependency, status, and aggregate changelog

## Required Output

Hard rules:

- Do not assume missing requirements.
- Do not compress unknowns into general statements.
- If any detail is not explicitly confirmed by current repo evidence or user instruction, write it as an open question.
- Do not mark Open Questions as "none blocking" unless you have checked every required section and found no omissions.
- Treat this as a contract document, not a summary.

### Output for `Small` or `Medium`

1. PRD
- Problem statement
- Goals
- Non-goals
- Actors / permissions
- User stories
- Success criteria
- Scope boundaries

2. TDD
- Technical approach
- Component breakdown
- Ownership boundaries
- Data flow
- Failure modes
- Validation strategy
- Test strategy

3. System Architecture
- Text description
- Mermaid diagram
- Browser -> API -> service -> DB flow
- Permission flow
- Read/write ownership

4. API Contract
For every endpoint involved, include:
- method and path
- purpose
- auth requirement
- request JSON schema
- request example
- success response JSON schema
- success example
- error response JSON schema
- validation rules
- status codes
- side effects
- idempotency / conflict behavior

5. Database Contract
For every affected collection/table, include:
- collection/table name
- every field
- type
- required/optional
- default value
- nullability
- unique/index rules
- read/write source
- migration or normalization behavior
- backfill behavior
- rollback impact
- compatibility impact

6. UI Contract
For every affected screen, include:
- route
- purpose
- visible states: loading / empty / error / ready
- create / edit / delete behavior
- save flow
- validation feedback
- disabled states
- dependency behavior if related API is unavailable

7. Operational Concerns
- audit logging
- observability
- concurrency
- retry/idempotency
- data retention/deletion
- performance constraints
- security / authz boundaries

8. Edge Cases
- missing referenced records
- duplicate values
- stale data
- partial failures
- malformed payloads
- unauthorized access
- deletion effects on dependent records

9. Open Questions
- List every unspecified decision
- Mark whether each one blocks implementation or not
- Do not hide gaps behind "none blocking" unless the checklist is truly complete

### Additional Output for `Large`

The global planning/implementation document must include:

1. Initiative overview
- problem statement
- initiative goals
- non-goals
- scope boundary
- success criteria

2. Scale assessment
- why the change is `Large`
- which signals triggered decomposition

3. Phase map
- ordered phase list
- phase name
- phase objective
- phase scope boundary
- phase owner boundary
- phase dependency
- phase exit criteria

4. Cross-phase contract
- dependency matrix
- shared data or API contracts
- sequencing risks
- rollback boundaries

5. Global verification model
- which verification belongs to each phase
- what initiative-level evidence is required before final delivery

6. Aggregate changelog
- planned changes by phase
- completed changes by phase
- deferred items

7. Open Questions
- initiative-level unknowns
- blocking status

Each phase document must include:

1. Phase metadata
- initiative name
- phase number and title
- status
- depends on
- governing spec/workflow authority

2. PRD
3. TDD
4. System Architecture for that phase
5. API / database / UI contract for that phase only
6. Validation plan
7. Test and verify contract
8. Open Questions
9. Phase changelog
10. Completion criteria

Completion rule:

- before finalizing, perform a completeness audit and verify that every entity, endpoint, schema field, UI surface, validation rule, dependency, and error path has been explicitly covered at the correct level
- for `Large` changes, verify both the global document and every phase document are internally complete

## Gate Rule

Do not edit code until the delivery rules, verification classification, and required planning artifacts are explicit.
