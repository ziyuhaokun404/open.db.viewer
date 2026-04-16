# Open DB Viewer SQLite UI Refresh Design

## Summary

This design resets the short-term product target for Open DB Viewer from "lightweight multi-database browser" to "well-crafted SQLite desktop viewer".

The immediate problem is not feature absence. The current renderer already covers home, connection, object tree, schema, table data, query, export, and theme plumbing. The current problem is that the UI still feels rough: visual hierarchy is noisy, spacing and surfaces are inconsistent, some dialog and shell details feel improvised, and the product promise is diluted by unfinished MySQL / PostgreSQL entry points.

This phase will improve product quality by doing two things together:

- narrow the active product scope to SQLite only
- redesign the renderer shell and core screens so the app feels deliberate, polished, and easier to use

## Product Decision

The short-term product goal is now:

"Build a polished SQLite database browsing tool with a clean Fluent-inspired desktop experience."

This replaces the previous short-term emphasis on three database types. SQLite becomes the only actively supported workflow in the product UI, copy, and validation logic for this phase.

## Goals

- Make the application feel visually coherent and intentionally designed
- Remove low-quality UI details that currently make the product feel unfinished
- Shorten the first-run path so a user can open a SQLite file and start browsing quickly
- Align product messaging, form behavior, and page structure with the new SQLite-only scope
- Preserve the current functional browsing workflow: open connection, browse objects, inspect schema, inspect data, run query, export CSV

## Non-Goals

- No re-expansion to MySQL or PostgreSQL in this phase
- No introduction of heavy IDE-style features such as tabs, query history, or visual builders
- No migration to a third-party UI component library
- No major rewrite of main-process database services unless required by SQLite-only simplification
- No attempt to make the product look like a generic marketing dashboard

## Problem Statement

The current product has three linked issues.

### 1. Visual quality is below the product ambition

Observed issues from the current renderer structure and styles:

- gradients and decorative accents are overused in shell, top bar, cards, and dialog surfaces
- spacing density is inconsistent across shell, panels, headers, actions, and modal surfaces
- home page content reads like a prototype dashboard instead of a focused database tool
- dialog presentation is still visually noisy, especially around overlay, head decoration, and mixed surface treatments
- controls do not yet share a crisp, repeatable sizing and hierarchy system

### 2. Product scope is visually and behaviorally inconsistent

The UI still presents SQLite, MySQL, and PostgreSQL as equal first-class options, but only SQLite has a production-grade path. This creates a trust problem:

- the home page implies wider readiness than the product actually has
- the connection form shows fields and types that distract from the primary SQLite job
- users can spend time in workflows that are not the true short-term product focus

### 3. The interaction model is broader than necessary for the current product stage

For a SQLite-first tool, the highest-value path is:

1. choose or reopen a SQLite file
2. browse tables
3. inspect schema or data
4. run a quick query

The current home page and connection entry experience add conceptual overhead around generalized "connection management" before the product has earned it.

## Experience Principles

### Focused

The product should behave like a dedicated SQLite viewer, not a partial general database manager.

### Calm

The UI should feel restrained and desktop-native. Surfaces stay neutral. Accent is used sparingly. Decorative treatments must never compete with data.

### Dense Enough, Not Cramped

The interface should support work, not presentation. Spacing and control sizing should be tighter and more regular than the current layout while still remaining readable.

### Data First

The shell should frame the database content, not dominate it. Tables, schema, and query results should remain the visual priority once a database is open.

## Scope Changes

### Active Scope

- SQLite file selection
- saved SQLite file entries
- object tree browsing
- schema inspection
- table data browsing
- query execution
- CSV export

### Deferred from Active UI Scope

- MySQL connection creation
- PostgreSQL connection creation
- cross-database messaging implying equal readiness

### Copy Scope

Product copy should no longer describe the current release as a three-database browser. It should describe the product as a SQLite viewer, with future database support mentioned only as later roadmap context when necessary.

## Information Architecture

### 1. Home

Purpose:

- reopen saved SQLite files
- open a new SQLite file
- explain the product in one sentence

The home page should no longer behave like a generic dashboard. It should become a focused launch surface with:

- one primary action: open SQLite file
- one secondary area: recent / saved SQLite files
- one compact supporting explanation of what the app does

### 2. Browser Workspace

Purpose:

- browse database objects in the sidebar
- inspect the selected table in the main area

The browser page remains the main working area, but with cleaner shell hierarchy:

- left: database object tree
- top of main: object context header
- below header: structure / data / query switch
- main content: one active work surface
- bottom footer: compact status only

## Screen Design Direction

### Home Page

Replace the current "workspace / connection overview" composition with:

- a restrained hero block that says what the app does
- a primary file-open action
- a compact list of saved SQLite entries
- optional empty-state guidance if there are no saved entries

Remove:

- database-type summary cards
- dashboard-like metrics that add no operational value
- over-decorated sectional framing

### Connection Dialog

The dialog should feel like a system tool surface, not a poster card.

Required changes:

- flat neutral overlay, no tinted gradient impression
- simpler header without accent flourish line
- SQLite-first copy
- only the fields required for SQLite in the visible primary flow
- clearer button hierarchy and spacing

The dialog should support folder-oriented file picking without implying filename suffix restrictions in the UI copy.

### Browser Workspace

Required changes:

- reduce ornamental shells around already-contained content
- tighten vertical rhythm between page header, tab switcher, and content surface
- make tab state clearer and less pill-heavy
- improve sidebar scanability for object tree and connection context
- ensure tables remain the dominant visual layer

## Visual System Direction

This phase builds on the existing Fluent-neutral groundwork, but simplifies it further.

### Palette

- neutral black/white/gray base
- Windows accent color only for active / focused / selected states
- no decorative accent gradients on panels, overlays, or shell chrome

### Surfaces

- app background: quiet neutral field
- shell panels: consistent neutral surfaces with restrained border and shadow
- dialogs: opaque or near-opaque neutral surface, visually separated by shadow and scrim, not gradient
- cards: only where grouping is necessary, not as a default wrapper everywhere

### Shape and Typography

- regularize radius usage into a smaller set
- reduce over-rounded areas that currently make the interface feel soft and toy-like
- strengthen hierarchy through size, weight, and spacing rather than decorative framing
- favor concise, work-oriented wording

## Component-Level Requirements

### App Shell

- consistent shell padding and gutters
- clearer separation between shell chrome and work surfaces
- less visual competition from top bar and outer panel styling

### Sidebar

- better density for saved connection list and object tree
- cleaner selected / hover states
- more disciplined heading and hint presentation

### Buttons

- unify heights, padding, border radius, and hover treatment
- primary buttons use accent; secondary buttons stay neutral
- tertiary utility actions should not look equal in weight to primary actions

### Tabs and Data Surfaces

- active tab state must be obvious
- inactive tab states should be quieter
- schema table, data grid, and query results should share one work-surface language
- header bars and meta chips should be quieter than the content itself

### Status and Empty States

- keep success / error / empty panels compact and informative
- reduce oversized empty-state treatment on working screens
- make guidance specific to SQLite tasks

## Functional Changes Required by SQLite-Only Scope

### Connection Creation Flow

The primary creation flow becomes "Open SQLite file".

Behavior changes:

- SQLite is the only available connection type in the active UI
- MySQL and PostgreSQL options are removed from the segmented control for this phase
- the file path field becomes the main input
- name suggestion continues to derive from the selected path

### Stored Connection Messaging

Saved entries should read as saved SQLite databases, not generic server connections.

### Product Copy

Update renderer copy and top-level docs so they consistently describe:

- SQLite support as current
- server databases as future scope, if mentioned at all

## Architecture Impact

The implementation should remain incremental and use the existing renderer structure.

Main change area:

- `src/renderer/app/app-layout.css`
- selected page components and shared controls
- supporting copy in `home-page.tsx`, `connection-form.tsx`, and `browser-page.tsx`

Existing connection store and SQLite browsing flow stay in place. The goal is to simplify the renderer-facing experience rather than redesign the storage model.

## Testing Strategy

### Renderer Behavior Tests

- home page reflects SQLite-only messaging
- connection form shows SQLite-only primary flow
- browser workspace still renders structure / data / query panels correctly

### Layout and Style Contract Tests

- overlay uses neutral scrim token rather than gradient treatment
- home page no longer renders obsolete multi-database summary content
- shell spacing and key surface classes remain present
- primary / secondary button hierarchy remains distinguishable

### Documentation Checks

- README and active product copy stop claiming current MySQL / PostgreSQL availability

## Risks

### Risk: Scope messaging changes without enough UI simplification

If copy says "SQLite only" but the UI still looks like a generic multi-database tool, the product will still feel unfocused.

Mitigation:

- change both content and layout in the same phase

### Risk: Over-polishing without improving usability

A cleaner visual style is not enough if scanability and flow remain awkward.

Mitigation:

- prioritize hierarchy, density, and focus over decorative redesign

### Risk: Existing tests lock in undesirable UI details

Some current tests may encode layout assumptions that belong to the old design.

Mitigation:

- update tests to protect the new product direction, not outdated styling choices

## Acceptance Criteria

- The app clearly presents itself as a SQLite viewer in current product copy
- The home page becomes a focused launch surface instead of a pseudo-dashboard
- The connection dialog is visually calmer and SQLite-first
- The browser workspace feels denser, cleaner, and more work-oriented
- Accent usage is limited to emphasis states, not decorative surface treatment
- Dialog overlay is neutral and non-gradient
- Existing SQLite browsing workflow remains intact after the redesign
