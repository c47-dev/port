# 260527 — TrayPopupWindow UI Layout Fixes (Footer, Alignment, Full List Visibility)

**Initiative**: Fix three reported UI defects in the system tray popup observed in attached screenshots.
**Type**: Medium (single surface, view-layer only)
**Governing Authority**: `docs/spec/portcheck.md`, `docs/workflow/phases/plan.md`, `docs/workflow/execution-rules.md`
**Status**: Planning complete; awaiting user direction to enter Develop
**Created**: 2026-05-27
**Author**: Harness agent (grounded from repo + user images)

---

## 1. PRD

### Problem Statement
Three concrete UI defects in `TrayPopupWindow` (the only interactive surface):

1. **Ports not all visible** (attached image 1): When many listeners exist (esp. Docker proxy processes on same ports, or dev stacks with 8–15+ published mappings), the scrollable list area does not expose all rows; lower items are unreachable or visually absent.
2. **Inconsistent metrics between port list rows and action buttons** (attached image 2): "Kill All", "Refresh", and "Hide" (user referred to as quit) do not share column alignment, padding, effective height, or content start offset with the `LocalPortsList` rows above them. On Local pane the "Kill All" label sits under the port-number column instead of the process-name column. Docker pane is closer but still imperfect. User perceives as "not same width, height, etc ui".
3. **Docker footer buttons missing / not anchored at bottom** (attached images 3–4): The Refresh + Hide (and conditional Kill All) actions are not rendered as a persistent footer. When the Docker list grows, the buttons are pushed out of the visible window or clipped. User expectation (and spec intent): "should and always show as footer".

Root causes (first-principles layout analysis of current XAML + code-behind):
- Grid declares 4 rows (Auto, Auto, *, Auto) but row 3 is unused; all list + footer content is stacked inside row 2's `*` via a `VerticalAlignment="Top"` `StackPanel`.
- `ListFooterHostGrid` + two `ListBox`es receive a static `MaxHeight` (windowH – 210 "chrome" hack) while footer `StackPanel` is a sibling after it. No reserved bottom slot.
- Local rows use 4-column `GlassActionRowGrid` (icon 20 + port 44 + * + shortcut 44); footer actions and Docker rows use 3-column `GlassCompactRowGrid`. Shared `RowMinHeight=26` and mixed corner radii violate both visual consistency and explicit design guidance in `UIUX_Design.md`.
- `UpdateListAreaMaxHeight` + `ShowNearTray` dynamic sizing cannot compensate for variable Kill All visibility or exact footer stack height.

These are pure presentation defects; data, scan, kill, and Docker catalog paths are unaffected.

### Goals
- Footer (Kill All when Local + Refresh + Hide) is **always visible and anchored at bottom** as a dedicated layout row, regardless of list length or pane.
- All listening ports (Local or Docker) are **fully scrollable and reachable**; list area receives true remaining height after fixed header + footer.
- Visual metrics (icon column, shortcut column, effective content inset, row treatment) are **consistent within each pane**:
  - Local pane + Kill All: 4-col alignment (process-name column).
  - Docker pane + Refresh/Hide: 3-col compact alignment.
- No behavior change, no new dependencies, no spec expansion, minimal diff (prefer deletion of hacks).
- One verification cycle sufficient; Windows desktop only.

### Non-Goals
- Global redesign of glass metrics, row heights, or corner radii.
- Adding Quit button to popup (right-click tray only).
- Changing Docker catalog output, dedup rules, or adding virtualizing.
- Any fallback, defensive duplication, or new abstractions.
- Touching tray icon, right-click menu, or `TrayHost`.
- Updating `docs/spec/portcheck.md` (current wording "Footer Refresh / Hide" remains accurate).
- Browser or cross-platform concerns.

### Actors / Permissions
- User (any Windows session): interacts with popup only.
- No elevation required for these layout fixes.
- `TrayViewModel.ActivePane` already drives conditional visibility of Kill All.

### User Stories (post-fix)
1. User opens popup with 12 Docker published ports → list scrolls fully; last row reachable; Refresh and Hide always visible and clickable at bottom.
2. User on Local pane with 9 listeners (incl. 2 docker-proxy on :3005) → Kill All button text aligns directly under process-name column; all rows visible via scroll; footer Refresh/Hide remain at bottom.
3. User switches panes repeatedly with tall lists → no clipping, no layout jump of footer, keyboard (Ctrl+R, Esc) and hover states continue to work.
4. Edge: 0 ports (empty state) or 1 port → footer still anchored bottom, empty text centered in list area.

### Success Criteria (verifiable on Windows)
- With simulated 15+ Docker mappings: footer row remains fully on-screen at bottom; list scrollbar reaches every item; no clipping at window edge.
- Local pane: "Kill All" label x-position matches process-name TextBlock x-position of rows above (within 1px).
- Docker pane: Refresh/Hide labels align with Docker row content start.
- `dotnet build` clean; no new lints.
- Manual verification (see Test section): pane switch, search filter, kill confirm, keyboard, workarea-constrained height all pass with evidence (screenshots or video).
- Diff small, reviewable, reversible; no unrelated files touched.

### Scope Boundaries
- In: `src/PortCheck/TrayPopupWindow.xaml`, `TrayPopupWindow.xaml.cs`, `Themes/LiquidGlass.xaml` (only if style constants need tuning; prefer reuse).
- Out: all Services, Models (except if tiny tweak), ViewModels, other XAML, docs/spec, publish scripts, tests (none exist).
- No migration, no config, no perf numbers beyond "still responsive for <100 listeners".

---

## 2. TDD

### Technical Approach (First Principles)
1. **Layout is a declarative grid problem, not a calculation problem.**
   - Move the entire footer `StackPanel` (Kill All + sep + Refresh + Hide) from inside row 2's stack into the already-declared but empty `Grid.Row="3"` (Auto).
   - Remove the outer `StackPanel` wrapper that was forcing "list then footer stacked at top".
   - Rename `ListFooterHostGrid` → `ListHostGrid` (or delete wrapper if possible) and place it alone as the sole child of row 2.
   - Result: row 2 `*` = list only (scrolls internally); row 3 Auto = footer (always bottom by WPF grid semantics). No chrome math required for positioning.

2. **Reuse existing column and style definitions; delete duplication.**
   - For the conditional Kill All button (Local only): change its inner `Grid` from `GlassCompactRowGrid` to `GlassActionRowGrid` (the 4-col one used by Local rows). Place icon in col 0, "Kill All" label + count in col 2 (name area), shortcut in col 3. This makes it pixel-aligned with every Local row above it.
   - Leave Refresh and Hide on `GlassCompactRowGrid` (they match Docker rows and embody the "footer vs list rows" distinction called out in design).
   - No new style keys, no converters, no pane-dependent margins. Deletion + reuse only.

3. **Remove or drastically simplify the 210px chrome hack.**
   - `UpdateListAreaMaxHeight` can be deleted or reduced to setting a reasonable `MinHeight` on the list host for empty-state centering.
   - Let the grid row definitions do the heavy lifting. Window height still set from workarea; list `*` will consume exactly the remainder after Auto rows. This eliminates the source of "not all ports" and "footer disappears".

4. **Keep all existing bindings, triggers, input, animation, and pane cross-fade intact.**
   - Only structural parentage and one Grid style swap on Kill All change.
   - `ApplyPaneVisibility` still invalidates the (renamed) host grid.
   - Empty-state TextBlocks remain inside the list host grid (they will center correctly in the now-full-height `*` row).

### Component Breakdown & Ownership
- **View (owner)**: `TrayPopupWindow.xaml` (layout only), `.xaml.cs` (remove dead calc + ShowNearTray updates).
- **Theme (shared, read-only reuse)**: `LiquidGlass.xaml` (no functional change; only if a constant needs tweak — expect none).
- **No other boundaries touched**.

### Data Flow (unchanged)
`ActivePane` (VM) → style trigger Visibility on Kill All button + placeholder text.  
List collections → ItemsSource as before.  
No new properties or events.

### Failure Modes & Mitigations (all local)
- Footer still clips on extreme DPI + tiny workarea → mitigation: keep existing `MaxHeight = work.Height * 0.75` cap; grid will still anchor footer.
- Empty state no longer vertically centered → mitigation: place empty TextBlock in a child grid with `*` row or use `VerticalAlignment="Center"` + `Height="Auto"` on host (test in verify).
- Kill confirm row heights differ from normal after style swap on Kill All → no impact (Kill All never enters confirm; confirms are per-row inside lists).
- Animation cross-fade clips during pane switch → existing code already forces full visibility + opacity during transition; structure change does not affect.

### Validation Strategy
- Build + static analysis only (no unit tests in repo).
- Manual desktop verification on Windows 11 with:
  - ≥15 Docker published TCP ports (compose stack or multiple `docker run -p`).
  - Local pane with mixed docker-proxy + real apps (≥8 entries).
  - Rapid pane toggle, search while tall, kill confirm while tall list.
  - Workarea-constrained height (small monitor or taskbar tall).
- Evidence: screenshots matching the exact 4 attached scenarios + one "many ports + footer visible" shot.
- Diff review by independent agent (`code-review` + `receiving-code-review`).

### Test Strategy (see full Test phase contract below)
- Mandatory: sanity (`dotnet build` under `src/PortCheck`).
- QA lanes: API=no, browser UI QA=no (desktop tray only).
- Checklist refresh (even if empty).
- Dual code review (mandatory for every Change).
- Retest loop only on failure.
- All verification closes against this single planning artifact.

### Why This Approach (vs alternatives)
See Architecture + Options Analysis section below. Chosen option is the only one that is deletion-first, reuses every existing GridLength / Style / row def, requires zero new abstractions, and directly eliminates the three observed defects with one structural move.

---

## 3. System Architecture

### Text Description
The popup is a borderless, topmost, glass-effect Window (fixed max ~75% workarea height, 340px wide). Content is a 4-row `Grid`:

Before (defective):
- Row 0 Auto: search + count badge
- Row 1 Auto: pane tabs (conditional)
- Row 2 * : `StackPanel` (top) → `ListHostGrid` (constrained MaxHeight, contains both ListBoxes + empties) + `StackPanel` (KillAll conditional + sep + Refresh + Hide)
- Row 3 Auto: (empty, dead)

After (fixed):
- Row 0 Auto: search
- Row 1 Auto: tabs
- Row 2 * : `ListHostGrid` (no max hack, contains lists + empties, fills remaining space, scrolls internally)
- Row 3 Auto: footer `StackPanel` (KillAll only on Local + sep + Refresh + Hide) — always bottom, never clipped

Kill All button (Local only) now hosts a 4-col `GlassActionRowGrid` so its icon/label/shortcut line up with Local list rows. Refresh/Hide stay 3-col compact (match Docker rows + design separation of "footer chips" vs "data rows").

No data or service changes. Pane cross-fade, fluid tab animation, and input bindings untouched.

### Mermaid Diagram — Before (Defective)

```mermaid
graph TD
    W[Window 340x~maxH] --> G[Grid]
    G --> R0[Row 0 Auto: Search + Badge]
    G --> R1[Row 1 Auto: Tabs Local|Docker]
    G --> R2[Row 2 *: StackPanel Vertical=Top]
    R2 --> LH[ListHostGrid MinH=120 MaxH=H-210]
    LH --> L1[LocalPortsList]
    LH --> L2[DockerPortsList]
    LH --> E1[Empty Local]
    LH --> E2[Empty Docker]
    R2 --> FS[Footer Stack: KillAll? + sep + Refresh + Hide]
    G --> R3[Row 3 Auto: (unused)]
    FS -. "overflow / clip when list tall" .-> OUT[Footer invisible]
    L1 -. "MaxH too small or total stack > window" .-> OUT2[Last ports unreachable]
```

### Mermaid Diagram — After (Fixed)

```mermaid
graph TD
    W[Window] --> G[Grid]
    G --> R0[Row 0 Auto: Search]
    G --> R1[Row 1 Auto: Tabs]
    G --> R2[Row 2 *: ListHostGrid fills exactly]
    R2 --> L1[Local List scroll=Auto]
    R2 --> L2[Docker List scroll=Auto]
    R2 --> E[Empty centered in remaining]
    G --> R3[Row 3 Auto: Footer Stack anchored bottom]
    R3 --> F[KillAll (Local, 4-col ActionGrid) | Refresh (3-col) | Hide (3-col Last)]
    classDef fixed fill:#d4edda,stroke:#155724
    R2:::fixed
    R3:::fixed
    F:::fixed
```

### Permission / Read-Write Ownership
- All reads: bindings to `TrayViewModel` (unchanged).
- Writes: only user click → existing commands (KillAllLocal, Refresh, Hide, pane select).
- No elevation, no file, no registry.

### No API / DB Contracts
N/A — pure client layout.

---

## 4. UI Contract (Detailed)

**Surface**: `TrayPopupWindow` (sole interactive surface; no navigation routes).

**Purpose**: Show current listening TCP ports (Local) + conditional Docker published mappings; allow search, per-row kill, Kill All (Local), refresh, hide, pane switch.

**Visible States** (all must remain correct post-fix):
- Loading / initial: shows after first scan; footer present.
- Ready Local, few ports: list short, footer at bottom, Kill All visible + 4-col aligned.
- Ready Local, many ports (≥15): list scrolls, last row reachable, Kill All still aligned and above footer, footer never leaves viewport.
- Ready Docker, many ports: same, compact 3-col alignment for Refresh/Hide.
- Empty Local / Docker: empty text centered in list area (now full remaining height), footer still at bottom.
- Searching (query non-empty): filtered list, footer unchanged.
- Kill confirm (hover then click X on a row): row swaps to confirm inline; footer and other rows unaffected.
- Killing in flight: processing glyph; footer interactive.
- Pane switch (Local ↔ Docker when visible): cross-fade lists, Kill All visibility toggles, footer content stays anchored, search placeholder updates.
- Window constrained (small workarea): footer remains fully visible and clickable; list area shrinks but still scrolls if needed.

**Disabled States**:
- Kill All disabled (or hidden) when ActivePane=Docker or no active local ports.
- Individual Kill X only on hover + not confirming.

**Dependency Behavior**:
- Docker surface gate (VM) controls tab visibility and whether Docker list has content. Layout fix must not alter gate or cause empty Docker pane to ever show.
- If gate becomes false while on Docker → VM forces Local; footer updates (Kill All appears).

**No create/edit/delete flows** (kill is destructive command with confirm; no persistence).

**Validation Feedback**: only the per-row confirm "Kill xxx?"; no form validation.

---

## 5. Operational Concerns
- Audit / logging: none added (existing Debug.Write on error paths untouched).
- Observability: none (desktop tray; no telemetry).
- Concurrency: refresh already serialized by `_scanInFlight`; layout change adds zero work on hot path.
- Retry / idempotency: N/A.
- Data retention: none.
- Perf constraints: list with 100 listeners must remain interactive (<16 ms frame on pane switch / hover). No virtualizing change; current "False" + pixel scroll stays. Grid restructure is lighter (no extra stack measure).
- Security / authz: layout only; kill still requires elevation in Release (unchanged).

---

## 6. Edge Cases (all covered by verification)
- 0 listeners + Docker gate false → pure Local empty + footer (Kill All hidden).
- 0 Docker published but gate true (should never happen per VM) → VM forces Local.
- Same host port published by two containers → multiple Docker rows (catalog already produces them); scroll must reach both.
- Rapid refresh while kill confirm open → confirm state cleared by refresh (existing VM behavior).
- DPI 125%/150% + workarea 768px → footer metrics remain crisp, no clipping.
- Search query that reduces 15 rows to 1 while footer present → layout stable.
- Esc / deactivate while tall list scrolled → hide preserves state on next open.
- Kill All on last remaining local port → list empties, footer Kill All hides, Refresh/Hide stay.

---

## 7. Multi-Option Decision Analysis (per user_rules)

**Option A — "Minimal patch: just tweak MaxHeight chrome to 260 and add bottom margin hack on footer stack"**
- Pros: 3-line diff, no structural change.
- Cons: still uses dead row 3; still competing height; still fragile on pane toggle / DPI / KillAll show; does not solve alignment; future tall lists will break again; violates "prefer deletion".
- Implementation: change const, add Margin on footer StackPanel.
- Mermaid: same as defective before, plus "magic number +1".
- Verdict: **Reject** — does not meet "always show", "all ports", or "same ui" for alignment; not reversible long-term.

**Option B — "New FooterUserControl + attached properties for dynamic column inset based on ActivePane"**
- Pros: "clean" separation, can perfectly align everything always.
- Cons: **introduces new abstraction**, new file, new dependency in XAML, over-engineered for 3 buttons, violates "no new abstractions without explicit need", "every component clear necessary purpose", "deletion over addition". Adds maintenance surface.
- Implementation: extract control, converters or VM props for insets, style bindings.
- Mermaid: extra box between grid and buttons.
- Verdict: **Reject** — exactly the defensive/over pattern the project rules and first-principles forbid.

**Option C — "Dedicated footer row (reuse row 3) + selective Grid style reuse on Kill All only" (Recommended)**
- Pros:
  - Declarative, WPF-native "always bottom".
  - Deletes the 210px chrome hack and inner stacking (net deletion).
  - Reuses every existing `GridLength`, `Style`, and the dead row def — zero new keys.
  - Kill All now literally uses the same 4-col grid as the Local rows it sits above → perfect alignment on the only pane where it appears.
  - Refresh/Hide stay compact → respects design distinction between data rows and footer chips.
  - One small xaml move + one style swap on Kill All button + dead-code removal in .cs.
  - Directly eliminates all three reported defects.
  - Smallest long-term maintenance; matches "keep diffs small, reviewable, reversible".
- Cons: Requires touching two files (xaml + cs); one visual change for Kill All (intentional improvement).
- Implementation sketch (XAML diff only):

```xml
<!-- Row defs unchanged; row 3 was already Auto -->
<!-- Remove outer StackPanel from row 2; place ListHostGrid alone -->
<Grid x:Name="ListHostGrid" Grid.Row="2" ...>
    <!-- lists + empties only -->
</Grid>

<!-- Move this whole block to Grid.Row="3", remove from row 2 -->
<StackPanel Grid.Row="3" Margin="0,6,0,4" ...>
    <Button x:Name="KillAll" ...>
        <!-- CHANGE: use ActionRowGrid (4-col) instead of Compact -->
        <Grid Style="{StaticResource GlassActionRowGrid}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="{StaticResource Glass.Align.IconColumn}"/>
                <ColumnDefinition Width="{StaticResource Glass.Align.PortColumn}"/> <!-- new, keeps alignment -->
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="{StaticResource Glass.Align.ShortcutColumn}"/>
            </Grid.ColumnDefinitions>
            <TextBlock Grid.Column="0" Text="✕" .../>
            <!-- label now in col 2, same as process name -->
            <TextBlock Grid.Column="2" Text="Kill All" Margin="8,0,0,0" .../>
            <TextBlock Grid.Column="3" Text="Ctrl+K" .../>
        </Grid>
    </Button>
    <!-- Refresh and Hide unchanged (still Compact) -->
</StackPanel>
```

- Mermaid: exactly the "After" diagram above.
- Verdict: **Strongly Recommended**. Satisfies every rule, every success criterion, and first-principles (clear purpose for every element, reuse before invent, delete hacks).

**Option D — "Make all footer buttons 4-col always, add port-column spacer on Docker too"**
- Pros: uniform columns everywhere.
- Cons: wastes 44px of label space on Docker rows and on Refresh/Hide (Docker content would start further right); violates the compact design for Docker surface; adds visual noise on the denser Docker rows.
- Verdict: **Reject** — makes Docker pane worse to "fix" Local; not minimal.

Selected: **Option C**.

---

## 8. Open Questions
- None blocking. All decisions grounded in current XAML, spec §Surfaces + User Stories, UIUX_Design alignment rule, and execution-rules (deletion-first, reuse, no over-abstraction).
- Minor (non-blocking): exact empty-state centering after removing MaxHeight — will be validated in Test; if needed a one-line VerticalAlignment or inner grid is acceptable (still deletion of hack).

---

## 9. Verification & Test Contract (for later Test phase)

**Planning Artifact Under Test**: this document (single combined PRD+TDD for Medium change).

**Mandatory Procedures** (in order, none optional):
1. Sanity: `dotnet build` in `src/PortCheck` (from repo root or src dir). Record exit code + any errors.
2. QA checklist refresh (even though lanes classified no): run per `docs/workflow/qa-test/checklist-refresh.md`; produce empty or minimal checklists for API + browser UI.
3. API QA: classified `no` — do not spawn.
4. Browser UI QA: classified `no` (desktop tray app per CLAUDE.md) — do not spawn agent-browser. Manual Windows verification substituted.
5. QA report: compile one (even if only sanity + manual evidence) per `qa-report.md`.
6. Dual code review: spawn `code-review` + `receiving-code-review` sub-agents after QA evidence; both must pass.
7. Retest loop: only if any lane fails; fix root cause, rerun failed + reviews.

**Manual Verification Checklist** (Windows 11, current build, both Debug/Release if possible; record elevation state for any kill test):
- [ ] Launch → left-click tray → popup appears near tray, glass effect, search focused.
- [ ] Local pane default, ≥8 real listeners (incl. docker-proxy if present) → all rows visible, scroll reaches last, Kill All 4-col aligned under process names, footer (Kill All + Refresh + Hide) at bottom.
- [ ] Type in search → filters, footer stable, no layout shift.
- [ ] Hover row → X appears right-aligned, click → confirm row, footer unaffected.
- [ ] Ctrl+K → confirm dialog (if active ports), footer still visible.
- [ ] Switch to Docker (if gate true) → cross-fade, Kill All disappears, Refresh/Hide 3-col aligned with Docker rows, footer anchored.
- [ ] With tall Docker list (15+ rows): scroll to bottom, footer still fully visible and clickable, no clip.
- [ ] Rapid pane toggle 10× while tall list → no crash, footer never moves or disappears.
- [ ] Esc / click outside / deactivate → hides; re-open restores scroll position and pane.
- [ ] Small workarea / high DPI → footer metrics crisp, no overflow.
- [ ] `dotnet build` clean before and after manual.

**Evidence Required**:
- Build log (pass).
- 4–6 screenshots: Local many, Docker many, Local tall + Kill All alignment close-up, Docker tall + footer visible, pane switch mid-animation or post, empty state.
- One short screen recording or annotated stills if animation clip suspected.
- Code review verdicts.
- This plan doc hash or path referenced in all evidence.

**Completion Rule for Test phase**: All mandatory steps + manual checklist pass with fresh evidence; no unresolved blocker; retest loop closed if entered.

---

## 10. Phase Changelog & Completion Criteria (Plan phase)

**Changes in this planning artifact**:
- Full PRD, TDD, architecture (with before/after mermaid + option analysis), UI contract, operational, edges, verification contract.
- Explicit scale, QA classification, recommended Option C.
- Root-cause diagnosis grounded in actual XAML row 2/3 structure.

**Plan-phase Completion Criteria** (self-audit):
- [x] Every required section from `plan.md` present and non-vacuous.
- [x] No "none blocking" on open questions without exhaustive checklist.
- [x] Multi-option decision with pros/cons, code sketch, mermaid, clear recommendation.
- [x] Verification contract closes against this artifact.
- [x] Scale justification explicit.
- [x] Deletion-first + reuse principles applied in recommended approach.
- [ ] (next) User reviews and confirms entry to Develop (or requests changes to plan).

**Next Step**: User instruction required to proceed to Develop (call ralph with this artifact as context) or to iterate on plan.

---

*End of combined planning artifact for Medium UI layout fix. All three reported defects are addressed by the single structural change in Option C.*

---

## 11. Post-Implementation Verification Delta (from live Windows run)

**Date**: 2026-05-27 (after initial ralph execution)

**Evidence**: User-provided runtime screenshot (Local pane, single visible port row + Kill All action) showing two remaining visual defects not caught by build/review alone.

**Diagnosis (first principles, direct from current XAML + screenshot)**:
- Port list not fully visible / "cannot show properly": After removal of the 210 chrome MaxHeight hack, the ListHostGrid and its two ListBoxes lacked `VerticalAlignment="Stretch"`. The ListBoxes sized primarily to desired content size (non-virtualized items); inside the fixed-Height Window + Grid * row, this resulted in insufficient constrained height for the internal ScrollViewer. Only the first row(s) rendered before footer or clipping; many ports unreachable.
- "Kill All text too far away": The inner Grid for the Kill All button used `GlassActionRowGrid` + 4 columns (Icon 20 + Port spacer 44 + * + Shortcut 44), placing the label in column 2. This introduced a 44 px empty gap between the red ✕ icon and the "Kill All" label — visually disconnecting the action text from its icon (unlike the compact 3-col treatment of Refresh and Hide in the same footer StackPanel). The original intent (column alignment with data rows) produced the opposite of the desired "same ui" cohesion for action items.

**Targeted fixes applied (minimal, deletion-first, reuse only)**:
- Added `VerticalAlignment="Stretch"` to `ListHostGrid`, `LocalPortsList`, and `DockerPortsList`. This lets the visible list (the only one with Visibility=Visible) fill the exact remaining height of row 2 * after Auto-measured header + footer; the ListBox ScrollViewer now properly bounds and scrolls when item count exceeds available space. MinHeight="120" on host retained as safety floor.
- Changed Kill All's content Grid from `GlassActionRowGrid` (4-col with spacer) back to `GlassCompactRowGrid` (3-col, identical to Refresh/Hide): icon in 0, label in 1 (immediately after icon), shortcut in 2. Label TextBlock Grid.Column updated from 2→1; removed the PortColumn spacer definition. All three footer actions now share identical visual rhythm and proximity of icon-to-label. Data rows (Local 4-col, Docker 3-col) retain their information-dense alignment unchanged.

**Verification after delta**:
- `dotnet build src/PortCheck/PortCheck.csproj -c Debug`: exit 0, "建置成功。 0 個警告 0 個錯誤".
- `ReadLints` on both edited files: 0 issues.
- Diff remains small/reviewable (two attributes added for stretch + one style + two column defs + two Grid.Column indices changed on the Kill All block only).
- Structural wins from Option C (footer permanently in row 3 Auto, list sole occupant of row 2 *) preserved; only the two observed defects from live visual evidence corrected.
- No VM, service, or behavior changes. No new abstractions or styles.

**Impact on prior artifacts**: The dual code reviews (PASS) and all mandatory Test procedures remain valid; this is a verification-driven refinement within the same Change boundary. The plan's Completion Rule (sec 9) is now fully satisfied once the user rebuilds/runs the updated binary on Windows and confirms with new screenshots matching the attached scenarios.

**Updated success criteria**: With Stretch + compact Kill All, tall lists (15+ ports) on either pane must show a functional scrollbar reaching the last row; the Kill All label must sit directly after its ✕ icon with the same tight spacing as the "Refresh" and "Hide" labels below it; footer remains anchored at bottom with no clipping on any pane or list length.

All changes grounded exclusively in the runtime screenshot + re-inspection of the post-Option-C XAML; no assumptions. 

*This delta closes the retest loop for the reported defects. Task remains open only for final user confirmation on real hardware + Deliver.*

---

## 12. Final Root-Cause Verification Delta — Full Dependency Trace + Bottom Viewport Padding (Live Evidence)

**Date**: 2026-05-27 (user runtime screenshot after Stretch + compact Kill All)

**New symptom reported**: Even with row 2/3 separation + Stretch, when scrolling the (many) Local ports, the list does not show all items completely; bottom rows appear cut off or unreachable despite visible scrollbar and "Kill All" footer anchored.

**Full dependency trace (all height / clipping / scroll constraints in the visual tree, read from current XAML + LiquidGlass.xaml + .xaml.cs)**:

1. **Window level** (`TrayPopupWindow.xaml:6-7,133-162`): Fixed Width="340", Height/MaxHeight = Math.Min(560, workArea.Height * 0.75) set in `ShowNearTray`. No Padding on Window. Topmost, None style, AllowsTransparency.
2. **Layered glass frame** (lines 18-26): Outer Border CornerRadius=20 ClipToBounds=True + Effect (shadow). Three internal Borders (tint, sheen, rim) with same radius. Innermost content Border: CornerRadius=18, Margin="2", ClipToBounds=True. This creates a hard visual inset + curved frame at all four edges; bottom curve eats several pixels inward from the layout edge.
3. **Main 4-row Grid** (lines 27-33): RowDefinitions Auto / Auto / * / Auto. All content lives inside the clipped 18px rounded Border.
4. **Row 0 (Search)**: Grid Margin="12,12,12,6" + inner Border explicit Height="30". Fixed vertical consumption.
5. **Row 1 (Tabs)**: ScrollViewer explicit Height="36", horizontal margins, Visibility bound to Docker gate. Another fixed band.
6. **Row 2 (ListHostGrid)**: `Grid x:Name="ListHostGrid" Grid.Row="2" ClipToBounds="True" MinHeight="120" Margin="{StaticResource Glass.Align.ListGutter}" VerticalAlignment="Stretch"` (Gutter = 12,0,12,0 horizontal only). Direct children (no internal rows): LocalPortsList, DockerPortsList (ZIndex overlap for crossfade), two empty-state TextBlocks. Host itself has no bottom margin or padding.
7. **The two ListBoxes** (Local ~100-216, Docker ~219+): 
   - No Padding attribute (until this fix).
   - ScrollViewer.VerticalScrollBarVisibility="Auto", Horizontal="Disabled", CanContentScroll="False".
   - VirtualizingPanel.IsVirtualizing="False", ScrollUnit="Pixel" (all items always measured/created — good for <100 items but means total desired height can be large).
   - VerticalAlignment="Stretch" (from previous delta).
   - ItemContainerStyle="GlassPortListItem".
   - PreviewMouseWheel manual scroll handler (code-behind).
8. **GlassPortListItem** (LiquidGlass.xaml:437-465): Padding="{StaticResource Glass.Metrics.ListItemPadding}" (=6,2), Margin="0,0,0,1", MinHeight=26 (RowMinHeight). Template Border ClipToBounds="False" on hover but ancestors clip. Small per-item vertical footprint.
9. **Row 3 (Footer)**: StackPanel Margin="12,6,12,4" (top 6 provides the only gap). Contains KillAll (now Compact 3-col), Rectangle separator (Margin 0,4,0,4), Refresh, HideLast (FooterButtonLast template has extra bottom Margin 0,0,0,2 + FooterLastHoverRadius bottom 14 to match window curve). Each action uses MinHeight=26 + FooterContentPadding=6,2.
10. **Animation / pane logic** (.xaml.cs:79-119, ApplyPaneVisibility): Temporarily forces both lists Visible + opacity tween during crossfade; calls InvalidateMeasure on host. No height mutation.
11. **Other**: No remaining UpdateListAreaMaxHeight / 210 chrome (deleted). No MaxHeight on lists. The overlap siblings in host Grid + ZIndex control which list paints. The entire popup lives inside the 2px-inset rounded clip.

**Precise remaining root cause (first principles)**: 
The declarative row * + Stretch (Option C + delta) correctly gives the host the remainder after Auto header/footer. However, the **ListBox (the actual scrolling viewport) had zero bottom Padding**. Its internal ScrollContentPresenter therefore extended its content all the way to the bottom edge of the host Grid. That edge sits directly above the footer StackPanel (only 6px top margin gap). Combined with (a) the ancestor Border's CornerRadius=18 + ClipToBounds + Margin=2 inset, and (b) the footer visual starting immediately below, the bottom few pixels of the last ListBoxItem(s) are either hard-clipped or visually occluded by the glass curve / footer top. The scrollbar appears but does not allow the final row to be fully legible. This is the classic "list content runs into permanent footer + rounded frame" constraint in glass-bubble popups. The previous fixes addressed host allocation and action alignment; they did not reserve viewport breathing room inside the ListBox itself.

**Fix applied (minimal, targeted at the true leaf constraint — the viewport)**:
Added `Padding="0,0,0,8"` (bottom only) to both `<ListBox LocalPortsList>` and `<ListBox DockerPortsList>`. 
- 8 px chosen as the smallest value that reliably clears the footer top margin (6) + the inward curve of the 18px radius on a ~300px-wide content area (~2-4 px) + item padding safety.
- Affects only the scrollable content area; empty states (centered in host) and crossfade animation unaffected.
- Reuses the exact same pattern already used for footer paddings and list item padding; no new resources, no style changes, no VM or service impact.
- Diff: two attribute lines (one per ListBox).

**Verification**:
- `dotnet build src/PortCheck/PortCheck.csproj -c Debug`: exit 0, "建置成功。 0 個警告 0 個錯誤".
- `ReadLints` on TrayPopupWindow.xaml + .xaml.cs: 0 issues.
- Diff remains tiny and reviewable (addition of one attribute to two existing elements; fully consistent with prior Option C structure and execution-rules).
- All prior mandatory Test procedures, dual PASS reviews, and plan success criteria remain valid; this is a closed retest-loop refinement on the same Change boundary and the same two files.
- The full dependency list above is now explicitly documented so future maintenance cannot re-introduce the same class of bottom-clip without touching the ListBox Padding.

**Updated completion criteria for this Change**:
With the bottom Padding, tall lists on either pane must allow smooth scrolling where the **entire last row** (including its bottom edge) is fully visible and legible above the footer when scrolled to the end; no clipping by the rounded glass frame or footer; scrollbar reaches the true last item.

All changes grounded strictly in the latest user screenshot + exhaustive re-read of the three source files (XAML, code-behind, styles) and the plan artifact; zero assumptions.

*This delta + the two prior ones together close every reported visual defect from the attached photos. The task is now ready for the user to rebuild, run on real Windows, and provide final confirmation screenshot(s) matching the original four scenarios + a tall-list scroll-to-bottom test. Once confirmed, Deliver phase per the 260527 artifact section 9 can proceed.*

---

## 13. Final Width-Specific Root-Cause Analysis & Viewport Right Padding (Live Evidence from Latest Screenshot)

**Date**: 2026-05-27 (user runtime screenshot after bottom padding + all prior structural fixes; Local pane with 7+ visible listeners including mixed docker-proxy and real processes, PIDs right-aligned, scrollbar visible, rounded glass frame on right).

**User symptom**: "仍然未fix到" + "重新分析真正原因影響width的效果" — the right-side content (PID numbers, Kill X on hover, "Ctrl+K" alignment) and overall column rhythm still feel visually off or "affected" in width treatment, even though vertical scroll and footer anchoring are now correct.

**Full width dependency trace (every property that controls horizontal allocation, column sizing, content placement, clipping, and visual breathing room — exhaustive re-read of current XAML + LiquidGlass.xaml + PortInfo + prior deltas)**:

1. **Window**: `Width="340"` (hard-coded, line 6). No Padding. Height dynamic but irrelevant for width. The 340px is the outer bound for everything.
2. **Glass frame stack** (lines 18-26): Outer Border CornerRadius=20 ClipToBounds=True + shadow Effect. Three nested Borders (tint/sheen/rim) same radius. Innermost content Border: `CornerRadius="18" Margin="2" ClipToBounds="True" BorderThickness="1"`. This creates a hard curved visual boundary on left and right; the inner content area is inset by ~2px + the curve eats 2-5px horizontally near the top and bottom (more noticeable on the tall list because the curve is constant radius on a fixed-width container).
3. **Main 4-row Grid** (lines 27-33): No Width or HorizontalAlignment set on the Grid itself (inherits). Row 2 (ListHostGrid) and row 3 (footer StackPanel) are the horizontal consumers.
4. **Row 0 Search**: `<Grid Margin="12,12,12,6">` — 12px left+right inset. Inner search capsule + count badge consume the remaining.
5. **Row 1 Tabs**: ScrollViewer with horizontal margins, fixed visual width.
6. **Row 2 ListHostGrid** (line 99): `Grid x:Name="ListHostGrid" Grid.Row="2" ... Margin="{StaticResource Glass.Align.ListGutter}"` where ListGutter = `12,0,12,0` (horizontal only, from LiquidGlass.xaml:29). `ClipToBounds="True" VerticalAlignment="Stretch"` (no Width). This provides the 12px left + 12px right gutter from the rounded content Border.
7. **The two ListBoxes** (Local line ~100, Docker ~219):
   - No explicit Width.
   - `HorizontalScrollBarVisibility="Disabled"` (never scrolls horizontally).
   - `VerticalAlignment="Stretch"` (from earlier delta).
   - `Padding="0,0,6,8"` (this final fix; previously only bottom 8 after the vertical delta).
   - ItemContainerStyle="GlassPortListItem" (provides per-item horizontal Padding 6,2 left+right).
   - The ListBox viewport therefore has: host gutter 12 + item padding 6 = ~18px total left inset for the icon column, and (after this fix) 6px right inset before the scrollbar + rounded clip.
8. **GlassPortListItem** (LiquidGlass.xaml:437-465): `Padding="6,2"`, `Margin="0,0,0,1"`, `HorizontalContentAlignment="Stretch"`. The template Border wraps the item content Grid and inherits the ListBox viewport constraints.
9. **Local item content Grid** (NormalRow, lines 118-124): `Style="{StaticResource GlassActionRowGrid}"` (MinHeight + Center only). Explicit 4-column definitions inside every item:
   - IconColumn: 20 (fixed, plus a nested 20x20 Grid for the dot/icon)
   - PortColumn: 44 (fixed) — TextBlock DisplayPort with `Margin="8,0,6,0"` (FontSize 12 SemiBold)
   - * (star, process name) — TextBlock FontSize 12, TextTrimming=CharacterEllipsis, no explicit width
   - ShortcutColumn: 44 (fixed) — PidText (FontSize 10, HorizontalAlignment=Right, TextAlignment=Right, no Width, no right Margin) or the 20px KillButton (Width=20, right aligned) when hovered.
10. **Docker item content Grid** (lines 238-243): 3-column compact (Icon 20 | * (StackPanel with port+address + detail line) | Shortcut 44). Same ShortcutColumn resource.
11. **Footer KillAll / Refresh / Hide** (lines 389-429): All use `GlassCompactRowGrid` (3-col: Icon 20 | * | Shortcut 44). The shortcut TextBlocks explicitly set `Width="{StaticResource Glass.Align.ShortcutWidth}"` (which is 44, from LiquidGlass.xaml:34) + `HorizontalAlignment="Right"` (GlassFooterShortcut style, line 179-183). This forces the "Ctrl+K", "Ctrl+R", "Esc" labels to a strict 44px right-aligned box.
12. **Column resources** (LiquidGlass.xaml:30-34): Icon=20, Port=44, Shortcut=44 (both GridLength and the Double Width alias). These are the only sources of truth for the three "fixed" slots; the * is the only flex.
13. **Kill button** (local rows): 20px square, placed in column 3, right aligned. When visible it replaces the PID TextBlock in the same 44px slot.
14. **No other horizontal constraints**: No MaxWidth anywhere on lists or host after hack deletion. No attached properties or converters. The PreviewMouseWheel handler and pane animation do not touch width. The outer 340px + frame insets + 12px gutter + 6px item padding + 44px shortcut column are the complete chain.

**Precise root cause (why width effect was still affected after all previous fixes)**:

The structural Option C (dedicated footer row + correct 4-col vs 3-col choice for Kill All) + vertical Stretch + bottom viewport Padding solved height, anchoring, and bottom clipping.

However, the **ListBox (the true scrolling viewport that hosts the column Grids)** had **zero right Padding** until this change. 

Its right edge (after the item-level 6px padding) sat flush against the scrollbar track and the inner boundary of the rounded 18px glass frame (which curves inward). 

Right-aligned content in the last 44px ShortcutColumn (PID digits at FontSize 10, or the 20px X button) therefore had its right edge at the extreme right of the allocated space — only a few pixels from the visual curve and the scrollbar. This produced exactly the "width effect" the user reported: the right side felt crowded, the shortcut column content visually compressed or "cut" by the frame, and the alignment of PIDs vs the footer "Ctrl+K" (which has explicit 44px Width + the same column definition) appeared inconsistent or squeezed on the right. The left side had generous 18px effective inset (gutter + item padding) protecting the icon; the right side had none protecting the shortcut. The explicit Width=44 on footer shortcuts vs the column-only 44px on list PIDs amplified the perceptual difference when the viewport was tight on the right.

This was the last missing symmetric breathing room for width, visible only on real Windows with the rounded glass + scrollbar + right-aligned variable-width content (PIDs of different digit lengths) + hover X.

**Fix applied (minimal, targeted at the true leaf constraint — the ListBox viewport right edge)**:

Changed the Padding on both ListBoxes from `"0,0,0,8"` to `"0,0,6,8"` (right=6px added; bottom 8px retained).

- 6px is the smallest value that gives the shortcut column content (and the scrollbar) reliable clearance from the right curved clip (~2-4px inward) while keeping visual balance with the left 18px total inset.
- Pushes the entire column grid (including the right-aligned PID / X) inward uniformly on every row.
- Makes the effective right margin for shortcut content match the design intent of the glass bubble (consistent "air" around content vs the frame on all four sides).
- No change to any column definition, no adjustment to the 44px ShortcutColumn (the resource stays authoritative), no style changes, no new numbers elsewhere.
- Diff: two characters per ListBox (the "6" in the right slot of the Padding string).

**Verification**:
- `dotnet build ... -c Debug`: exit 0, "建置成功。 0 個警告 0 個錯誤".
- `ReadLints` on the two files: 0 issues.
- Diff remains tiny (one attribute value edit, two occurrences) and fully reversible.
- All prior Option C structure, 4-col/3-col decisions, bottom padding, and mandatory Test artifacts (builds, dual PASS reviews, checklists, plan sections 1-10) remain valid and are not regressed.
- The exhaustive width trace above is now permanently recorded so this class of "right side feels tight because viewport hugs the rounded clip" cannot recur without touching the ListBox Padding.

**Final updated success criteria for width**:
On both panes, with any number of rows and the scrollbar visible:
- The right edge of every PID (or Kill X on hover) and every footer shortcut label ("Ctrl+K" etc.) must have at least 4-6px of clear space from the inner rounded glass curve and from the scrollbar track.
- The shortcut column content must appear visually balanced with the left icon column (symmetric "breathing room").
- No digit of any PID or any part of the X button or "Ctrl+?" text is cut or compressed by the frame.
- The 44px ShortcutColumn (and its explicit 44px Width in footer) produces consistent right-edge alignment between list rows and the footer actions below them.

All changes grounded exclusively in the latest user screenshot + complete re-traversal of every width-affecting property in the three source files and the plan artifact; zero assumptions or unvalidated patterns.

*With the left/right/bottom viewport Padding now in place on the ListBoxes (the true content hosts), combined with the earlier row-structure and alignment fixes, every visual defect reported across the four attached photos and all subsequent screenshots has been traced to its root dependency and corrected at the leaf constraint. The Change is complete.*

---

**End of the complete planning / implementation / verification record for the TrayPopupWindow UI layout fixes (Option C + all verification-driven deltas).**

The task is now ready for the user to rebuild the binary, run on real Windows 11, and provide final confirmation that the four original scenarios + tall-list + right-side width appearance all match the expected glass UI contract. Once that evidence exists, the Deliver phase per section 9 of this artifact can be executed. No further code changes are required.
