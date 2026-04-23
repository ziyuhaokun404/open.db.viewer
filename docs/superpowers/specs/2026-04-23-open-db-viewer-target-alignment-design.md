# open.db.viewer Target Alignment Design

Date: 2026-04-23
Project: `open.db.viewer`
Scope: shell chrome, home landing page, target-mock visual alignment, missing interaction completion

## Goal

Align the current `open.db.viewer` shell and landing page with the supplied target mock while preserving the existing WPF shell architecture and data flow.

This pass includes both:

- visual alignment to the target screenshot
- interaction completion for UI elements that are visually implied by the mock

The result should look and behave like the target design without expanding the scope into a full application redesign.

## Non-Goals

This design does not include:

- workspace feature expansion
- database editing workflows
- new persistence formats
- advanced context menus beyond what the target mock needs
- a full design-system extraction for every page in the app

This is a targeted shell and home-page refinement pass.

## Source of Truth

The desired behavior is defined by comparing:

- current app screenshot provided by the user
- target screenshot provided by the user
- existing code in `src/Open.Db.Viewer.Shell`

Code-level scope is concentrated in:

- `Views/MainWindow.xaml`
- `Views/MainWindow.xaml.cs`
- `Views/Navigation/HomeLandingPage.xaml`
- `ViewModels/ShellViewModel.cs`
- `ViewModels/Navigation/HomeLandingViewModel.cs`

The existing `DatabaseEntry` model already exposes the data needed by the target UI:

- `Name`
- `FilePath`
- `LastOpenedAt`
- `IsPinned`

No domain-model expansion is required for this pass.

## Current vs Target: Structural Differences

### 1. Title Bar

Current:

- theme button is a large circular icon button
- title bar reads heavier than the target
- spacing near the right window controls is too loose

Target:

- theme toggle is a compact pill-shaped control
- overall chrome is flatter and quieter
- the app brand occupies less vertical emphasis

Required change:

- replace the current theme action presentation with a compact capsule-style toggle host
- reduce title-bar visual weight without changing existing WPF title bar ownership

### 2. Left Navigation Rail

Current:

- navigation panel is visually close but still oversized
- there is no collapse/expand affordance at the top
- active item styling is not tight enough to the mock
- section spacing and lower utility actions feel too separated

Target:

- rail includes a top collapse button
- active item uses a stronger left indicator and lighter selected background
- rail uses tighter vertical spacing and cleaner alignment

Required change:

- add a rail-collapse state
- add a top rail toggle button
- tune selected, hover, and resting states to match the mock
- re-balance rail spacing and heights

### 3. Content Host Container

Current:

- outer content frame has too much margin and too much visible container presence
- content area looks more padded and more detached than in the target

Target:

- content area feels broader and more integrated with the shell
- main cards inside the content area carry the hierarchy instead of the outer frame

Required change:

- reduce outer spacing and container weight
- shift hierarchy toward interior cards

## Current vs Target: Home Page Differences

### 1. Hero Card

Current:

- hero text is close in wording but spacing and scale do not match
- illustration composition is similar but not proportional
- decorative layers are too heavy and the database cylinder is oversized
- current version removed secondary helper text and path-level detail that the target relies on elsewhere

Target:

- hero is balanced: large title on the left, controlled illustration on the right
- action buttons sit closer to the title block
- illustration reads as a light layered composition, not a dominant graphic

Required change:

- recompose the illustration layers, orb sizes, and database-cylinder drawing
- tighten hero spacing
- match target button sizing and placement

### 2. Quick Open Section

Current:

- quick-open entries show only the database name
- path subtitle is missing
- action button copy and geometry do not fully match the target
- secondary quick-open card is too sparse

Target:

- main quick-open card shows icon, database name, path, and a direct open action
- secondary card shows the same database preview with a more-options affordance

Required change:

- restore `FilePath` as subtitle
- restore list metadata layout
- align button label, sizing, and spacing with the mock

### 3. Pinned and Recent Sections

Current:

- cards only show the name
- supporting file path text is missing
- recent items do not expose last-opened information in the card body
- header copy and spacing are simplified compared with the target

Target:

- each item shows primary label and file path
- recent items may also show recency metadata if it fits the layout
- each section header is compact and consistent

Required change:

- reintroduce path subtitles
- preserve concise metadata presentation
- tune item card height and spacing to the target density

## Interaction Requirements

This pass must include the interaction implied by the target mock, not just static visuals.

### Rail Collapse

Add a collapsible left rail with two states:

- expanded: current default behavior with labels visible
- collapsed: icon-first rail with reduced width

First implementation may keep collapse state in memory only. Persistence is optional and out of scope.

### Theme Toggle

Keep the existing theme-switching behavior, but restyle the control to match the mock. The logic can remain in `MainWindow.xaml.cs`.

### Section Actions

The following interactions must remain or become explicit:

- `打开数据库` opens the file picker
- `快速打开` opens the most recent database
- `查看全部` navigates to `最近使用` or `已固定`
- home-card items remain clickable targets for opening or managing entries

### Item Action Area

The target mock visually reserves a right-side action slot using pin and more icons.

Required behavior:

- keep pin/unpin actions where already supported
- expose a visible more-actions affordance even if the first pass only uses a simple menu host or a minimal `ContextMenu`

Do not leave dead-looking icons with no click target.

## Data and ViewModel Impact

### No Domain Changes

`DatabaseEntry` already supports the UI. No changes are needed in:

- `Open.Db.Viewer.Domain`
- `Open.Db.Viewer.Application`
- SQLite infrastructure

### Shell ViewModel Changes

`ShellViewModel` needs a small UI-state extension for:

- `IsNavigationCollapsed`
- collapse toggle command

This is presentation state only.

### Home Landing ViewModel Changes

`HomeLandingViewModel` already exposes:

- `QuickOpenEntry`
- `PinnedSummary`
- `RecentSummary`
- `OpenDatabaseCommand`
- `OpenQuickOpenCommand`

This is sufficient. Any additions should remain presentation-focused and minimal.

## Visual System Adjustments

This pass should unify a small set of shell-level visual tokens used by both `MainWindow` and `HomeLandingPage`.

### Tokens to Normalize

- shell background
- rail background
- content background
- card background
- border color
- selected background
- accent color
- muted text color
- corner radii
- major spacing increments
- card shadow strength

The goal is not to build a full theme system. The goal is to stop tuning visual values ad hoc in each XAML block.

### Recommended Strategy

Keep the token set local to shell resources unless duplication grows further.

That means:

- shared shell resources stay in `MainWindow.xaml` or a small local resource dictionary
- page-specific decorative resources stay in `HomeLandingPage.xaml`

Do not over-abstract this pass.

## Implementation Strategy

### Option A: Patch Existing XAML In Place

Pros:

- fastest
- minimal file churn

Cons:

- current XAML is already visually dense
- continued in-place tuning will become hard to reason about

### Option B: Recompose Existing Shell and Home Page While Keeping ViewModels

Pros:

- best fit for this task
- concentrates changes in the view layer
- avoids unnecessary business-layer churn
- supports both visual and interaction alignment

Cons:

- moderate XAML rewrite required

### Option C: Full Shared-Component Refactor

Pros:

- cleaner long-term UI architecture

Cons:

- too large for the current goal
- increases risk and review surface

## Recommendation

Use Option B.

Recompose `MainWindow.xaml` and `HomeLandingPage.xaml` around the target mock, keep existing data flow, and add only the minimal presentation state required for collapse and action affordances.

## File-Level Design

### `Views/MainWindow.xaml`

Owns:

- title bar
- theme toggle host
- navigation rail layout
- collapse button
- selected nav-item visuals
- outer content stage container

Planned changes:

- reduce title bar visual weight
- add compact theme capsule
- add rail collapse button
- add triggers for expanded vs collapsed rail
- tighten rail spacing and selected state
- reduce outer content container heaviness

### `Views/MainWindow.xaml.cs`

Owns:

- theme toggle visual behavior

Planned changes:

- update theme-toggle visual state handling if the new control requires different icon placement or tooltip messaging

### `ViewModels/ShellViewModel.cs`

Owns:

- section navigation state
- shell presentation state for rail collapse

Planned changes:

- add `IsNavigationCollapsed`
- add collapse toggle command
- keep existing navigation and page-loading behavior intact

### `Views/Navigation/HomeLandingPage.xaml`

Owns:

- hero card
- quick-open section
- pinned summary section
- recent summary section

Planned changes:

- rebuild hero proportions
- restore subtitle/path rendering in cards
- align button and card geometry to target
- restore header and item metadata hierarchy
- make right-side action affordances explicit and clickable

### `ViewModels/Navigation/HomeLandingViewModel.cs`

Owns:

- landing-page data projection

Planned changes:

- keep changes minimal
- only add derived properties if layout clarity genuinely needs them

## Verification Criteria

This design is successful when:

- the shell visually matches the target at a glance
- the title bar and left rail no longer feel oversized
- the home hero reads closer to the target composition
- quick-open, pinned, and recent cards all show the expected metadata
- the left rail can collapse and expand without breaking navigation
- theme switching still works
- opening a database from the home page still routes into the workspace correctly
- recent and pinned section links still navigate correctly

## Risks and Mitigations

### Risk: XAML styling complexity grows quickly

Mitigation:

- keep changes concentrated in `MainWindow.xaml` and `HomeLandingPage.xaml`
- only extract shared tokens, not shared component frameworks

### Risk: Visual alignment changes accidentally regress navigation behavior

Mitigation:

- avoid changing existing section-routing contracts
- confine new behavior to presentation state and explicit commands

### Risk: Collapsed rail harms layout stability

Mitigation:

- treat collapsed state as a shell-width variation only
- do not change content routing or page template selection

### Risk: Decorative illustration becomes harder to maintain

Mitigation:

- keep hero illustration as simple WPF primitives
- prefer fewer layered shapes with better sizing over more shapes

## Delivery Boundary

The first implementation delivery should include:

- aligned title bar and theme toggle styling
- collapsible navigation rail
- aligned content-stage spacing
- rebuilt home hero
- restored path and metadata rendering in cards
- visible and clickable item action affordances

The first delivery should not include:

- workspace visual redesign beyond necessary shell integration compatibility
- new data-management features
- global resource-system refactor across all pages

## Final Recommendation

Treat this as a focused UI-alignment pass over the existing unified shell, not as a new product architecture project.

Preserve the current shell/page structure, tighten shell chrome, rebuild the landing page composition, and add the minimum interaction state needed to make the mock fully believable in the running application.
