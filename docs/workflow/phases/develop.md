# Develop

## Purpose

Use this phase only after planning is complete.

## Required Actions

- update the governing spec first when contract changes
- make code changes grouped by boundary
- keep unrelated boundaries out of the same patch
- choose and call exactly one execution owner before any code edit:
  - `ralph` for one-owner execution
  - `team` for multi-lane execution
- keep the same task open until the requested scope is finished

## Planning Prerequisite

Before development starts, verify that the planning artifacts required by `docs/workflow/phases/plan.md` already exist.

- for `Small` or `Medium` changes, the combined PRD + TDD document must exist in the project's planning-record directory
- for `Large` changes, both of these must exist in the same planning-record directory:
  - one global planning/implementation document
  - the current phase document for the phase being executed
- do not start code changes for a later phase until the earlier required phase boundary and exit criteria are explicit
- only execute one phase scope at a time unless the governing plan explicitly allows parallel independent phases

## Required Output

1. changed files
2. changed boundaries
3. selected execution owner: `ralph` / `team`
4. development status
5. planning artifact used
6. current phase, if the change is `Large`

## Ownership Rule

Do not start development without calling either `ralph` or `team`.
