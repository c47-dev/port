# Implementation Records

This directory stores planning and implementation records for projects that use the `docs/implementation-records` name.

Purpose:

- record the global control document for a meaningful change or initiative
- record per-phase planning and implementation documents when the change is decomposed
- record the purpose of a feature or change
- record the major code changes
- record the test phase and verification state

Authority:

- `docs/spec/*` and `docs/workflow/*` are canonical
- files here are planning/execution artifacts and historical context
- if a planning or implementation record conflicts with a canonical contract, follow the canonical contract

Rules:

- keep one global document per meaningful feature or initiative
- for a `Large` change, add one phase document per executable phase
- keep the phase documents in the same directory family as the global document
- the global document controls initiative scope, phase ordering, dependency, status, and aggregate changelog
- each phase document must contain its own PRD, TDD, test plan, verification contract, and completion criteria
- use this directory for planning and change history, not enduring product or workflow rules
- when a durable rule is discovered here, promote it into `docs/spec/*` or `docs/workflow/*`
