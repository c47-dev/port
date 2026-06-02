# GlassRoundButton — Lens shader (WPF-Liquid-Glass-Effect adoption)

**Date:** 2026-06-01  
**Governing spec:** [`docs/spec/liquid-glass-uiux.md`](../spec/liquid-glass-uiux.md)  
**Reference:** [dragosniamtu/WPF-Liquid-Glass-Effect](https://github.com/dragosniamtu/WPF-Liquid-Glass-Effect) (MIT, `GlassyEffect.ps`)

## PRD

### Problem

Round chrome has Tier-A motion (scale, inward pinch, gel follow) but no **mirror + backdrop warp** on hover.

### Goals

| Channel | Behavior |
| --- | --- |
| Hover | Keep **outer** `PART_Scale` + `PART_GelFollow` motion unchanged |
| Hover | Add **inner** captured-backdrop layer with pixel-shader distortion (subtle) |
| Hover | Add **mirror** sheen layer (specular read) |
| Press / leave | Lens layers fade with existing timings; icon stays sharp |

### Non-goals

- Full-window liquid glass (popup shell unchanged)
- Pane tab motion parity
- Hide-window capture loop per frame

## TDD

### Approach

1. Vendor `GlassRoundLensEffect.ps` (from reference `GlassyEffect.ps`).
2. `GlassRoundLensEffect` `ShaderEffect` wrapper — smaller `BlurIntensity`, cursor-driven `GlassCenter`.
3. `GlassRoundLensBackdrop` — crop popup `GlassPopupShell` cache or `BackdropBlurHelper` 48×48 device capture.
4. Template: `PART_LensBackdrop` + `PART_LensDistort` (effect host) + `PART_MirrorSheen` under icon.
5. Extend `GlassLiquidInteractionAnimator` for lens/mirror opacities + shader uniforms only.

### Completion criteria

- [x] Build succeeds with embedded `.ps` resource
- [x] Hover: scale/gel unchanged; lens + mirror visible (manual tray QA pending)
- [x] Icon never under `Effect` parent
- [x] Spec updated: round chrome lensing **partial** (32×32 only)
