# Open DB Viewer UI/UX Refresh Design

## 1. Document Info

- Product: Open DB Viewer
- Doc type: UI/UX refresh design
- Target: WPF SQLite MVP usability and presentation uplift
- Date: 2026-04-21
- Scope: `Open.Db.Viewer.Shell`

## 2. Goal

Refresh the current WPF shell so it feels like a polished desktop product instead of a functional prototype, while preserving the existing SQLite-first MVP workflow.

This round should improve both visual quality and day-to-day usability, with emphasis on:

1. clearer page hierarchy
2. better first-run and return-user experience
3. stronger contextual cues inside the workspace
4. more explicit empty, loading, success, and error states
5. low-risk structural improvements without rewriting core business logic

## 3. Constraints

- Keep the existing MVVM structure and core SQLite workflow intact.
- Do not introduce new product scope such as multi-database tabs, query history, or advanced IDE features.
- Allow moderate page-structure refactors and small interaction upgrades where they materially improve UX.
- Prefer incremental upgrades to existing XAML and ViewModel files over large architectural churn.

## 4. Design Direction

### 4.1 Product Feel

The UI should feel like a modern desktop productivity tool:

- soft, light, Fluent-compatible surfaces
- stronger layout hierarchy than the current default-control composition
- clear separation between navigation, context, content, and status
- warm, modern, card-based presentation without becoming decorative

### 4.2 Tone

The product should feel:

- approachable for light SQLite users
- clear enough for repeated technical use
- calm and focused rather than dense or flashy

### 4.3 Visual Principles

- Use layered surfaces instead of large flat white areas.
- Group related controls into panels with consistent corner radius, padding, and border strength.
- Use large title text only where it communicates page purpose.
- Reduce visual competition from long file paths and secondary metadata.
- Keep the main action on each page visually obvious.

## 5. Information Architecture

## 5.1 Home Page

The home page should become a desktop-style landing workspace rather than a simple list screen.

### Structure

1. Welcome hero
2. Global search/filter
3. Pinned databases section
4. Recent databases section
5. Contextual empty or no-results state

### Interaction Goals

- A first-time user should immediately understand that the primary action is opening a local SQLite database.
- A returning user should quickly reopen a recent or pinned database.
- Search should feel like a top-level helper for finding known entries, not a hidden secondary field.

### Content Rules

- Database name is primary.
- Path and last-opened metadata are secondary.
- Pin/unpin is a lightweight side action.
- Empty sections must render explanatory content rather than blank space.

## 5.2 Workspace

The workspace should keep the current left-navigation-plus-right-content model, but with stronger layout hierarchy.

### Structure

1. Workspace context bar
2. Left object-navigation panel
3. Right content area
4. Tab-local toolbars
5. In-place state and result messaging

### Workspace Context Bar

The top workspace band should show:

- current database title
- database path as secondary metadata
- return-to-home action
- refresh action
- lightweight status feedback area

This bar should make it obvious where the user is and what the current working context is.

### Object Navigation Panel

The object tree should become a clear navigation panel with:

- its own visual container
- a title and optional small summary
- better row spacing and hierarchy contrast
- internal empty or error state rendering when objects are unavailable

### Right Content Area

The right side should be broken into:

1. current object context
2. tab navigation
3. active tab content

This makes structure, data, and query feel like coordinated modes within one workspace instead of unrelated controls on the same page.

## 6. Page-Level UX Changes

## 6.1 Home Page

### Welcome Area

Create a stronger top section with:

- page title
- short supporting description
- primary `Open database` action

The supporting text should explain that the app opens local SQLite files for browsing, querying, and exporting.

### Search

Treat search as a global filter over pinned and recent entries.

Behavior:

- filter both sections at once
- show a dedicated no-results state when the filter matches nothing
- keep the input visually integrated with the content area

### Database Entry Cards

Each entry should display:

- name
- path
- last opened time when applicable
- pin or unpin affordance

Behavior:

- clicking the main card area opens the database
- pin or unpin remains a secondary icon action
- hover and focus states should feel interactive and desktop-native

### Home Empty States

Support at least:

- first run, no entries yet
- no pinned entries
- no search results

Each state should tell the user what to do next.

## 6.2 Workspace Shell

### Context Bar

Add a dedicated bar above the main workspace panels for:

- current database identity
- navigation back to home
- refresh
- lightweight status text or indicator

This is the right location for cross-tab feedback such as refresh completion or open-state messaging.

### No Selection State

When no table is selected, the right content area should show an explicit instructional empty state such as selecting a table from the object list.

Avoid showing a blank schema grid or empty data grid with no explanation.

## 6.3 Schema Tab

The schema tab should feel like a table-definition overview.

Add:

- a local header showing table name
- a small summary such as field count
- clearer table-header hierarchy

The goal is quick comprehension, not high-density detail.

## 6.4 Data Tab

The data tab should feel like a result browser.

### Toolbar

Unify the current controls into one coherent toolbar containing:

- previous page
- next page
- page size
- current page indicator
- row count summary
- sort context if available
- refresh
- export current page

### Data Grid Container

Place the grid inside a main result panel so it is visually obvious that this is the active work area.

### States

Support:

- loading data
- empty result
- normal result
- recoverable error message area

## 6.5 Query Tab

The query tab should feel like a lightweight SQL workbench.

### Query Toolbar

Keep:

- execute
- export CSV
- table-based template buttons

Improve layout so the toolbar, current-table hint, editor, and result status feel like one grouped workflow.

### Query Editor Area

Make the SQL input feel like an editor panel distinct from the result panel.

### Query Result Area

Use the same visual language as the data tab so both pages feel like related result-browsing experiences.

### States

Support:

- ready state
- executing state
- no result
- result available
- query failure

## 7. State Feedback Strategy

The UI should standardize around four state categories:

1. empty
2. in progress
3. success
4. failure

### Presentation Rules

- Prefer inline or page-local state presentation over modal interruption.
- Use the workspace context bar for lightweight global feedback.
- Use panel-local messaging for table, query, and object-tree issues.
- Keep copy short and actionable.

### Examples

- Empty: no database entries, no selected table, no query results
- In progress: loading objects, paging data, executing SQL, exporting CSV
- Success: database opened, refresh finished, export completed
- Failure: invalid database file, query failure, export failure

## 8. Interaction Enhancements Included In Scope

This refresh may add small interaction improvements where they directly support usability:

- return-to-home action from workspace
- refresh action in workspace
- clearer state containers
- data-tab export entry point
- reorganized toolbar layouts
- stronger empty-state guidance

These are considered part of the UI/UX refresh and do not count as new product scope.

## 9. Explicitly Out Of Scope

This round should not implement:

- multi-tab database workspaces
- query history
- saved SQL
- multi-database support
- deep business-layer rewrites
- custom control-library extraction
- advanced editor features

## 10. Implementation Surface

Primary files expected to change:

- `src/Open.Db.Viewer.Shell/Views/MainWindow.xaml`
- `src/Open.Db.Viewer.Shell/Views/MainWindow.xaml.cs`
- `src/Open.Db.Viewer.Shell/Views/Pages/HomePage.xaml`
- `src/Open.Db.Viewer.Shell/Views/Pages/HomePage.xaml.cs`
- `src/Open.Db.Viewer.Shell/Views/Pages/DatabaseWorkspacePage.xaml`
- `src/Open.Db.Viewer.Shell/Views/Pages/DatabaseWorkspacePage.xaml.cs`
- `src/Open.Db.Viewer.Shell/ViewModels/HomeViewModel.cs`
- `src/Open.Db.Viewer.Shell/ViewModels/DatabaseWorkspaceViewModel.cs`
- `src/Open.Db.Viewer.Shell/ViewModels/DataViewModel.cs`
- `src/Open.Db.Viewer.Shell/ViewModels/QueryViewModel.cs`

Possible small additions are acceptable if they keep responsibilities clear, for example:

- a small state model for reusable view feedback
- focused helper properties on existing ViewModels

## 11. Success Criteria

This refresh succeeds when:

1. the app feels like a coherent desktop product instead of a raw functional prototype
2. the home page clearly supports both first-run and return-user workflows
3. the workspace hierarchy is visually obvious at a glance
4. users are less likely to get lost in no-selection, no-result, loading, and failure states
5. structure, data, and query pages feel like one coordinated tool
6. the refresh lands without destabilizing the existing SQLite MVP workflow

## 12. Risks And Mitigations

### Risk: visual churn without usability gain

Mitigation:

- prioritize hierarchy, status clarity, and task flow before decorative styling

### Risk: too much restructuring for one pass

Mitigation:

- keep current MVVM boundaries
- prefer page-level upgrades over architectural rewrites

### Risk: inconsistent styling between pages

Mitigation:

- define shared panel, spacing, and toolbar rules and apply them across home, data, and query areas

## 13. Final Recommendation

Implement a moderate UI/UX refresh focused on:

- desktop-product presentation
- stronger layout hierarchy
- better state handling
- small but meaningful workflow improvements

This approach gives the project a clear user-facing quality lift while staying compatible with the current WPF MVP codebase and schedule.
