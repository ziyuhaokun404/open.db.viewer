# Open DB Viewer Fluent System Theme Design

## Summary

This design updates the renderer theme system to use a Fluent Neutral visual direction with:

- black-and-white neutral surfaces as the base palette
- Windows system accent color as the only primary accent
- automatic light/dark mode following the OS
- automatic accent updates when the Windows accent color changes

The goal is to change the visual system without restructuring the existing pages or replacing the current component architecture.

## Goals

- Keep the current application structure and workflow intact
- Shift the visual language to Fluent Neutral instead of the current cool gray-blue bias
- Follow the OS light/dark mode automatically
- Read the current Windows accent color and update the UI when it changes
- Restrict accent usage to primary actions, selected states, focus states, links, and active controls
- Centralize theme behavior so components consume semantic tokens instead of hardcoded colors

## Non-Goals

- No page layout redesign
- No settings UI for manual theme overrides in this phase
- No migration to a third-party component library
- No full redesign of all controls to match WinUI component internals
- No broad non-Windows accent integration beyond a sane fallback color

## Visual Direction

The approved visual direction is `A. Standard Fluent Neutral`.

Visual rules:

- Base surfaces are neutral white, gray, and near-black, not blue-tinted
- Light theme should feel like a restrained Windows desktop workbench
- Dark theme should use layered dark grays instead of pure black
- System accent color should appear only in focused and active UI states
- Panels must not become large tinted accent blocks

## Architecture

The theme system will be split into four layers:

1. Main-process theme source
2. Preload bridge
3. Renderer theme sync
4. CSS token consumption

### 1. Main-Process Theme Source

A new main-process theme service will be introduced. It will:

- read the current effective dark/light state from Electron `nativeTheme`
- read the current system accent color from Electron `systemPreferences`
- normalize Electron color output into renderer-ready values
- subscribe to `nativeTheme.updated`
- subscribe to `systemPreferences` accent color changes on Windows
- broadcast theme updates to renderer windows
- update `BrowserWindow` title bar overlay colors to stay legible in both themes

This service is the only place that knows about Electron theme APIs.

### 2. Preload Bridge

The preload layer will expose a narrow renderer-safe theme API:

- `getThemeSnapshot()`
- `onThemeChange(listener)`

The API will expose only normalized theme data. Renderer code will not access Electron theme APIs directly.

Implementation detail:

- `ipcMain.handle("theme:get-snapshot")` returns the current snapshot
- main broadcasts `webContents.send("theme:changed", snapshot)` on theme changes
- preload wraps both channels behind the theme API above

### 3. Renderer Theme Sync

A lightweight renderer-side sync layer will:

- fetch the initial theme snapshot on startup
- subscribe to runtime theme updates
- write the effective theme mode to the root element, for example `data-theme="light"` or `data-theme="dark"`
- write accent-derived CSS custom properties to the root element
- set `color-scheme` consistently with the active theme

This layer will not own any business state.

### 4. CSS Token Consumption

The styling system will move to semantic tokens. Existing renderer styles will consume tokens instead of hardcoded palette values.

The primary files in scope are:

- `src/renderer/styles/design-tokens.css`
- `src/renderer/styles/base.css`
- `src/renderer/app/app-layout.css`

## Theme Model

The renderer theme payload should be small and explicit.

Proposed shape:

```ts
interface SystemThemeSnapshot {
  mode: "light" | "dark";
  accent: string;
  accentHover: string;
  accentPressed: string;
  accentSoft: string;
  accentFocusRing: string;
  titleBarColor: string;
  titleBarSymbolColor: string;
}
```

Notes:

- `mode` reflects the effective OS-following result
- Accent derivatives are computed in main so the renderer remains simple
- Title bar colors are included because the native title bar overlay must track the same theme

## Token Strategy

Tokens will be organized into four groups.

### Neutral Foundation Tokens

Examples:

- `--color-bg-canvas`
- `--color-bg-shell`
- `--color-surface-1`
- `--color-surface-2`
- `--color-surface-3`
- `--color-stroke-subtle`
- `--color-stroke-strong`
- `--color-text-primary`
- `--color-text-secondary`
- `--color-text-tertiary`

These tokens differ by light and dark mode and define the Fluent Neutral baseline.

### Accent Tokens

Examples:

- `--accent`
- `--accent-hover`
- `--accent-pressed`
- `--accent-soft`
- `--accent-focus-ring`

These are set at runtime from the system accent color.

### Status Tokens

Existing success and error tokens remain, but will be normalized so they work in both light and dark modes without clashing with the neutral palette.

### Material Tokens

Current `mica` and `acrylic` semantics remain, but their tint is changed from cool blue-gray toward neutral white/gray layering.

## Styling Rules

The renderer styling update will follow these rules:

- Remove hardcoded blue ambient gradients from the page background
- Replace fixed blue focus styles with accent-derived tokens
- Use accent only for:
  - primary buttons
  - selected navigation and tree items
  - active tabs
  - active segmented-control options
  - links
  - focus rings
- Keep panels, cards, and work surfaces neutral
- Ensure dark theme surfaces are layered and readable without looking like OLED pure black

## File Scope

Expected files to change:

- `src/main/index.ts`
- `src/main/preload.ts`
- `src/renderer/types/global.d.ts`
- `src/renderer/main.tsx` or `src/renderer/app/App.tsx`
- `src/renderer/styles/design-tokens.css`
- `src/renderer/styles/base.css`
- `src/renderer/app/app-layout.css`

Expected files to add:

- `src/main/services/system-theme-service.ts`
- `src/main/ipc/theme-ipc.ts`
- `src/renderer/app/theme-sync.tsx`

## Data Flow

Startup flow:

1. Main process creates the theme service
2. Preload exposes the theme API
3. Renderer requests the initial snapshot
4. Renderer writes root theme mode and CSS variables
5. Styles resolve through semantic tokens

Runtime flow:

1. OS theme or accent changes
2. Electron emits a native theme or accent event
3. Main theme service computes a fresh snapshot
4. Renderer receives the updated snapshot
5. Root attributes and CSS variables are updated without a reload

## Error Handling and Fallbacks

- On non-Windows platforms, accent color falls back to a fixed Fluent blue
- If accent color retrieval fails, use the same Fluent blue fallback
- If a theme update event fires before a window is ready, the next renderer snapshot pull still returns the latest state
- If the renderer listener is absent, the current theme remains stable until next load

## Testing Strategy

The implementation will use test-first changes for each behavior slice.

### Main-Process Tests

- theme snapshot generation for light mode
- theme snapshot generation for dark mode
- accent color normalization
- fallback accent behavior when system accent is unavailable

### Renderer Tests

- initial theme snapshot is applied to the document root
- runtime theme updates rewrite root attributes and CSS variables

### Style Regression Tests

Extend existing Fluent-related tests to verify:

- neutral token families exist for both light and dark modes
- old hardcoded accent-blue values are no longer the styling source of truth
- base styles consume semantic accent variables
- background styling no longer depends on the old blue poster-like treatment

### Integration-Sensitive Coverage

- title bar overlay colors update coherently with light/dark changes

## Risks

### Risk: Title Bar Drift

The native title bar overlay is configured in the main process. If renderer theme updates and title bar colors do not move together, the shell will look broken.

Mitigation:

- compute title bar colors in the same theme service
- update the browser window overlay when theme changes

### Risk: Accent Overuse

If accent variables are applied too broadly, the UI will drift away from the approved Fluent Neutral direction.

Mitigation:

- limit accent consumption to explicit interactive states
- keep surfaces neutral by design

### Risk: Dark Theme Contrast Errors

A neutral dark theme can become muddy if stroke and surface separation are weak.

Mitigation:

- use layered dark grays with clear stroke hierarchy
- verify common shells, dialogs, and tables in tests

## Implementation Constraints

- Do not restructure page layouts
- Do not add a user-facing theme settings panel in this phase
- Do not convert the app into a highly colored Fluent variant
- Do not couple business stores to theme synchronization

## Acceptance Criteria

- The app follows OS light/dark mode automatically
- On Windows, the app reads the current system accent color
- On Windows, changing the system accent color updates the app without restart
- Accent usage remains limited to interactive emphasis
- Light and dark themes both preserve the approved Fluent Neutral character
- Title bar overlay remains visually aligned with the active theme
- Existing app layout and workflow remain unchanged
