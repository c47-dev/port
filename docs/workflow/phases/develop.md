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

## Error Track (WPF control templates)

Observed failure pattern (GlassRoundButton liquid-glass hover, 2026-06-01):

- `PART_Scale` / `PART_GelFollow` were named inside a single `TransformGroup`; `Template.FindName` often returned null, so `GlassRoundButtonInteractionAnimator` never attached and hover looked unchanged.
- `Height="42%"` / `28%"` on template `Border` elements threw `XamlParseException` under some cultures (`LengthConverter` rejects `%` for `Border.Height`).
- Pixel `ShaderEffect` on 32×32 lens was removed after freezes/black frames; delivery still claimed “glass” without running the harness.
- Agents reported validation “failed” with exit `4294967295` while `PortCheck.exe` held file locks or the app fell through to Docker/tray init instead of the harness.

Corrected future behavior:

- one named transform per element (nested grids), not multiple named transforms inside `TransformGroup`.
- use fixed dip heights in small round templates, not percentage lengths on `Border.Height`.
- do not re-enable round-button pixel shaders without a harness case that proves no UI-thread hang.
- after template or animator edits, run `--validate-glass-round-button` and read the `PASS` report before telling the user to retest UI.
