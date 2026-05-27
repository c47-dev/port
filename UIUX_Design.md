
# Liquid Glass UI/UX Design System

## Core Visual & Structural Rules

### 1. Material & Lensing Physics

* **Translucency & Opacity:** Never use solid, flat backgrounds for container UI. Use a dynamic opacity scale ranging from **20% to 80%** depending on content depth.
* **Real-time Refraction (Lensing):** Background elements passing beneath the container must subtly bend, warp, and concentrate light, simulating physical glass optics.
* **Dynamic Background Blending:** The material must dynamically alter its tint based on the colors of the wallpaper or content immediately behind it.
* **Specular Highlights:** Apply a razor-thin, high-contrast inner border or top-edge highlight that shifts relative to interaction, giving the illusion of a reflective glass lip.

### 2. Layout, Geometry & Hierarchy

* **Floating Component Bubble Architecture:** UI containers must exist as detached, floating bubbles layer-stacked above the content canvas.
* **Concentric Rounded Geometry:** Corner radiuses must be highly pronounced and perfectly concentric.
* **Soft Spatial Shadows:** Use expansive, low-opacity, high-blur multi-layered drop shadows to elevate the glass layer.
* **Avoid Nested Glass Stacking:** Prefer variable text weight, subtle dividers, or structural spacing rather than another glass container.

### 3. Typography & Interface Contrast

* **Adaptive Text Toning:** Text and icon colors must automatically shift in contrast/luminance depending on the background behind the glass.
* **Hierarchy via Weight & Scale:** Bold headers and crisp system spacing counteract transparent backgrounds.

---

## Component-Specific Implementation

| Component Element | Specific Style Rule |
| --- | --- |
| **Search/Header Bar** | Inset capsule with subtle inner shadow. Muted opacities until active. |
| **Status Indicators** | Vibrant, high-saturation markers on a neutral glass stage — not flat text badges. |
| **Interactive Rows** | Hover uses internal glow / step-up illumination, not solid blocks. |
| **Alignment** | Icon + shortcut columns align across surfaces; **do not** reuse the same corner radius / row height on list rows vs footer menu chips. |
| **Action Items & Alerts** | Destructive actions use focused high-contrast text; avoid full-width solid warning fills. |

---

## iOS 18 Mail Smart Categories (Pane Tabs)

Reference: iOS 18 Mail top category chips — horizontal scroll, frosted glass, fluid spring motion.

### UI/UX 設計與動畫細節

| Dimension | Rule |
| --- | --- |
| **Layout** | Borderless horizontal `ScrollViewer`; chips left-aligned under search |
| **Idle chip** | 32px circle, `#22FFFFFF` glass, thin rim; **icon only** |
| **Active chip** | Expands to pill; `#72FFFFFF` frosted fill + `#A0FFFFFF` stroke; **icon + label** SemiBold |
| **Interaction** | Tap chip or switch list → spring pop on icon (scale 1→1.12→1) |
| **Content** | List cross-fade + 14px X slide, **220ms**; `ElasticEase` spring |
| **Counter** | Port count badge in search row (pop on refresh optional) |

**Do not** use solid opaque pills (e.g. full `#1D63ED` block) — accent color only on **icons** (Docker seeklogo blue), not tab background.

### PortCheck mapping

| Tab | Idle | Active |
| --- | --- | --- |
| Local Port | Segoe Fluent Laptop `E7F8`, muted | Frosted pill + “Local Port” (width spring pushes Docker chip) |
| Docker Port | 24×24 whale path, Docker blue | Frosted pill + “Docker Port” |

**Visibility gate:** No Engine / no published TCP → **hide entire tab strip** (no empty Docker messaging).

**WPF:** `GlassPaneTabLocal`, `GlassPaneTabDocker`, `FluidAnimation.cs`, `TrayPopupWindow` scroll + pane crossfade.

### 視覺 UI Prompt (AI / Figma)

```text
UI design of Apple iOS 18 Mail app, top navigation Smart Categories, horizontal scrollable tabs,
Primary Transactions Updates Promotions style. Minimalist Liquid Glass aesthetic, translucent
frosted glass blurred background, subtle colorful icons per category, dark mode, SF Pro,
high-fidelity mockup, clean interface.
```

### 動效 Prompt (Figma Smart Animate / WPF)

| Trigger | Motion |
| --- | --- |
| On Tap tab | Chip idle→active width; icon scale pop; label fade in |
| On pane change | Outgoing list opacity 1→0; incoming 0→1 + X 14→0 |
| Spring | Mass 1, Stiffness 120, Damping 15 — or WPF `ElasticEase` Oscillations=1 Springiness=4 |
| Duration | Fade **220ms** (content), pop **280ms** total |

```csharp
// SwiftUI reference (conceptual parity in WPF via ElasticEase)
withAnimation(.spring(response: 0.35, dampingFraction: 0.75)) { selectedCategory = .transactions }
```

### 圖標微互動 (PortCheck)

| Tab | Micro-interaction |
| --- | --- |
| Local | Plug icon scale pop on select |
| Docker | Whale icon scale pop; row tile uses 24×24 geometry (no nested white box on chip) |

---

## Local Port Row Leading Icon

| Row type | Leading marker | Not used |
| --- | --- | --- |
| Normal local listener | Green dot (6px), `Status.Active` | — |
| Docker-published host port | **Docker tile** (20×20, `#1D63ED`, white seeklogo mark) | ~~Green dot~~, ~~`Docker` text badge~~ |

Mirrors Mail list leading squares: one recognizable icon slot per row, no duplicate badge + dot.

**WPF:** `DockerPortLeadingIcon` data template; `DataTrigger` on `IsDockerPublished` swaps dot for tile.

---

## Implementation Prompt (for agents / future UI work)

Copy and adapt when changing PortCheck tray UI:

```text
Redesign PortCheck tray popup using Liquid Glass + Mail-style icon tabs.

Material:
- Floating glass bubble, captured backdrop blur, rim highlight, no nested glass-on-glass.
- Search: inset capsule, 30px height, muted placeholder until focus.

Pane tabs (only when Docker surface is visible):
- Horizontal ScrollViewer, icon-first chips (iOS 18 Mail Smart Categories).
- IDLE: 32px glass circle, icon only.
- ACTIVE: frosted pill #72FFFFFF + label; Docker accent on whale icon only (not solid blue pill).
- Spring pop on tab tap; list cross-fade 220ms on pane change.
- No "Docker unavailable" copy when Engine off — hide tab row entirely.

Lists (compact):
- Row min-height 26px local / 28px docker; list item padding 8,3 margin 2,1.
- Local row: leading 20px slot — green dot OR docker tile (not both, no text badge).
- Docker row: port detail two lines (host :port + address; mapping · container · compose), listen badge, Kill = docker stop.

Actions:
- Kill All only in Local section above footer; footer = Refresh + Hide only.
- Row hover: glass illumination, reveal Kill chip.

Do not spawn docker.exe; passive pipe only.
```

---

## File map

| Asset | Path |
| --- | --- |
| Tab styles | `src/PortCheck/Themes/LiquidGlass.xaml` → `GlassPaneTabLocal`, `GlassPaneTabDocker` |
| Tab layout | `src/PortCheck/TrayPopupWindow.xaml` → `StackPanel` row 1 |
| Docker row icon | `DockerPortLeadingIcon`, `DockerIconGeometry.Logo` (from `docker-icon-seeklogo.svg`) |
| Product contract | `docs/spec/portcheck.md` |
