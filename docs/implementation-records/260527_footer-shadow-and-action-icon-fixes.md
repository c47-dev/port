## PRD

### Problem statement

Current popup UI still has three presentation issues:

- Port-row dismiss/cross icon and Hide footer icon do not match the intended icon semantics.
- `Kill All`, `Refresh`, and `Hide` footer actions are still visually weaker than the extracted liquid-glass action language.
- White text over bright backgrounds can lose contrast because the current UI relies on fill color alone with no text shadow support.

### Goals

- Move the cross icon language to port-row dismiss controls.
- Change Hide footer action to use a minimize icon.
- Apply liquid-glass treatment to the footer actions.
- Add text shadow, not border, to improve bright-background readability.
- Keep behavior unchanged.

### Non-goals

- No service/view-model logic changes.
- No product flow changes.
- No blur pipeline change in this patch.

### Scope boundaries

- In scope: `src/PortCheck/Themes/LiquidGlass.xaml`, `src/PortCheck/TrayPopupWindow.xaml`
- Out of scope: runtime behavior, refresh logic, service layer

## TDD

### Technical approach

- Add reusable text shadow effect and shadowed text styles in theme resources.
- Upgrade footer button shell from hover-only to persistent liquid-glass shell.
- Replace Hide footer glyph with a minimize glyph.
- Use the cross/dismiss visual language only on row dismiss controls.

### Validation strategy

- Build the solution after the XAML/theme changes.
- Verify the popup compiles with updated resource keys and styles.

### Completion criteria

- Footer actions use liquid-glass shell styling.
- Hide uses minimize icon semantics.
- Row dismiss controls keep cross semantics.
- Text shadow is applied through theme resources.
- Build succeeds.
