# PortCheck Liquid Glass UI/UX Specification

## Authority

| Document | Scope |
| --- | --- |
| **This file** | Visual language, reusable UI modules, motion, and composition rules for WPF surfaces |
| [`portcheck.md`](portcheck.md) | Product behavior, surfaces, settings, Docker gate, kill flows |

When UI work conflicts with product behavior, `portcheck.md` wins. When implementation drifts from this file, update code or this spec together in the same change.

---

## Design intent (aspirational)

PortCheck popup UI follows an **Apple Liquid Glass**–inspired language: one floating translucent bubble above the desktop, with crisp typography and motion that feels physical without mimicking iOS controls literally.

### Aspirational material rules

| Principle | Target behavior |
| --- | --- |
| **Translucency** | Container UI uses layered translucency (~20–80% effective opacity by depth), not flat opaque panels |
| **Lensing / refraction** | Content behind the shell subtly bends and concentrates light like optical glass |
| **Adaptive tint** | Frosted layer picks up hue from wallpaper or content directly behind the window |
| **Specular rim** | Thin high-contrast inner border / top highlight shifts with interaction |
| **Floating bubble** | One detached shell; avoid nested full glass cards |
| **Concentric radius** | Large outer corner radius; inner elements stay concentric |
| **Soft elevation** | Expansive, low-opacity, high-blur drop shadow |
| **Adaptive text** | Text/icon luminance shifts for contrast over varying backdrops |
| **Hierarchy** | Weight and scale carry structure; avoid stacking glass-on-glass |

### Aspirational component cues

| Element | Rule |
| --- | --- |
| Search / header | Inset capsule, muted until focused |
| Status | Saturated markers on neutral glass (not flat text badges) |
| List rows | Idle = flat on shared bubble; hover = internal glow / step-up illumination |
| Footer actions | Compact, borderless-looking, icon + label + shortcut columns aligned |
| Destructive | High-contrast text or ghost/danger fills; no full-width solid warning blocks |

Pane tabs borrow **iOS 18 Mail Smart Categories** metaphor: horizontal chips, idle circle → active frosted pill, spring width push, list cross-fade on pane change. Accent color on **icons only** (Docker blue `#1D63ED`), never solid opaque tab fills.

---

## Runtime implementation (actual)

What PortCheck ships today:

| Layer | Implementation |
| --- | --- |
| **Backdrop** | `BackdropBlurHelper`: `CopyFromScreen` of region behind window → downsample → WPF `BlurEffect` → optional dim overlay |
| **Tint / sheen / rim** | Fixed brushes in `LiquidGlass.xaml`: `Glass.Tint`, `Glass.InnerSheen`, `Glass.RimLight` |
| **Shell shadow** | `Glass.WindowShadow` on outer popup border |
| **Text** | Fixed `Text.Primary` / `Text.Secondary` / `Text.Tertiary` + `Text.Shadow` effect |
| **Motion** | `FluidAnimation.cs` (pane tabs); `GlassRoundButtonInteractionAnimator.cs` (`GlassRoundButton` only) |

### Not implemented (do not claim in QA or screenshots)

| Aspirational feature | Status |
| --- | --- |
| Full-popup real-time lensing | **No** — shell uses capture blur + tint only |
| Round chrome (`GlassRoundButton`) lensing | **Partial** — 32×32 hover backdrop crop + rim/specular (no runtime pixel shader); see [GlassRoundButton contract](#glassroundbutton--liquid-chrome-contract) |
| Wallpaper-adaptive tint | **No** — static `Glass.Tint` |
| Dynamic opacity scale by content depth | **No** — fixed brush alphas |
| Adaptive text toning | **No** — fixed white palette |
| Search-row port count badge | **No** — `ActivePanePortCount` exists in VM, not bound in UI |
| Tab label fade-in animation | **No** — instant visibility via triggers |
| Refresh counter pop | **No** |

**Capture QA flags (Debug):** `--capture-to=…`, optional `--capture-surface=settings|kill-confirm`.

`ConfirmDialog` uses a separate dark opaque stack (`#F21E1E1E`); it is **not** part of the liquid-glass module set.

---

## Material stack (popup shell)

Apply layers in order via `Controls/GlassPopupShell` (backdrop host):

```text
[BackdropBrush]     ← BackdropBlurHelper capture (optional fallback: no image)
[Glass.Tint]        ← #18FFFFFF frosted wash
[Glass.InnerSheen]  ← top gradient highlight
[Glass.RimLight]    ← 1px border stroke
[Content]           ← chrome, tabs, lists, footer (sibling overlay on main popup)
```

**Main popup layout:** `OuterChromeBorder` (shadow) → `Grid` → `MainGlassShell` (`UseMenuChrome=false`, corner 20) under content → `InnerChromeBorder` (corner 18, margin 2).

**Sort menu layout:** `GlassPopupShell` with default `UseMenuChrome=true`, `ShellContent` = `GlassSortMenuControl`.

**When to use:** Any floating surface that needs captured blur + tint stack. **Do not** repeat this stack on list rows, footer buttons, or Settings section bodies.

---

## Global tokens (`Themes/LiquidGlass.xaml`)

### Semantic brushes

| Key | Role |
| --- | --- |
| `Glass.Tint`, `Glass.InnerSheen`, `Glass.RimLight` | Shell material |
| `Glass.WindowShadow`, `Text.Shadow` | Elevation and text legibility |
| `Text.Primary` / `Secondary` / `Tertiary` | Body copy hierarchy |
| `Search.Fill`, `Search.Stroke` | Search capsule |
| `PaneTab.Idle.Fill`, `PaneTab.Active.Fill`, `PaneTab.Active.Stroke` | Pane chips (`#22` / `#72` / `#A0` white alpha) |
| `PaneTab.Docker.Accent` | Docker icon fill `#1D63ED` |
| `Hover.Surface` | Generic hover wash |
| `Glass.Action.Ghost.*` | Neutral inline actions (dismiss) |
| `Glass.Action.Danger.*` | Destructive inline actions (kill) |
| `Glass.List.Surface.Fill/Border/Highlight` | Settings section cards only (not port-row hover) |
| `Status.Active` | 6px listener dot `#34C759` |
| `Glass.Action.Danger.Foreground` | Destructive label/icon on glass (Kill All, validation) |
| `Glass.Action.Danger.*` | Destructive fills/strokes for row actions |
| `Settings.BlueIconFill`, `Settings.GreenIconFill` | Settings section leading badges |

### Alignment grid

| Key | Value | Use |
| --- | --- | --- |
| `Glass.Align.IconColumn` | 20 | Leading icon / dot column |
| `Glass.Align.PortColumn` | 56 | Port number column (local rows) |
| `Glass.Align.ShortcutColumn` | 44 | PID / shortcut column |
| `Glass.Align.*Gutter` | Thickness | Chrome, list, footer horizontal inset (12px) |

Icon + shortcut columns **align across** footer, port rows, and settings rows. **Do not** reuse the same corner radius or row height between list hover chips and footer hover chips.

### Metrics (selected)

| Key | Typical use |
| --- | --- |
| `Glass.Metrics.ChromeRowHeight` (32) | Search bar, icon buttons, tab height |
| `Glass.Metrics.ChromeCapsuleRadius` (16) | Search, capsules, rounded actions |
| `Glass.Metrics.TabIconSlotWidth` (32) | Collapsed pane tab |
| `Glass.Metrics.ListHoverRadius` (10) | List row hover |
| `Glass.Metrics.FooterHoverRadius` (8) | Footer button hover |
| `Glass.Metrics.WindowCornerRadius` (20) | Main popup + `MainGlassShell` |
| `Glass.Metrics.WindowInnerCornerRadius` (18) | Inner content inset |
| `Glass.Metrics.PopupMenuCornerRadius` (12) | Sort menu shell (default `ShellCornerRadius`) |
| `Glass.Metrics.DockerMarkWidth/Height` (15×12) | Docker path icon |

---

## Reusable style modules

All styles live in `src/PortCheck/Themes/LiquidGlass.xaml` unless noted.

### Shell & chrome

| Style | Target | Purpose | When to use |
| --- | --- | --- | --- |
| `GlassSortMenuShell` | `Border` | Sort menu: captured blur + `Glass.List.Surface` tint; single soft border; no rim/sheen/window shadow | `GlassPopupShell` when `UseMenuChrome` |
| `GlassRoundButton` | `Controls.GlassRoundButton` (`Themes/GlassRoundButton.xaml`) | 32×32 liquid lens chrome — see [GlassRoundButton contract](#glassroundbutton--liquid-chrome-contract) | Sort/filter, Settings back |
| `GlassScrollBar` | `ScrollBar` | Thin translucent thumb | Port list scrollers |

### Typography

| Style | Purpose | When to use |
| --- | --- | --- |
| `GlassTextPrimaryShadow` | Primary labels on glass | Titles, port numbers, settings labels |
| `GlassTextSecondaryShadow` | Secondary copy | Process names, placeholders |
| `GlassTextTertiaryShadow` | Tertiary / meta | PID, units, hints |
| `GlassFooterIcon` / `Label` / `Shortcut` | Footer row text roles | Inside `PopupActionRow` |

### Layout grids

| Style | Columns | When to use |
| --- | --- | --- |
| `GlassActionRowGrid` | Icon 20 + Port 56 + * + Shortcut 44 | `LocalPortRowControl`, `DockerPortRowControl` (4-col) |
| `GlassCompactRowGrid` | Icon 20 + * + Shortcut 44 | `PopupActionRow`, footer actions, 3-col rows |

### Pane tab bar (Local Port / Docker Port)

Also called **pane tabs** or **Smart Category chips** (not a separate `TabBar` control).

| Artifact | Name |
| --- | --- |
| XAML region | `TrayPopupWindow` Grid Row 1 pane-tab host (`ClipToBounds=False`, same overflow model as chrome row) |
| Buttons | `LocalPaneTabButton`, `DockerPaneTabButton` |
| Styles | `GlassPaneTabBase` → `GlassPaneTabLocal`, `GlassPaneTabDocker` |
| Tokens | `PaneTab.Idle.Fill`, `PaneTab.Active.Fill`, `PaneTab.Active.Stroke`, `PaneTab.Docker.Accent` |
| Motion | `FluidAnimation.RunTabPush`, `PopIcon` |

Hide entire pane-tab row when `IsDockerSurfaceVisible` is false.

### Lists

| Style | Purpose | When to use |
| --- | --- | --- |
| `GlassPortListItem` | Port row hover = `Hover.Surface`; idle transparent | Local + Docker `ListBox` |
| `GlassEmbeddedPortListItem` | Settings excluded-port list | Based on `GlassPortListItem`; extra padding |

**Idle rule:** row background transparent. **Hover/selected/confirm:** surface appears per `portcheck.md` UI Material Contract.

### Footer & actions (action foundation)

| Style | Purpose | When to use |
| --- | --- | --- |
| `GlassFooterButton` | Transparent; hover wash | Refresh, Settings, Hide, Kill All |
| `GlassDivider` | Hairline separator | Sort menu, footer (override `Margin` for footer) |
| `GlassCapsuleActionButton` | 32px chrome capsule (`ChromeCapsuleRadius`) | Settings Add, inline commands |

### Inline row actions

| Style | Purpose | When to use |
| --- | --- | --- |
| `GlassRowKillButton` | “Kill” label, `Glass.Action.Danger.*` fill | Row confirm kill |
| `GlassRowKillIconButton` | 20px circular kill (path X) | Hover row kill affordance |
| `GlassRowDismissButton` | 20px circular ghost dismiss (path X) | Cancel confirm / remove excluded port |
| `Glass.Icon.DismissPath` | 8×8 vector cross | Row kill icon + dismiss buttons |

### Pop-up menus

| Style | Purpose | When to use |
| --- | --- | --- |
| `GlassSortMenuItem` | `Hover.Surface` hover/selected; no item stroke (matches `GlassPortListItem`) | Sort field/order items |
| `GlassDivider` | Menu separator | Sort menu groups |

### Settings layout

| Style | Purpose | When to use |
| --- | --- | --- |
| `GlassSectionCardBorder` | Light grouped section | Settings blocks |
| `GlassSectionDivider` | Section hairline | Between settings groups |

### Tray (system menu)

| Style | Purpose | When to use |
| --- | --- | --- |
| `GlassTrayMenuButton` | Tray context menu rows | `TrayHost` right-click menu |

---

## Reusable control modules (`Controls/`)

| Control | Path | Purpose | When to use |
| --- | --- | --- | --- |
| **GlassPopupShell** | `GlassPopupShell.xaml` | Captured blur + tint/sheen/rim; `ShellCornerRadius`, `UseMenuChrome` | Main popup backdrop (`MainGlassShell`) and sort menu (`ShellContent`) |
| **GlassSortMenuControl** | `GlassSortMenuControl.xaml` | Sort field + ascending/descending | Port list sort popup |
| **PopupActionRow** | `PopupActionRow.xaml` | Icon + label + shortcut columns | Footer actions (Refresh, Settings, Hide, Kill All) |
| **LocalPortRowControl** | `LocalPortRowControl.xaml` | Local listener row + kill flow | Local `ListBox` item template |
| **DockerPortRowControl** | `DockerPortRowControl.xaml` | Docker catalog row + container kill | Docker `ListBox` item template |
| **ExcludedPortRowControl** | `ExcludedPortRowControl.xaml` | Excluded port + remove | Settings port list |
| **SettingsIconBadge** | `SettingsIconBadge.xaml` | 22×22 gradient square + glyph | Settings section headers (Filter Ports, Scan Interval) |
| **GlassRoundButton** | `GlassRoundButton.cs` + `GlassRoundButton.xaml` | 32×32 liquid lens chrome (isolated module) | Sort, Settings back |

### Theme assets

| Asset | Path | Purpose |
| --- | --- | --- |
| `DockerPortLeadingIcon` | `Themes/DockerIcon.xaml` | Blue Docker path only (no tile background) on local rows when `IsDockerPublished` |
| `DockerIconGeometry.Logo` | `Helpers/DockerIconGeometry.cs` | Shared path data for tab + row |

---

## Motion (`Helpers/FluidAnimation.cs`)

Invoked from `TrayPopupWindow.xaml.cs` on pane tab click and pane changes.

| API | Behavior | Timing / easing |
| --- | --- | --- |
| `RunTabPush` | Animate tab `Width` collapsed ↔ expanded pill | **380ms**, `ElasticEase` (Oscillations=1, Springiness=4) |
| `SetPaneTabWidths` | Snap widths without animation | Local expanded **106**, Docker **118**, collapsed **32** |
| `RunPaneCrossfade` | Outgoing list opacity 1→0; incoming 0→1 + X **14→0** | Fade **220ms** (`QuadraticEase`); slide uses `SpringEase` |
| `PopIcon` | Scale **1 → 1.12 → 1** on tab tap | **280ms** total |
| `GlassRoundButtonInteractionAnimator` | Round chrome only: scale **→1.14**, **inward pinch** (~9.5%), gel follow **+dx/+dy** (~3.2px); press **→0.96** + under-finger glow | Enter **220ms** spring; leave **380ms**; honors reduce-motion |

### Micro-interaction rules

| Trigger | Motion |
| --- | --- |
| Tap pane tab | `PopIcon` on button; `RunTabPush` if pane changes |
| Switch pane | `RunPaneCrossfade` between `LocalPortsList` / `DockerPortsList` |
| First show | `SetPaneTabWidths` only (no crossfade) |
| Hover round chrome (`GlassRoundButton`) | Enlarge + inward pinch + gel follow; lens plate (backdrop crop); fresnel rim + top/bottom specular; press glow only while pressed |

**Not specified here:** label opacity animation on tab activate (labels appear via XAML triggers).

---

## GlassRoundButton — liquid chrome contract

### Ownership boundary (do not mix with global liquid glass)

| Owned by `GlassRoundButton` | Global liquid glass (`LiquidGlass.xaml`, other controls) |
| --- | --- |
| `Controls/GlassRoundButton.cs` | `GlassPopupShell` menu chrome, tint/sheen, popup blur |
| `Themes/GlassRoundButton.xaml` (`GlassRound.*` brushes, template `PART_*`) | Pane tabs, rows, search capsule, dividers |
| `Controls/GlassRoundButtonInteractionAnimator.cs` | `FluidAnimation.cs` (pane crossfade / tab push) |
| `Helpers/GlassRoundLensBackdrop.cs` | `BackdropBlurHelper` (shared capture primitive only) |
| `Effects/GlassRoundLensEffect.cs` + `Shaders/GlassRoundLensEffect.ps` (not loaded at runtime) | — |
| `Validation/GlassRoundButtonHarness.cs`, `App` `--validate-glass-round-button` | — |

**Integration only (not round-button styling):**

- `GlassPopupShell.TryGetRoundButtonLensBackdrop` — internal cache read for lens crop.
- Host row in `TrayPopupWindow.xaml` sets `ClipToBounds="False"` so `PART_Outer` scale can overflow 32×32.

Copy this module when adding liquid glass to another control: duplicate the control + theme + animator pattern; do not extend `LiquidGlass.xaml` round-button `PART_*` names globally.

Canonical implementation: `Controls/GlassRoundButton.cs`, `Themes/GlassRoundButton.xaml`, `Controls/GlassRoundButtonInteractionAnimator.cs`.

### Product intent (WPF tier-A motion)

| Channel | Behavior |
| --- | --- |
| **Translucency** | Idle frost `PART_Bd` ~0.88 opacity; hover fill near-clear, `PART_Bd` ~0.05 |
| **Enlarge** | `PART_Outer` / `PART_Scale` uniform **×1.14** spring on enter |
| **Inward collapse** | `PART_InnerPinch` up to **~9.5%** toward cursor (never outward stretch) |
| **Gel follow** | `PART_GelFollow` on `PART_Root` — **+dx/+dy** (~**3.2px** max) |
| **Press** | Brief **×0.96** on outer scale; `PART_PressGlow` under finger only while pressed |
| **Inner lens** | `PART_LensPlate`: crop popup shell cache or screen capture; light `BlurEffect` (~0.6). Pixel shader **off** at runtime (hang risk on 32×32). |
| **Rim / specular** | `PART_FresnelRim`, `PART_TopSpecular`, `PART_BottomSpecular`, `PART_RimHighlight` fade in on hover |
| **Reduce motion** | When `SystemParameters.ClientAreaAnimation` is false, snap values without lerp |

### Template layering (bottom → top)

WPF **bitmap effects on a parent blur all descendants**. Glass and icon must stay separated.

| Part | Hit-test | Role |
| --- | --- | --- |
| `PART_HitTarget` | **Yes** (transparent full circle) | Defines **entire 32×32** hover/click area |
| `PART_Outer` / `PART_Scale` | No | Hover enlarge (outer grid; one transform per element for `FindName`) |
| `PART_Root` / `PART_GelFollow` | No | Gel translation with glass stack |
| `PART_ShadowHost` | No | `DropShadowEffect` only here |
| `PART_LensPlate` | No | Backdrop crop on hover; no effect on icon parent |
| `PART_Bd` | No | Idle frost fill + rim; fades on hover |
| `PART_FresnelRim` / speculars | No | Hover glass read |
| `PART_PressGlow` | No | Press-only |
| `PART_Icon` (`ContentPresenter`) | **No** | Sharp glyph — no `Text.Shadow`, no effect on ancestors |

### Icon clarity rules (mandatory)

1. **Never** assign `Effect` (blur/shadow) on `PART_Root` or any parent of `PART_Icon`.
2. **Never** use `Text.Shadow` on glyphs inside `GlassRoundButton`.
3. Use `TextOptions.TextRenderingMode="ClearType"` on `PART_Icon`.
4. Decorative glass borders use `IsHitTestVisible="False"` except `PART_HitTarget`.

### Hit-testing

Custom templates shrink the clickable region to hit-testable children. Without `PART_HitTarget`, only the small glyph receives hits (reported bug: hover limited to sort icon). The transparent full-size circle restores **whole-disc** hover, press, and tooltip.

### When to use

Sort/filter, Settings back, any single 32×32 chrome glyph on the popup. Not for pane tabs (width morph via `FluidAnimation`) or footer text rows.

---

## Composition rules

### Single glass-heavy container

The **popup outer bubble** (`TrayPopupWindow` `OuterChromeBorder`) is the only persistent full material stack. Settings sections use `GlassSectionCardBorder` (light grouping), not a second blur capture.

### Action foundation

`Refresh`, `Settings`, `Hide`, and list-toolbar `Kill All` share the **footer action language**: `GlassFooterButton` + `PopupActionRow`, hover illumination only, no persistent chip outline.

Port list rows **derive hover language** from action foundation (ghost/danger fills) but:

- Use **smaller** `ListHoverRadius` (10) than footer (8) hover chrome
- Stay **flat at idle**
- Never look like footer menu chips

### Chrome vs tabs vs rows

| System | Foundation | Do not use for |
| --- | --- | --- |
| Search capsule | `Search.Fill` / inset chrome | Row kill, footer actions |
| Pane tabs | `GlassPaneTabLocal/Docker` | Footer or list rows |
| Port rows | `GlassPortListItem` | Footer buttons |

### Leading icons (local list)

| Row state | Leading marker |
| --- | --- |
| Normal listener | 6px green dot (`Status.Active`) |
| Docker-published on host | `DockerPortLeadingIcon` (blue path, 15×12) — not dot + text badge |

### Docker pane tab

- Idle: 32px circle, `#22FFFFFF`, icon only
- Active: frosted pill `#72FFFFFF`, stroke `#A0FFFFFF`, icon + SemiBold label
- Whale/path accent `#1D63ED`; **no** solid blue pill background

---

## Surface map (where modules compose)

```text
TrayPopupWindow
├── OuterChromeBorder (shadow)
├── MainGlassShell (backdrop + tint + sheen + rim)  … GlassPopupShell, UseMenuChrome=false
├── InnerChromeBorder (margin 2, radius 18)
├── Row 0 Chrome
│   ├── Search capsule                              … Search.*
│   ├── GlassRoundButton → GlassPopupShell          … GlassSortMenuControl
│   └── Settings header (back + title)            … GlassRoundButton
├── Row 1 Pane tabs (if IsDockerSurfaceVisible)     … GlassPaneTab*
├── Row 2 Lists                                     … GlassPortListItem*
│   ├── LocalPortRowControl / DockerPortRowControl
│   └── Empty states (Text.Secondary + Text.Shadow)
├── Row 3 Footer                                    … GlassFooterButton + PopupActionRow
└── Settings overlay (same shell)                   … GlassSection* + ExcludedPortRowControl
```

---

## File map

| Concern | Path |
| --- | --- |
| Tokens & styles | `src/PortCheck/Themes/LiquidGlass.xaml` |
| Docker icon template | `src/PortCheck/Themes/DockerIcon.xaml` |
| App merge | `src/PortCheck/App.xaml` |
| Main popup layout | `src/PortCheck/TrayPopupWindow.xaml` |
| Motion | `src/PortCheck/Helpers/FluidAnimation.cs` |
| Round chrome motion | `src/PortCheck/Controls/GlassRoundButtonInteractionAnimator.cs` |
| Round chrome control | `src/PortCheck/Controls/GlassRoundButton.cs`, `Themes/GlassRoundButton.xaml` |
| Backdrop capture | `src/PortCheck/Helpers/BackdropBlurHelper.cs` |
| Product behavior | `docs/spec/portcheck.md` |

---

## Module hygiene (2026-05-29)

Dedup pass complete. Canonical keys:

| Was | Now |
| --- | --- |
| `GlassSortFilterButton` / `GlassChromeIconButton` | **`GlassRoundButton`** |
| `GlassPortListItemLocal` / `Docker` | **`GlassPortListItem`** |
| Legacy row `*Extracted` styles | **`GlassRowKillButton`**, **`GlassRowKillIconButton`**, **`GlassRowDismissButton`** |
| Inline popup stack | **`GlassPopupShell`** |
| `GlassPopupMenuDivider` / `GlassFooterDivider` | **`GlassDivider`** (footer overrides `Margin`) |
| `GlassCapsuleActionButtonRounded` / `GlassPillButton` | **`GlassCapsuleActionButton`** |
| `GlassCapsuleDangerButton`, `GlassPillButtonDanger` | **Removed** (unused) |
| Inline Settings icon borders | **`SettingsIconBadge`** |
| `Glass.TintEdge`, `Glass.Overlay`, `Glass.StrokeBright` | **Removed** |
| `Glass.Icon.CrossGeometry` | **`Glass.Icon.DismissPath`** (used by row actions) |
| `Danger` token | **`Glass.Action.Danger.Foreground`** |

**Intentional variants (keep):** `GlassCompactRowGrid` (3-col), paired row kill/dismiss templates, `ConfirmDialog` separate stack.

**Rule:** One visual control → one style key. No alias-only styles.

---

## Change discipline

1. New reusable visual behavior → add or extend a named resource in `LiquidGlass.xaml` (or a `Controls/*` module), then reference it here.
2. Aspirational-only experiments → document under **Not implemented** until shipped.
3. Do not duplicate footer chip styling on port rows or nested blur shells inside Settings.
4. Do not add alias styles (`Foo` / `FooChrome`) — extend the canonical key or document why a variant is required.
