# open.db.viewer Shell Redesign Design

Date: 2026-04-23
Project: `open.db.viewer`
Scope: application shell, home/navigation experience, workspace integration

## Goal

Redesign `open.db.viewer` from a two-page application (`Home` vs `Workspace`) into a single-shell desktop product with:

- a stable left navigation rail
- a modern launch-oriented home experience
- dedicated pages for recent and pinned databases
- a database workspace that lives inside the same shell instead of replacing it

This redesign should improve product coherence, information hierarchy, and future extensibility without requiring an immediate rewrite of the database inspection core.

## Non-Goals

This design does not require the first implementation phase to add:

- full multi-document database tabs
- row editing, deletion, or save workflows
- full IDE-style database tooling
- complete support for views, indexes, and triggers in the first UI pass
- backend or storage redesign beyond what the shell needs

Those are future workspace upgrades after the shell is stable.

## Current Problems

The current application has these structural issues:

- `MainWindow` only switches between `Home` and `Workspace`, so the product feels like two separate apps.
- `HomePage` mixes welcome content, search, pinned items, and recent items into one page with weak hierarchy.
- "Open database" is effectively a page-local action instead of a global product action.
- Recent and pinned database management do not have dedicated surfaces.
- The workspace is useful but visually detached from the home experience.
- The app cannot grow naturally into settings, about, richer workspace navigation, or additional database-object workflows.

## Product Direction

The target product form is a unified desktop shell:

- left rail for global navigation
- right content stage for the active section
- launch-oriented home page
- list-oriented management pages for recent and pinned databases
- workspace as a core section inside the same shell

The visual tone should be:

- light desktop productivity app
- calm blue accent color
- low-to-medium border contrast
- restrained shadows and gradients
- consistent spacing and rounded corners

The shell should feel modern, but the workspace may remain denser than the home page as long as both share the same visual language.

## Information Architecture

### Global Navigation

Primary sections:

- `首页`
- `最近使用`
- `已固定`
- `数据库工作台`

Secondary sections:

- `设置`
- `关于`

Global action:

- `打开数据库`

`打开数据库` is not a normal page. It is a global action available from the shell, and it may also appear as a call to action inside the home page.

### Section Responsibilities

#### 首页

Purpose:

- welcome the user
- provide the fastest path to open a database
- help the user continue recent work
- summarize pinned and recent collections without becoming a management page

Contains:

- hero section with title, description, primary and secondary actions
- quick open card for the most relevant recent item
- pinned summary card
- recent summary card
- lightweight tip/info area

Does not contain:

- full searchable collection management
- dense admin-like lists

#### 最近使用

Purpose:

- browse and manage the full recent-database collection

Contains:

- searchable list
- open action
- pin/unpin action
- sort or ordering support if needed

#### 已固定

Purpose:

- browse and manage the pinned-database collection

Contains:

- searchable list
- open action
- unpin action
- optional direct "add database to pinned" affordance

#### 数据库工作台

Purpose:

- host the active database browsing experience inside the unified shell

Contains:

- workspace header/context area
- object explorer
- object content area for data, schema, and SQL
- empty state when no database is open

#### 设置

First version can remain lightweight and include:

- theme preference
- default page size
- default behavior when opening databases
- export preferences if needed

#### 关于

Contains:

- app name
- version
- short product description
- technology and project metadata as needed

## Shell Layout

The main window becomes a persistent application shell with:

1. custom title bar
2. fixed left navigation rail
3. right content host

The left rail remains visible across all sections.

The right content host swaps section content while preserving shell identity.

### Left Navigation Rail

Top:

- app brand
- primary global action: `打开数据库`

Middle:

- primary navigation items

Bottom:

- `设置`
- `关于`

The rail should be visually quiet and narrow enough to avoid wasting space, because the app does not yet have many sections.

### Right Content Host

The active section fills the right stage.

Expected section density:

- `首页`: spacious, presentation-oriented
- `最近使用` / `已固定`: medium density, list-oriented
- `数据库工作台`: high density, tool-oriented
- `设置` / `关于`: low density

## Home Page Design

The home page is a launch surface, not a collection management page.

### Home Layout

Top block:

- hero card
- product title
- short SQLite-oriented description
- primary action: `打开数据库`
- secondary action: `打开最近`
- light illustration or geometric decorative panel

Middle block:

- `快速打开`
- shows the most relevant recent database, typically the most recently opened item

Lower block:

- two summary cards: `已固定的数据库` and `最近使用`
- each card shows a small subset of entries
- each card has `查看全部` to jump to its dedicated page

Bottom block:

- tip or info banner

### Home Search Decision

Search is removed from the main home stage.

Reason:

- home should emphasize launch and continuation
- search belongs to collection-management pages
- keeping search on the home hero weakens the intended hierarchy

Search moves to:

- `最近使用`
- `已固定`

## Recent and Pinned Pages

These pages replace the overloaded list management currently embedded in `HomePage`.

### Shared Behaviors

- searchable list
- consistent entry cards or rows
- open database
- pin/unpin
- empty state

### Differences

`最近使用` focuses on recency and quick return.

`已固定` focuses on a curated stable set of databases.

The two pages may share list components and data models, but they should remain separate navigation destinations because their user intent differs.

## Workspace Integration

The existing workspace remains functionally important, but it becomes a section inside the shell.

### First-Phase Workspace Behavior

- opening a database navigates the shell to `数据库工作台`
- the workspace renders inside the content host
- existing object explorer, schema, data, and query functionality are reused where possible
- if no database is open, the workspace shows a clear empty state instead of forcing a page switch back to home

### Workspace Visual Role

Compared with the home page, the workspace should be:

- denser
- more operational
- more tool-like

But it should still share:

- shell chrome
- accent color
- border language
- typography rules
- spacing system

### Future Workspace Expansion

Once the shell redesign is stable, future iterations may add:

- richer object grouping
- workspace-level toolbar
- object tabs
- row detail pane
- status bar
- more database-object categories

Those enhancements are intentionally excluded from the first redesign pass.

## View and ViewModel Architecture

Replace the current binary shell state with a section-driven shell.

### New Top-Level Concepts

- `AppShellViewModel`
- `CurrentSection`
- `NavigationItems`
- `CurrentContentViewModel`
- `CurrentDatabaseSession`

Recommended section enum:

- `Home`
- `Recent`
- `Pinned`
- `Workspace`
- `Settings`
- `About`

### Database Session

Introduce a session-level model representing the active workspace context:

- database path
- display name
- open state
- last opened metadata as needed
- active object context if needed

This keeps shell-level state coherent across home, recent, pinned, and workspace transitions.

## Page Structure

### Split Current Home Page

Current `HomePage` responsibilities should be separated into:

- `HomeLandingPage`
- `RecentDatabasesPage`
- `PinnedDatabasesPage`
- `SettingsPage`
- `AboutPage`

### Workspace Host

Current workspace page becomes a hosted shell section:

- `WorkspaceHostPage`

The existing internal workspace parts may initially remain largely intact, but they now live inside a stable shell contract.

## Existing ViewModel Reuse

These existing ViewModels should be retained and repositioned rather than discarded:

- `ObjectExplorerViewModel`
- `SchemaViewModel`
- `DataViewModel`
- `QueryViewModel`

Their current responsibilities still map well to the internal workspace, even if the top-level shell changes.

## File Organization Recommendation

Reorganize shell-layer code by responsibility:

- `Views/Shell/`
- `Views/Navigation/`
- `Views/Workspace/`
- `ViewModels/Shell/`
- `ViewModels/Navigation/`
- `ViewModels/Workspace/`

This avoids long-term accumulation under a generic `Views/Pages` bucket.

## Implementation Phases

### Phase 1: Build Unified Shell

- convert `MainWindow` into a real shell
- add left navigation rail and content host
- introduce shell navigation state
- wire sections: home, recent, pinned, workspace, settings, about
- make `打开数据库` a global action

### Phase 2: Redesign Home and Library Pages

- implement home hero
- add quick open card
- add pinned and recent summary cards
- create full recent page
- create full pinned page
- move search from home to the dedicated collection pages

### Phase 3: Move Workspace Into Shell

- route open-database flow into workspace section
- keep current workspace internals running inside the shell
- add workspace empty state when no database is open
- align visual language with shell

### Phase 4: Future Workspace Upgrade

Not part of the first redesign pass, but enabled by the new structure:

- richer database object model
- object tabs
- toolbars
- detail panes
- stronger database-client behaviors

## Minimum Viable Redesign

The recommended first delivery includes:

- unified shell
- left navigation
- redesigned home page
- separate recent and pinned pages
- global open-database action
- workspace integrated into the same shell

The first delivery should not include:

- full multi-document workspace
- row editing and save workflows
- heavy IDE-like workspace expansion

This boundary keeps scope realistic while still changing the product shape meaningfully.

## Verification Criteria

The redesign is successful when:

- the user experiences one coherent app shell
- home, recent, pinned, and workspace feel like sections of one product
- opening a database no longer feels like switching into another application
- the home page clearly prioritizes launch and continuation
- recent and pinned collections are manageable without overloading the home page
- the workspace remains operational during the first redesign phase

## Risks and Mitigations

### Risk: Shell and workspace feel visually disconnected

Mitigation:

- define one shared visual system
- vary density, not identity

### Risk: Navigation pages duplicate data logic

Mitigation:

- centralize collection and session state
- let pages focus on presentation and interaction

### Risk: Scope expands into a full database IDE rewrite

Mitigation:

- keep the first pass focused on shell, navigation, and integration
- defer advanced workspace capabilities

## Recommendation

Implement the redesign as a unified shell with independent navigation pages and an integrated workspace section.

Do not keep the current `Home` vs `Workspace` page switch.

Do not attempt a full workspace IDE rewrite in the same phase.

The right strategy is:

1. fix the product shape
2. stabilize the shell
3. preserve the current workspace capabilities inside that shell
4. evolve the workspace later
