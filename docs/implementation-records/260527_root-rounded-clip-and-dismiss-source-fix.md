## PRD

### Problem statement

The popup still exposes rectangular child rendering inside rounded corners, and the dismiss button still does not follow the intended plain cross because the style template owns a different glyph than the caller.

### Goals

- Make popup corner clipping real instead of decorative only.
- Remove rectangular bleed for backdrop and any child content near rounded corners.
- Make dismiss button glyph ownership single-source and predictable.
- Remove current popup text literal corruption tied to the same surface.

### Non-goals

- No service or view-model behavior changes.
- No blur pipeline redesign.
- No footer interaction redesign.

### Success criteria

- Corner-adjacent content renders inside a true rounded clip.
- The backdrop does not expose rectangular child content at rounded corners.
- Dismiss buttons render the intended plain cross from one source only.
- `dotnet build` succeeds.

## TDD

### Technical approach

- Apply `RectangleGeometry` clips to the actual popup content roots that own child rendering.
- Keep `Border CornerRadius` for drawing only; stop relying on it as a child clip mechanism.
- Change dismiss button templates to use `ContentPresenter` so usage controls the glyph.
- Normalize popup literals that were corrupted in the same dependency chain.

### Ownership boundaries

- `src/PortCheck/TrayPopupWindow.xaml`
- `src/PortCheck/TrayPopupWindow.xaml.cs`
- `src/PortCheck/Themes/LiquidGlass.xaml`

### Validation strategy

- Build the solution.
- Run a local render verification harness against `TrayPopupWindow` and inspect the produced image for rounded-corner clipping and dismiss glyph output.
