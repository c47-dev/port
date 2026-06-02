# Improve

## Purpose

Use this phase for evidence-backed workflow improvement after repeated phase friction is observed, improving the quality for **next session llm agent**.

## Required Actions

- collect every phase traces from completed work
- diagnose every code bug and agent runtime in terminal, MCP, skills execution repeated friction or failure patterns
- base on the action, identify which phases, which workflow documents and lines lead to that direction
- rewrite the relevant workflow authority file so future agents are redirected before repeating the same error, check `Rewrite Rules` section for detail
- keep any raw trace log as evidence only; do not treat logs or sidecar context files as the improvement
- include the error track directly in the workflow authority file that owns the behavior
- validate proposed patches before publishing
- publish validated workflow changes into the correct authority file

## Important Note

- Improve must change the future execution path by rewriting the narrowest workflow authority file that owns the failed behavior.
- Improve phase **is not** a reporting phase OR complete when the agent only writes a log, report, memory, or sidecar runtime context.

## Rewrite Rules
- write the corrected future-run rule into the relevant `docs/workflow/**.md` authority file
- place the error track next to the corrected rule so the next agent sees why the rule exists
- keep raw trace evidence only as supporting evidence, not as the read-back mechanism
- do not publish a separate `.omx/state/*` runtime-context file as the primary fix
- do not use Improve to weaken mandatory API QA, dual review, delivery, or ambiguity-stop rules

## Performance Loop

| Step | Authority |
| --- | --- |
| trace | `docs/workflow/performance-loop/trace-capture.md` |
| diagnose | `docs/workflow/performance-loop/diagnosis.md` |
| patch | `docs/workflow/performance-loop/workflow-patch.md` |
| validate | `docs/workflow/performance-loop/validation.md` |
| publish | `docs/workflow/performance-loop/publish.md` |

## Required Output
Listing each improved issue in following structure
a. source traces reviewed
b. issue classification
c. file self-healing update
-
And write down
1. proposed workflow patch, if any
2. validation result
3. publish status

## Guardrail

The improve phase must not weaken mandatory API QA, dual review, delivery, or ambiguity-stop rules.

---

## Published improvement — GlassRoundButton liquid-glass (2026-06-01)

### a. Source traces reviewed

- User report: hover “no visible difference”; screenshot showed flat gray ring, no scale/glass vs Apple liquid-glass reference.
- Repeated background shell tasks: exit `4294967295` (hung/killed `dotnet exec`), exit `1` (MSB3027 `PortCheck.exe` locked by running tray), empty or missing `%TEMP%\portcheck-glass-round-validate\glass-round-button-report.txt`.
- Harness report before fix: `FAIL` — `XamlParseException` on `Height="42%"` in `Themes/GlassRoundButton.xaml`.
- Post-fix harness: `PASS` via `dotnet exec …/PortCheck.dll --validate-glass-round-button` (~2s, no tray/Docker).

### b. Issue classification

| Class | What happened |
| --- | --- |
| Product bug | Animator never wired: `Template.FindName` failed for transforms inside `TransformGroup` → no hover scale/lens/specular. |
| Product bug | Template parse failure on `%` heights in some locales → control template could not apply. |
| Product risk | `GlassRoundLensEffect` / screen capture on hover caused UI-thread hang or black lens; shader removed from runtime path. |
| Agent/process | Delivered UI changes without a green harness report; user tested stale `PortCheck.exe` while build was locked. |
| Agent/process | Validation entry ran after `base.OnStartup` + full DI → hung on Docker init; looked like “validation broken”. |

### c. File self-healing update (workflow authority)

| File | Change |
| --- | --- |
| `docs/workflow/phases/test.md` | Error track: harness `PASS` file required, kill `PortCheck` before build, hung-shell vs product failure, hover symptom → `FindName`/animator. |
| `docs/workflow/qa-test/sanity.md` | Allowed check: `--validate-glass-round-button` with report `PASS`. |
| `docs/workflow/phases/develop.md` | Error track: WPF template rules (no `TransformGroup` names, no `%` on `Border.Height`, shader caution). |

Code/product fixes (for traceability, not workflow authority): `Themes/GlassRoundButton.xaml` nested scale/gel grids; dip heights; `App.xaml.cs` early harness exit + skip DI; `Validation/GlassRoundButtonHarness.cs` parts + hover motion checks.

### 1. Proposed workflow patch

Published into the three files above (no separate `.omx/state` sidecar).

### 2. Validation result

- Re-ran `dotnet build` + `dotnet exec … --validate-glass-round-button` → exit `0`, report `PASS`.
- Confirmed report path uses `Path.GetTempPath()` (on this machine often `C:\Temp\…`, not only `%USERPROFILE%\AppData\Local\Temp`).

### 3. Publish status

**Published** — workflow authority updated 2026-06-01. Mandatory API QA, dual review, and delivery gates unchanged.
