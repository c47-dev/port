## PRD

### Problem statement

The latest popup styling introduced three regressions:

- Footer hover behavior no longer matches the accepted original interaction.
- The top-right count badge is visually clipped.
- The dismiss/cross glyph choice is wrong.

### Goals

- Restore original footer hover behavior with no shared liquid-glass shell change.
- Remove the badge clipping.
- Replace dismiss glyphs with the intended cross glyph.
- Preserve the lighter text shadow improvement where it does not break layout.

### Scope boundaries

- In scope: `src/PortCheck/Themes/LiquidGlass.xaml`, `src/PortCheck/TrayPopupWindow.xaml`
- Out of scope: services, view models, blur pipeline logic

### Completion criteria

- Footer hover returns to original accepted behavior.
- Count badge no longer clips.
- Dismiss buttons use the intended glyph.
- Build succeeds.
