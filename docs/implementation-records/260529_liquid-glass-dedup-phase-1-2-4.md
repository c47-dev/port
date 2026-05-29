# Liquid Glass UI Dedup — Phase 1, 2, 4

**Status:** Complete (Phases 1–4)  
**Governing UI spec:** `docs/spec/liquid-glass-uiux.md`  
**Product spec:** `docs/spec/portcheck.md` (unchanged behavior)

## Scope

| Phase | Topic | This delivery |
| --- | --- | --- |
| **1** | Remove dead global tokens | Yes |
| **2** | Consolidate port list `ListBoxItem` styles | Yes |
| **3** | Remove legacy row kill/dismiss styles | Yes |
| **4** | Single glass material stack via `GlassPopupShell` | Yes |

---

## Point 1 — Dead tokens

### Items

| Item | Resource keys |
| --- | --- |
| 1a | `PaneTab.Local.RowHover.Fill`, `PaneTab.Local.RowHover.Stroke` |
| 1b | `PaneTab.Docker.RowHover.Fill`, `PaneTab.Docker.RowHover.Stroke` |
| 1c | `Glass.List.Surface.LocalHover`, `DockerHover`, `LocalBorderHover`, `DockerBorderHover` |

### Questions

- Should any list row ever use pane-tinted hover (Docker blue wash) in the future?
- Are `Glass.List.Surface.Fill/Border/Highlight` still required for `GlassSectionCardBorder`?

### Why

- Zero XAML references → misleading spec surface and maintenance cost.
- Accidental reuse could reintroduce divergent hover languages.

### How

1. `rg` repo for each key; confirm no references.
2. Delete keys from `Themes/LiquidGlass.xaml`.
3. Update `docs/spec/liquid-glass-uiux.md` token table.
4. Do **not** remove `Glass.List.Surface.Fill/Border/Highlight` (used by settings section cards).

### Test validation

| Check | Method | Pass |
| --- | --- | --- |
| Build | `dotnet build src/PortCheck/PortCheck.csproj -c Release` | No errors |
| Runtime | `--capture-to=artifacts/popup-capture.png` | Window renders |
| Visual | Compare capture to pre-change baseline | Glass shell, lists, settings cards unchanged |

---

## Point 2 — Port list item styles

### Items

| Item | Action |
| --- | --- |
| 2a | Merge `GlassPortListItemLocal` + `GlassPortListItemDocker` into one `GlassPortListItem` |
| 2b | Drop unused reflection + `Glass.List.Surface.*` hover from old base template |
| 2c | Standardize hover on `Hover.Surface` + `IsConfirmingKill` clear |
| 2d | Point both `ListBox`es + `GlassEmbeddedPortListItem` at unified style |

### Questions

- Keep Docker-tinted list hover (`Glass.List.Surface.DockerHover`) or one neutral hover for both panes?
- **Decision:** One neutral `Hover.Surface` (matches shipped UI; spec said Docker variant was never wired).

### Why

- Duplicate 70-line templates with identical triggers.
- Base `GlassPortListItem` used a different hover system than overrides → dead code path.

### How

1. Replace `GlassPortListItem` template with current Local/Docker simplified template.
2. Delete `GlassPortListItemLocal`, `GlassPortListItemDocker`.
3. `TrayPopupWindow.xaml`: both lists use `GlassPortListItem`.
4. `GlassEmbeddedPortListItem` `BasedOn` `GlassPortListItem`.
5. Update `liquid-glass-uiux.md` module table.

### Test validation

| Check | Method | Pass |
| --- | --- | --- |
| Build | `dotnet build` | Clean |
| Local list hover | Capture + manual if needed | Row hover wash visible |
| Docker list hover | `--capture-surface` N/A; switch pane in capture or second capture | Same hover language |
| Kill confirm row | Hover row, trigger confirm | Background clears on confirm |
| Settings excluded list | `--capture-surface=settings` | Embedded rows still align |

---

## Point 3 — Legacy row actions (DEFERRED)

### Items

| Item | Action |
| --- | --- |
| 3a | Remove `GlassRowKillButton`, `GlassRowDismissButton` after migrating callers |
| 3b | Keep only `*Extracted` styles |

### Questions

- Any external/theme dictionary references to legacy keys?
- Confirm `GlassRowKillButtonExtracted` `BasedOn` chain without parent.

### Why

- Two visual languages for the same affordance (solid `Danger` vs glass danger).

### How (later)

1. Change `GlassRowKillButtonExtracted` to not `BasedOn` legacy style.
2. Delete legacy styles; grep validation.
3. Capture rows in kill-confirm state.

### Test validation

| Check | Method | Pass |
| --- | --- | --- |
| Build | `dotnet build` Debug + Release | 0 errors |
| Kill confirm UI | `--capture-surface=kill-confirm` | Glass `Kill` pill + ghost `X` |
| Settings remove | `--capture-surface=settings` | Red circular `GlassRowKillIconButton` on excluded rows |
| Main popup | `--capture-to=artifacts/popup-capture.png` | No regression |

---

## Point 4 — Unified glass material stack

### Items

| Item | Action |
| --- | --- |
| 4a | Extend `GlassPopupShell`: `ShellCornerRadius`, `UseMenuChrome`, backdrop cache API |
| 4b | `TrayPopupWindow` hosts main content inside `GlassPopupShell` |
| 4c | Remove inline `BackdropImage` + tint/sheen/rim duplicates |
| 4d | `UpdateBackdropBlur()` delegates to shell with window device rect |

### Questions

- Menu shell corner 12 vs window 20 — one control or two corner DPs?
- **Decision:** `ShellCornerRadius` DP; menu default 12, window 20.
- Backdrop rect: `GetDeviceRect(Window)` vs `GetDeviceRect(Chrome)` — keep window rect for main popup to avoid parity regression.

### Why

- Two copies of tint/sheen/rim/backdrop logic diverge over time (blur radius already duplicated).

### How

1. Add DPs and `RefreshBackdrop(Rect? deviceRectOverride = null)` with cache in `GlassPopupShell.xaml.cs`.
2. Bind layer `CornerRadius` to `ShellCornerRadius`.
3. When `UseMenuChrome=false`, skip `GlassPopupMenuShell` style (no menu padding/min width).
4. Refactor `TrayPopupWindow.xaml` tree; update `ApplyRoundedClips` targets.
5. Sort menu popup continues `new GlassPopupShell { ShellContent = menu }` (defaults).

### Test validation

| Check | Method | Pass |
| --- | --- | --- |
| Build | `dotnet build` | Clean |
| Main popup | `--capture-to=artifacts/popup-capture.png` | Blur + tint + rim visible |
| Sort menu | Open sort popup; visual check or secondary capture | Menu glass stack intact |
| Resize | Resize window / reopen popup | No clip/backdrop tear |
| Settings surface | `--capture-surface=settings` | Inner chrome 18px margin preserved |

---

## Aggregate completion criteria (Phase 1+2+4)

- [x] All Point 1 keys removed; build passes
- [x] Single `GlassPortListItem`; both panes use it
- [x] `TrayPopupWindow` uses `GlassPopupShell` (`MainGlassShell`) for material stack
- [x] `artifacts/popup-capture.png` regenerated; visual parity confirmed
- [x] `artifacts/popup-capture-settings.png` — settings + section cards OK
- [x] `liquid-glass-uiux.md` reflects module changes
- [x] Point 3 complete — legacy solid-danger row styles removed; canonical `GlassRowKill*` / `GlassRowDismissButton`

## Validation evidence (2026-05-29)

| Capture | Command | Result |
| --- | --- | --- |
| Main popup | `PortCheck.exe --capture-to=artifacts/popup-capture.png` | Glass shell, pane tab bar, port list, footer |
| Settings | `… --capture-surface=settings` | Section cards, excluded list, footer highlight |

Build: `dotnet build -c Debug` and `-c Release` — 0 errors.

## Changelog

| Date | Change |
| --- | --- |
| 2026-05-29 | Plan authored; execution started for points 1, 2, 4 |
| 2026-05-29 | Points 1, 2, 4 delivered; UI capture validation passed |
| 2026-05-29 | Point 3 delivered; renamed `*Extracted` → canonical row action styles; kill-confirm capture |
| 2026-05-29 | Module hygiene batch: `GlassRoundButton`, `GlassDivider`, capsule/settings/danger dedup; all captures re-run |

## Point 3 validation evidence (2026-05-29)

| Capture | Command | Result |
| --- | --- | --- |
| Main popup | `--capture-to=artifacts/popup-capture.png` | Shell + list OK |
| Settings | `--capture-surface=settings` | Ghost dismiss on excluded rows OK |
| Kill confirm | `--capture-surface=kill-confirm --capture-to=artifacts/popup-capture-kill-confirm.png` | Glass danger Kill + ghost X OK |
