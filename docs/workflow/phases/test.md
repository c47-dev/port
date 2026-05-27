# Test

## Purpose

Use this phase after development work is in place.

## Progressive Disclosure

Read only the depth that applies to the current role and lane. The authority table below defines the lane files.

## Mandatory Procedure

Run these procedures in order. None of them are optional when applicable.

| Step | Procedure | Authority |
| --- | --- | --- |
| 1 | Sanity | `docs/workflow/qa-test/sanity.md` |
| 2 | QA checklist refresh | `docs/workflow/qa-test/checklist-refresh.md` |
| 3 | API QA | `docs/workflow/qa-test/api-qa.md` |
| 4 | Browser UI QA | `docs/workflow/qa-test/browser-ui-qa.md` |
| 5 | QA report | `docs/workflow/qa-test/qa-report.md` |
| 6 | Dual code review | `docs/workflow/qa-test/code-review.md` |
| 7 | Fix and retest loop | `docs/workflow/qa-test/retest-loop.md` |

## Required Actions

- run sanity checks
- derive or refresh the required QA checklist from the governing spec before spawning sub-agent QA
- spawn required sub-agent QA when API or browser-visible behavior is affected
- sub-agent QA should use smallest model only for executing their checklist
- collect QA evidence from required QA agents
- compile one QA report from the returned evidence
- spawn one sub-agent to run `code-review` and one sub-agent to run `receiving-code-review` after the required QA evidence exists
- classify each required QA lane as `pass`, `fail`, or `blocked`
- classify both review lanes as `pass`, `fail`, or `blocked`
- rerun sanity, the failed QA lane, and both review lanes after fixing issues
- continue until every mandatory test procedure passes or a real blocker exists

## Phase Verification Rule

- verification must close against the planning artifact for the exact scope under test
- for `Small` or `Medium` changes, verify against the single combined PRD + TDD document
- for `Large` changes, verify against the current phase document first
- after a `Large` phase passes, write the phase status and returned evidence back into the global planning/implementation document
- do not treat initiative-level verification as complete until every required phase has passed its own verification contract

## Mandatory Rules

- sanity is mandatory
- required API QA is mandatory, always spawn a sub-agent
- required browser UI QA is mandatory, always spawn a sub-agent
- QA report is mandatory whenever any QA lane runs
- `code-review` and `receiving-code-review` are mandatory for every `Change`
- none of the above procedures are optional or replaceable by self-reasoning

## Required Output

1. sanity checks run
2. sanity pass/fail
3. API QA required: `yes` / `no`
4. browser UI QA required: `yes` / `no`
5. API QA result
6. browser UI QA result
7. QA report status
8. dual code-review result
9. returned evidence
10. whether a QA loop was required
11. which boundary failed, if any
12. planning artifact verified
13. current phase status update, if the change is `Large`

## Completion Rule

Test passes only when all applicable mandatory procedures pass with fresh evidence.

For a `Large` change, a phase passes only when:

- the current phase verification contract passes with fresh evidence
- the phase status is updated in the global planning/implementation document
- no required lane for that phase remains unresolved

## Error Track

Observed failure patterns:

- repeated QA runs stayed on the same already-passing boundary instead of moving to the first failing boundary
- live runtime mismatches were not checked before editing code
- tool failures were not treated as signals to switch tools or validate the environment

Corrected future behavior:

- if auth fails, verify the active auth client, secret, and environment before editing auth code
- if health checks or service discovery fail, verify the real runtime port or endpoint before changing client configuration
- if create succeeds but the UI state is stale, verify persistence and read-model alignment before rerunning the same create flow
- if a downstream request times out, inspect the first failing downstream boundary instead of rewriting already-proven upstream code
- if a model or external dependency fails, classify it as a downstream runtime failure rather than misattributing it to the browser-facing layer
- if browser QA fails after an earlier step passed, inspect the actual visible state and selector path before assuming an API failure
