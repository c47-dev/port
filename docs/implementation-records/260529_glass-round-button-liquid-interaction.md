# GlassRoundButton — Liquid / Glass Interaction (Research + Change Plan)

**Status:** Phases 1–3 implemented (motion + faux rim/glow). Phase 4 (pane tab parity) not started.  
**Governing UI spec:** [`docs/spec/liquid-glass-uiux.md`](../spec/liquid-glass-uiux.md)  
**Related motion:** [`FluidAnimation.cs`](../../src/PortCheck/Helpers/FluidAnimation.cs) (pane tabs today)  
**Target control:** `GlassRoundButton` (`Themes/LiquidGlass.xaml`, `TrayPopupWindow` sort + Settings back)

---

## 1. Problem statement

PortCheck popup chrome already has **Mail-style pane tab** motion (width spring, icon pop, cross-fade). `GlassRoundButton` is still **static apart from instant XAML triggers** (background/border swap on hover/press).

The desired experience for `GlassRoundButton`:

| Phase | User-visible behavior |
| --- | --- |
| **Hover enter** | Control **grows** as if a liquid glass lens is inflating: translucency reads stronger, slight **3D lift** (scale + shadow/sheen), not a flat opacity jump |
| **Hover move** | While the pointer stays inside, **circling the cursor** makes the disc **stretch slightly toward the pointer** (gel flex), as if the glass membrane follows touch |
| **Hover leave / press end** | Material **relaxes back** to the idle 32×32 circle over ~300–450ms with spring easing |
| **Press** | Brief **compression** or inward energize (Apple: glow from under fingertip), then settle on release or exit |

This is **more than** current tab animation (uniform scale pop on tap only). It is closer to Apple’s **`.glassEffect(.interactive())`** + **lensing / gel** language from WWDC 2025.

---

## 2. Research — how Apple describes Liquid Glass

Sources: [Apple Newsroom (Jun 2025)](https://www.apple.com/newsroom/2025/06/apple-introduces-a-delightful-and-elegant-new-software-design/), [Meet Liquid Glass (WWDC25)](https://www.youtube.com/watch?v=IrGYUq1mklk), [Liquid Glass — Apple Developer](https://developer.apple.com/documentation/technologyoverviews/liquid-glass), community distillations ([LiquidGlassReference](https://github.com/conorluddy/liquidglassreference), [Design Milk overview](https://design-milk.com/apple-returns-to-a-layered-software-design-with-liquid-glass-interface/)).

### 2.1 Material model (not “blur only”)

Apple positions Liquid Glass as a **digital meta-material**, not a flat blur:

| Mechanism | What Apple claims | Perceptual effect |
| --- | --- | --- |
| **Lensing** | Light **bends and concentrates** through the control; background appears warped, not just frosted | “Thick glass” / magnifier over content |
| **Translucency** | Layer sits **above** content; color **informed by** what is behind | Content bleeds through; hierarchy |
| **Specular highlights** | Highlights move with **device motion** and interaction | Glass edge catches light |
| **Adaptive thickness** | When glass **grows** (toolbar → menu), material acts **thicker**: deeper shadow, stronger lensing, softer scatter | Size change = mass change |
| **Appear / disappear** | Objects **materialize** by modulating lensing—not only opacity fade | Optical integrity on enter/exit |
| **Internal glow** | On touch, light **starts under finger** and spreads through element and **nearby** glass | Energized, fluid feedback |

### 2.2 Motion / “liquid” behaviors

| Behavior | Description |
| --- | --- |
| **Gel flexibility** | Material flexes and morphs organically; responds to **touch and drag** |
| **Interactive controls** | `.interactive()` on `glassEffect`: **scale on press**, **bounce**, **shimmer**, **touch-point illumination**, **drag response** |
| **Morphing** | `glassEffectID` + `GlassEffectContainer`: shapes **blend/morph** between states (glass cannot sample glass without shared container) |
| **Navigation** | Tab bars **shrink/expand** with scroll; sidebars **refract** content behind |

### 2.3 What third-party replicas emphasize (web / shaders)

Open implementations ([liquid-glass-studio](https://github.com/Acadbek/liquid-glass-studio), [Ken Sorrell lens shader](https://www.sorrell.info/blog/liquid-glass-lens-effect)) break “glass” into:

- **Refraction / dispersion** (Snell-style offset in fragment shader)
- **Fresnel** rim brightness at grazing angles
- **Gaussian blur** of captured background
- **Mouse-follow** uniforms driving lens center and stretch
- **SDF blobs** for smooth merging (related to `GlassEffectContainer` morphing)

**Takeaway for PortCheck:** True Apple fidelity needs **GPU lensing + shared sampling region**. WPF popup today has **screen-capture blur + fixed tint** only ([`liquid-glass-uiux.md`](../spec/liquid-glass-uiux.md) — Runtime implementation). Interaction motion can still approach the *feel* without full optics.

---

## 3. Translation — your abstract spec → measurable motion

Your description maps cleanly onto decomposed **animation channels**:

```text
                    ┌─────────────────────────────────────┐
  Idle (32×32)      │  Hover envelope                      │
  circle            │  ┌──────────────────────────────┐  │
                    │  │ Scale 1.0 → 1.08–1.12 (spring) │  │
                    │  │ Sheen/rim shift (pseudo-3D)    │  │
                    │  │ Optional shadow deepen         │  │
                    │  └──────────────────────────────┘  │
                    │         + cursor loop:            │
                    │  ┌──────────────────────────────┐  │
                    │  │ Stretch vector toward cursor │  │
                    │  │ (asymmetric scale / skew)    │  │
                    │  │ Max ~4–8% elongation         │  │
                    │  └──────────────────────────────┘  │
                    └─────────────────────────────────────┘
                                      │
                          Leave / Press release
                                      ▼
                    Spring return to Scale 1.0, zero stretch
                    (380ms class, match TabPush family)
```

| Your phrase | Concrete parameter (proposal) |
| --- | --- |
| 透視 / 玻璃 / 少許 3D | Scale up + **rim highlight translation** toward pointer + **slightly stronger** `PaneTab.Active.Fill` / inner sheen opacity (not full lens shader) |
| enlarge | Uniform scale **1.10** (match existing `PopIcon` peak 1.12, sustain while hovered) |
| cursor circling → stretch | **Directional scale**: `scaleX = 1 + k*|dx|`, `scaleY = 1 + k*|dy|` capped, or 2D skew from cursor angle |
| 慢慢縮小 | **380ms** `ElasticEase` (same family as `FluidAnimation.TabPushDuration`) or **320ms** `QuadraticEase` out |
| 按下去 | Scale **0.96–0.98** for 80–120ms + border → `PaneTab.Active.Stroke` (already in template) |

---

## 4. Current PortCheck baseline

| Asset | Today |
| --- | --- |
| `GlassRoundButton` | XAML `ControlTemplate`: circle border, instant `IsMouseOver` / `IsPressed` setters |
| `FluidAnimation.PopIcon` | 1 → 1.12 → 1 over **280ms** on **pane tab tap** only |
| `FluidAnimation.RunTabPush` | Width spring **380ms** — closest “liquid” precedent in-repo |
| Backdrop | `GlassPopupShell` + `BackdropBlurHelper` — **no** dynamic refraction |
| Pane tabs | Width morph + icon pop — **no** cursor-follow stretch |

**Gap:** No `MouseMove` handling on chrome buttons; no layered template for sheen/rim animation; no shared “interactive glass” helper.

---

## 5. Fidelity tiers (honest scope)

| Tier | Optics | Motion | Effort | Matches Apple TV ad? |
| --- | --- | --- | --- | --- |
| **A — Motion-only** | Existing blur/tint | Scale spring + directional stretch + rim offset | Medium | Feel only |
| **B — Layered faux lens** | Extra `Border`/`Ellipse` highlight + opacity ramps | Tier A + press glow pulse | Medium–high | Closer |
| **C — Shader lens** | D3D/WPF shader effect on 32×32 region | Tier A + live refraction map | High | Partial |
| **D — Platform-native** | N/A on WPF tray | N/A | — | iOS/macOS only |

**Recommendation:** Ship **Tier A** for `GlassRoundButton`, design template hooks for **Tier B**, document **Tier C** as future if popup moves to WinUI3/Composition APIs with brush effects.

**Do not** update `liquid-glass-uiux.md` “Runtime implementation” to claim lensing until Tier C exists.

---

## 6. Technical approach options (WPF)

### Option 1 — `GlassRoundButton` custom `ControlTemplate` + code-behind (recommended)

**Structure:**

```text
GlassRoundButton (Button, Style uses template)
└── Grid (RenderTransformOrigin 0.5,0.5)
    ├── Border "Bd" (fill, stroke)           ← existing
    ├── Border "RimHighlight" (gradient)    ← NEW, opacity 0 idle → 0.6 hover
    ├── Border "PressGlow" (radial)         ← NEW, visible on IsPressed
    └── ContentPresenter
```

**Code:** `GlassRoundButtonChrome.cs` attached to template via `TemplatePart`, or subclass `Button` used only for round chrome.

| Pros | Cons |
| --- | --- |
| Keeps one style key `GlassRoundButton` | Requires template contract + parts |
| Cursor tracking in one place | Slightly more complex than behavior |

### Option 2 — Attached behavior `GlassLiquidInteractionBehavior`

Apply to any `Button` with `GlassRoundButton` style.

| Pros | Cons |
| --- | --- |
| Reusable on future chrome | Harder to drive template parts by name |
| Less invasive to XAML | Behaviors + templates can fight |

### Option 3 — Composition-only (no code)

Pure XAML triggers only.

| Pros | Cons |
| --- | --- |
| Simple | **Cannot** do cursor-direction stretch |

**Decision:** **Option 1** — cursor stretch **requires** `MouseMove` code.

---

## 7. Proposed implementation — `GlassLiquidInteraction` module

### 7.1 New files

| File | Responsibility |
| --- | --- |
| `Helpers/GlassLiquidInteractionAnimator.cs` | Spring scale, stretch matrix, highlight offset math; timing constants |
| `Controls/GlassRoundButtonChrome.cs` + optional XAML | Template parts, wires mouse enter/move/leave/press |
| Update `Themes/LiquidGlass.xaml` | `GlassRoundButton` template layers + `RenderTransformOrigin` |

### 7.2 Animation contract (align with `FluidAnimation`)

| Constant | Proposed value | Notes |
| --- | --- | --- |
| `HoverEnterScale` | `1.10` | Align with `PopIcon` peak 1.12 |
| `HoverLeaveDuration` | `380ms` | Match `TabPushDuration` |
| `PressScale` | `0.97` | Brief compress |
| `MaxStretch` | `0.06` | 6% elongation toward cursor |
| `StretchSmoothing` | `0.35` | Lerp factor per frame or per MouseMove |
| `SpringEase` | Reuse `FluidAnimation.SpringEase` | Visual family consistency |

### 7.3 Cursor stretch algorithm (sketch)

```csharp
// center = ActualWidth/2, ActualHeight/2
// v = normalize(pointer - center), clamp magnitude
// scaleX = hoverScale * (1 + maxStretch * |v.X|)
// scaleY = hoverScale * (1 + maxStretch * |v.Y|)
// optional: rimTranslate = v * 2px
```

Use `MatrixTransform` or `ScaleTransform` + `TranslateTransform` on outer `Grid`. **Do not** animate `Width`/`Height` (layout thrash).

### 7.4 Pseudo-3D without lens shader

| Layer | Hover | Press |
| --- | --- | --- |
| Fill | Lerp `PaneTab.Idle.Fill` → `#58FFFFFF` | `PaneTab.Active.Fill` |
| Rim | Translate highlight gradient toward cursor | Stronger stroke `PaneTab.Active.Stroke` |
| Shadow | Optional `DropShadowEffect` blur 8→12, depth 2→4 | — |

### 7.5 Integration points

| Consumer | Change |
| --- | --- |
| `SortFilterButton` | No XAML change if style template updated |
| `BackButton` (Settings) | Same |
| `FluidAnimation.PopIcon` | **Do not** double-scale on pane tabs; round button is separate control |

### 7.6 Accessibility / settings

| Concern | Mitigation |
| --- | --- |
| Reduce motion | Respect `SystemParameters.ClientAreaAnimation` or app flag → instant state, no stretch |
| Reduce transparency | Already separate; motion can stay |
| CPU on MouseMove | Throttle to 60Hz; only animate while `IsMouseOver` |

---

## 8. Phased delivery plan

### Phase 0 — Design lock (this document)

- [x] Research Apple language
- [ ] User confirms stretch strength + scale peak (1.10 vs 1.12)
- [ ] User confirms scope: **GlassRoundButton only** first (not pane tabs)

### Phase 1 — Hover scale spring (MVP)

**Questions:** Should press use same spring as leave?  
**Why:** Validates “liquid enlarge” without cursor complexity.  
**How:** `GlassLiquidInteractionAnimator.RunHoverEnter/Leave`; template `Grid` scale.  
**Test:** Capture sort button hover; compare to pane tab pop; `dotnet build`; manual hover in tray popup.

### Phase 2 — Cursor-follow stretch

**Questions:** Elliptical stretch vs skew? Cap at 6%?  
**Why:** Core differentiator from current UI.  
**How:** `MouseMove` on template root; lerp stretch; reset on leave.  
**Test:** Screen recording or burst captures at N/E/S/W cursor positions; verify no jitter at edge.

### Phase 3 — Rim / glow (Tier B optics)

**Questions:** Is radial press glow worth complexity on 32px control?  
**Why:** Sells “glass” without shader.  
**How:** Extra template borders; animate opacity/offset with pointer.  
**Test:** Side-by-side with Phase 2 capture.

### Phase 4 — Spec + optional pane tab parity

**Why:** Tabs already width-morph; only add stretch if product wants consistency.  
**How:** Extract shared `GlassLiquidInteractionAnimator` calls from tab buttons (larger scope).  
**Test:** Full popup capture + Docker tab switch regression.

**Out of scope for initial delivery:**

- Real-time lensing shader on `GlassRoundButton`
- `GlassEffectContainer`-style morph between sort menu and button
- Applying stretch to footer rows or list items

---

## 9. Validation criteria

| ID | Criterion | Method |
| --- | --- | --- |
| V1 | Hover enter: circle grows with spring, not instant | Visual / 60fps recording |
| V2 | Cursor at N/E/S/W: visible directional stretch, returns to symmetric at center | Manual + optional `--capture-*` if hook added |
| V3 | Leave: returns to 32×32 within ~380ms without layout shift | Measure `ActualWidth` unchanged |
| V4 | Press: compress then release; sort menu still opens | Functional click test |
| V5 | Settings back + sort filter both behave identically | Two-control pass |
| V6 | Pane tabs unchanged (Phase 1–3) | Regression capture `popup-capture.png` |
| V7 | Reduce motion: no stretch/pop | System setting test |
| V8 | Build Release/Debug 0 errors | CI / local build |

**Capture commands (existing harness):**

```text
PortCheck.exe --capture-to=artifacts/popup-capture.png
PortCheck.exe --capture-surface=settings --capture-to=artifacts/popup-capture-settings.png
```

Add optional `--capture-surface=round-button-hover` if automation needed (mirror `kill-confirm` pattern).

---

## 10. Open questions (need product answer before Develop)

| # | Question | Default if silent |
| --- | --- | --- |
| Q1 | Scale peak: **1.10** or **1.12**? | 1.10 |
| Q2 | Max stretch: **4%**, **6%**, or **8%**? | 6% |
| Q3 | Apply to **pane tab circles** in same phase or round buttons only? | Round only |
| Q4 | Stretch on **press-and-hold** without hover (touch)? | N/A desktop |
| Q5 | Accept **fake 3D** (scale+sheen) without real refraction for v1? | Yes |
| Q6 | Performance cap: disable stretch when popup backdrop is updating? | Skip stretch during capture refresh |

---

## 11. Spec document updates (after implementation)

When code ships, update [`liquid-glass-uiux.md`](../spec/liquid-glass-uiux.md):

| Section | Addition |
| --- | --- |
| `GlassRoundButton` | Hover spring, cursor stretch, press compress |
| Motion | New APIs in `GlassLiquidInteractionAnimator` |
| Not implemented | GPU lensing on chrome buttons (still) |
| Module hygiene | Remove “interactive TBD” if any |

---

## 12. References

| Resource | URL |
| --- | --- |
| Apple Newsroom — Liquid Glass | https://www.apple.com/newsroom/2025/06/apple-introduces-a-delightful-and-elegant-new-software-design/ |
| WWDC25 — Meet Liquid Glass | https://www.youtube.com/watch?v=IrGYUq1mklk |
| Apple — Liquid Glass overview | https://developer.apple.com/documentation/technologyoverviews/liquid-glass |
| Apple — Adopting Liquid Glass | https://developer.apple.com/documentation/TechnologyOverviews/adopting-liquid-glass |
| Community API reference | https://github.com/conorluddy/liquidglassreference |
| Web lens replication | https://www.sorrell.info/blog/liquid-glass-lens-effect |
| WebGL glass lab | https://github.com/Acadbek/liquid-glass-studio |
| PortCheck UI spec | [`docs/spec/liquid-glass-uiux.md`](../spec/liquid-glass-uiux.md) |

---

## Changelog

| Date | Change |
| --- | --- |
| 2026-05-29 | Initial research + change plan from user request and web research |
