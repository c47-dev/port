# PortCheck Project Instructions

This repository follows a workflow-governed desktop application model.

## Project Model

PortCheck is a Windows system-tray app that lists local TCP listening ports and terminates processes. UI is WPF; platform work uses Win32 APIs behind service boundaries.

Code structure:

| Area | Purpose |
| --- | --- |
| `src/PortCheck/*.xaml` | WPF views and windows |
| `src/PortCheck/ViewModels/*` | UI state, commands, filtering, refresh timer |
| `src/PortCheck/Services/*` | Port scan and process termination |
| `src/PortCheck/Models/*` | Domain models (`PortInfo`, etc.) |
| `src/PortCheck/Helpers/*` | Blur, tray position, elevation helpers |
| `docs/spec/*` | Product and system behavior contracts |
| `docs/implementation-records/*` | Planning and implementation records; not default source of truth |
| `docs/workflow/*` | Harness workflow contracts |

Code style rules:

- Prefer existing repository patterns over new abstractions.
- Keep changes grouped by ownership boundary (view, view model, service, helper).
- Do not mix unrelated product surfaces in one patch.
- Update governing specs before code when behavior contracts change.
- Keep UI behavior, service behavior, and QA evidence aligned with the governing spec.
- Do not introduce fallback mechanisms, defensive duplication, or new dependencies without explicit need.
- For tray or popup behavior, verify the real desktop UI path when QA classification requires it.

Architecture rules:

- Views and code-behind bind to view models; they do not call Win32 or process APIs directly.
- View models orchestrate user actions and surface state; they call services for scan and kill work.
- Services own port enumeration and process termination; helpers stay stateless unless a spec says otherwise.
- Win32 and OS calls stay inside services or helpers, not in view models or XAML code-behind except thin view lifecycle glue.
- `docs/spec/*` and `docs/workflow/*` are canonical authority. Planning/implementation records are execution artifacts, not enduring product authority.
- `docs/implementation-records/*` is the planning and implementation entrypoint for agents.

## Phase Model

Harness Engineering uses these semantic phases:

| Phase | Entrypoint |
| --- | --- |
| `Clarify` | `docs/workflow/phases/clarify.md` |
| `Plan` | `docs/workflow/phases/plan.md` |
| `Develop` | `docs/workflow/phases/develop.md` |
| `Test` | `docs/workflow/phases/test.md` |
| `Deliver` | `docs/workflow/phases/deliver.md` |
| `Improve` | `docs/workflow/phases/improve.md` |

Rules:

- execute phases in order for every `Change`
- stop at `Clarify` when the request is materially ambiguous
- do not edit code until `Plan` has identified the governing contract, delivery rules, verification classification, and required planning artifacts
- enter `Develop` through exactly one owner
- do not deliver while mandatory verification is unresolved
- use `Improve` for evidence-backed workflow improvement

## Skill-To-Phase Mapping

Detailed rules: `docs/workflow/skill-to-phase-map.md`

## QA Authority

Detailed rules: `docs/workflow/phases/test.md`

Required lane files:

- checklist refresh
- sanity
- API QA (classify `no` unless an HTTP/API surface is added)
- browser UI QA (classify `no` for this desktop app; verify tray/popup UI manually or with desktop automation when UI behavior changes)
- QA report
- dual code review
- retest loop

Rules:

- required QA must use QA subagents when a lane is classified `yes`
- main-agent self-reasoning must not replace required QA evidence
- QA report is mandatory whenever any QA lane runs
- code review and receiving-code-review are mandatory for every `Change`
- Release kill flows require an elevated process; document elevation state in test evidence

## Delivery Rule

Do not return final delivery until the requested scope is finished and every required verification procedure has passed or a real blocker exists.
