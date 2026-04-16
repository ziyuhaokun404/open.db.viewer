# SQLite UI Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Re-scope Open DB Viewer to a polished SQLite-first product and redesign the current renderer UI so the home, connection, and browsing flows feel coherent and production-ready.

**Architecture:** Keep the existing Electron, React, Zustand, and IPC structure. Implement the change as a renderer-focused refresh: update product copy, simplify the SQLite connection flow, replace dashboard-like home content with a focused launch surface, and normalize the shell and component styling around a quieter Fluent-neutral visual system.

**Tech Stack:** Electron, React 19, TypeScript, Zustand, CSS, Vitest, Testing Library

---

## File Responsibilities

- `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/home-page.tsx`
  Owns the SQLite-first launch surface and saved database framing.
- `C:/Code_Research/Open.Db.Viewer/src/renderer/features/connections/connection-form.tsx`
  Owns the SQLite-only form flow and dialog copy.
- `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/browser-page.tsx`
  Owns the browser workspace hierarchy and page framing.
- `C:/Code_Research/Open.Db.Viewer/src/renderer/app/app-layout.css`
  Owns shell, dialog, home, workspace, and shared control styling.
- `C:/Code_Research/Open.Db.Viewer/src/renderer/styles/design-tokens.css`
  Owns spacing, radius, and neutral/accent token values.
- `C:/Code_Research/Open.Db.Viewer/README.md`
  Owns top-level product positioning.
- `C:/Code_Research/Open.Db.Viewer/tests/renderer/pages/home-page-layout.spec.tsx`
  Covers SQLite-first home layout.
- `C:/Code_Research/Open.Db.Viewer/tests/renderer/connections/connection-form.spec.tsx`
  Covers SQLite-only connection flow.
- `C:/Code_Research/Open.Db.Viewer/tests/renderer/connections/connection-dialog-style.spec.ts`
  Covers neutral overlay and dialog style contract.
- `C:/Code_Research/Open.Db.Viewer/tests/renderer/pages/browser-workspace.spec.tsx`
  Covers browser workspace hierarchy.
- `C:/Code_Research/Open.Db.Viewer/tests/renderer/styles/fluent-shell-theme.spec.ts`
  Covers shared shell and control style contracts.

### Task 1: Refocus Product Copy and Home Messaging on SQLite

**Files:**
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/home-page.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/README.md`
- Test: `C:/Code_Research/Open.Db.Viewer/tests/renderer/pages/home-page-layout.spec.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
it("renders SQLite-first copy", () => {
  render(<HomePage />);

  expect(screen.getByRole("heading", { name: "打开一个 SQLite 数据库" })).toBeInTheDocument();
  expect(screen.getByText("一个轻量、清晰的 SQLite 浏览工具。")).toBeInTheDocument();
  expect(screen.queryByText("MySQL / PostgreSQL / SQLite")).not.toBeInTheDocument();
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- tests/renderer/pages/home-page-layout.spec.tsx`

Expected: FAIL because the current page still uses generic workspace copy and multi-database messaging.

- [ ] **Step 3: Implement the SQLite-first copy**

```tsx
<section className="home-launch" data-testid="home-launch-surface">
  <p className="home-launch__eyebrow">SQLite Viewer</p>
  <h2 className="home-launch__title">打开一个 SQLite 数据库</h2>
  <p className="home-launch__subtitle">一个轻量、清晰的 SQLite 浏览工具。</p>
  <button onClick={openNewConnection} type="button">打开 SQLite 文件</button>
</section>
```

```md
- 当前短期目标聚焦 SQLite：连接管理、对象树、表结构、表数据、SQL 查询、CSV 导出
- MySQL / PostgreSQL 适配器保留在架构层，但不作为当前产品 UI 的活跃范围
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- tests/renderer/pages/home-page-layout.spec.tsx`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add C:/Code_Research/Open.Db.Viewer/src/renderer/pages/home-page.tsx C:/Code_Research/Open.Db.Viewer/README.md C:/Code_Research/Open.Db.Viewer/tests/renderer/pages/home-page-layout.spec.tsx
git commit -m "feat: refocus product copy on sqlite"
```

### Task 2: Simplify the Connection Form to SQLite-Only

**Files:**
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/connections/connection-form.tsx`
- Test: `C:/Code_Research/Open.Db.Viewer/tests/renderer/connections/connection-form.spec.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
it("shows only SQLite in the active form flow", () => {
  render(<ConnectionForm variant="dialog" />);

  expect(screen.getByText("SQLite")).toBeInTheDocument();
  expect(screen.queryByText("MySQL")).not.toBeInTheDocument();
  expect(screen.queryByText("PostgreSQL")).not.toBeInTheDocument();
  expect(screen.getByRole("button", { name: "选择数据库文件" })).toBeInTheDocument();
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- tests/renderer/connections/connection-form.spec.tsx`

Expected: FAIL because the current segmented control still exposes MySQL and PostgreSQL.

- [ ] **Step 3: Implement the SQLite-only flow**

```tsx
<section className="connection-form__panel">
  <h2>SQLite 数据库</h2>
  <p className="connection-form__label">当前版本仅提供 SQLite 工作流。</p>
  <label className="connection-form__field">
    数据库名称
    <input aria-label="数据库名称" value={formValues.name} onChange={(event) => updateForm("name", event.target.value)} />
  </label>
  <label className="connection-form__field">
    数据库文件
    <div className="connection-form__file-picker">
      <input aria-label="数据库文件" value={formValues.filePath} onChange={(event) => updateForm("filePath", event.target.value)} />
      <button className="connection-form__browse" onClick={() => void browseSQLiteFile()} type="button">选择数据库文件</button>
    </div>
  </label>
</section>
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- tests/renderer/connections/connection-form.spec.tsx`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add C:/Code_Research/Open.Db.Viewer/src/renderer/features/connections/connection-form.tsx C:/Code_Research/Open.Db.Viewer/tests/renderer/connections/connection-form.spec.tsx
git commit -m "feat: simplify connection form for sqlite"
```

### Task 3: Replace the Dashboard-Like Home Layout with a Focused Launch Surface

**Files:**
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/home-page.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/app/app-layout.css`
- Test: `C:/Code_Research/Open.Db.Viewer/tests/renderer/pages/home-page-layout.spec.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
it("removes dashboard summary cards from the home page", () => {
  render(<HomePage />);

  expect(screen.getByTestId("home-launch-surface")).toBeInTheDocument();
  expect(screen.queryByText("连接总览")).not.toBeInTheDocument();
  expect(screen.queryByText("数据库类型")).not.toBeInTheDocument();
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- tests/renderer/pages/home-page-layout.spec.tsx`

Expected: FAIL because the current page still renders overview cards.

- [ ] **Step 3: Implement the focused launch layout**

```tsx
<section className="home-workspace">
  <section className="home-launch" data-testid="home-launch-surface">
    <p className="home-launch__eyebrow">SQLite Viewer</p>
    <h2 className="home-launch__title">打开一个 SQLite 数据库</h2>
    <p className="home-launch__subtitle">直接浏览结构、数据和查询结果。</p>
    <button onClick={openNewConnection} type="button">打开 SQLite 文件</button>
  </section>
  <section className="home-library">
    <div className="home-library__header">
      <h3>已保存数据库</h3>
      <p>{connectionSummary}</p>
    </div>
  </section>
</section>
```

```css
.home-workspace {
  display: grid;
  gap: 16px;
}

.home-launch,
.home-library,
.sidebar-panel {
  background: var(--surface-2);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-card);
  padding: 20px;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- tests/renderer/pages/home-page-layout.spec.tsx`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add C:/Code_Research/Open.Db.Viewer/src/renderer/pages/home-page.tsx C:/Code_Research/Open.Db.Viewer/src/renderer/app/app-layout.css C:/Code_Research/Open.Db.Viewer/tests/renderer/pages/home-page-layout.spec.tsx
git commit -m "feat: replace home dashboard with sqlite launch surface"
```

### Task 4: Calm the Dialog Overlay and Shared Surface Styling

**Files:**
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/app/app-layout.css`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/styles/design-tokens.css`
- Test: `C:/Code_Research/Open.Db.Viewer/tests/renderer/connections/connection-dialog-style.spec.ts`
- Test: `C:/Code_Research/Open.Db.Viewer/tests/renderer/styles/fluent-shell-theme.spec.ts`

- [ ] **Step 1: Write the failing tests**

```ts
it("uses a neutral scrim without gradient background", () => {
  const css = fs.readFileSync("src/renderer/app/app-layout.css", "utf8");
  const dialogLayer = css.match(/\\.dialog-layer\\s*\\{[^}]*background:\\s*([^;]+);/s)?.[1] ?? "";
  expect(dialogLayer).toContain("var(--overlay-scrim)");
  expect(dialogLayer).not.toContain("gradient");
});
```

```ts
it("defines compact radius tokens for the refreshed shell", () => {
  const tokens = fs.readFileSync("src/renderer/styles/design-tokens.css", "utf8");
  expect(tokens).toContain("--radius-sm:");
  expect(tokens).toContain("--radius-md:");
});
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `npm test -- tests/renderer/connections/connection-dialog-style.spec.ts tests/renderer/styles/fluent-shell-theme.spec.ts`

Expected: FAIL because the old styling still carries decorative surface assumptions.

- [ ] **Step 3: Implement calmer shell and dialog styling**

```css
.dialog-layer {
  align-items: center;
  backdrop-filter: blur(18px);
  background: var(--overlay-scrim);
  display: flex;
  inset: 34px 0 0;
  justify-content: center;
  padding: 16px;
  position: fixed;
  z-index: 20;
}

.dialog-layer__surface {
  background: var(--surface-2);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-soft);
  padding: 20px;
}
```

```css
--radius-sm: 10px;
--radius-md: 16px;
--radius-lg: 22px;
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `npm test -- tests/renderer/connections/connection-dialog-style.spec.ts tests/renderer/styles/fluent-shell-theme.spec.ts`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add C:/Code_Research/Open.Db.Viewer/src/renderer/app/app-layout.css C:/Code_Research/Open.Db.Viewer/src/renderer/styles/design-tokens.css C:/Code_Research/Open.Db.Viewer/tests/renderer/connections/connection-dialog-style.spec.ts C:/Code_Research/Open.Db.Viewer/tests/renderer/styles/fluent-shell-theme.spec.ts
git commit -m "style: calm dialog and shell surfaces"
```

### Task 5: Tighten the Browser Workspace Hierarchy

**Files:**
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/browser-page.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/app/app-layout.css`
- Test: `C:/Code_Research/Open.Db.Viewer/tests/renderer/pages/browser-workspace.spec.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
it("renders a compact browser workspace shell", () => {
  render(<BrowserPage />);

  expect(screen.getByTestId("browser-workspace-shell")).toBeInTheDocument();
  expect(screen.getByTestId("browser-workspace-tabs")).toBeInTheDocument();
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- tests/renderer/pages/browser-workspace.spec.tsx`

Expected: FAIL because the current page does not expose the refreshed hierarchy hooks.

- [ ] **Step 3: Implement the tighter workspace framing**

```tsx
<section className="workspace-shell" data-testid="browser-workspace-shell">
  <PageHeader
    title={selectedNode?.name ?? "请选择一个表"}
    description={selectedNode?.kind === "table" ? "SQLite 工作区" : "从左侧选择一个表"}
    action={<button className="workspace-header__back" onClick={goHome} type="button">返回数据库列表</button>}
  />
  <div data-testid="browser-workspace-tabs">
    <TabBar tabs={["结构", "数据", "查询"]} activeTab={activeTab} onChange={setActiveTab} />
  </div>
  {renderActiveTab()}
</section>
```

```css
.workspace-shell {
  display: grid;
  gap: 12px;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- tests/renderer/pages/browser-workspace.spec.tsx`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add C:/Code_Research/Open.Db.Viewer/src/renderer/pages/browser-page.tsx C:/Code_Research/Open.Db.Viewer/src/renderer/app/app-layout.css C:/Code_Research/Open.Db.Viewer/tests/renderer/pages/browser-workspace.spec.tsx
git commit -m "style: tighten browser workspace hierarchy"
```

### Task 6: Normalize Shared Control Density and Emphasis

**Files:**
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/app/app-layout.css`
- Test: `C:/Code_Research/Open.Db.Viewer/tests/renderer/styles/fluent-shell-theme.spec.ts`

- [ ] **Step 1: Write the failing test**

```ts
it("keeps primary actions accented and secondary actions neutral", () => {
  const css = fs.readFileSync("src/renderer/app/app-layout.css", "utf8");
  expect(css).toMatch(/\\.connection-form__primary-action\\s*\\{[^}]*background:\\s*[^;]*var\\(--accent\\)/s);
  expect(css).toMatch(/\\.connection-form__secondary-action\\s*\\{[^}]*background:\\s*var\\(--surface-3\\)/s);
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- tests/renderer/styles/fluent-shell-theme.spec.ts`

Expected: FAIL if the refreshed control hierarchy is not yet encoded in the stylesheet.

- [ ] **Step 3: Implement unified control density**

```css
.workspace-header__back,
.connection-form__browse,
.connection-form__secondary-action,
.connection-form__primary-action,
.connection-list__open,
.workspace-tabs__tab {
  border-radius: var(--radius-sm);
  font-size: 13px;
  min-height: 34px;
}

.connection-form__secondary-action,
.connection-list__open,
.workspace-header__back {
  background: var(--surface-3);
  border: 1px solid var(--border-subtle);
  color: var(--color-text-primary);
}

.connection-form__primary-action {
  background: var(--accent);
  border: 1px solid var(--accent);
  color: #ffffff;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- tests/renderer/styles/fluent-shell-theme.spec.ts`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add C:/Code_Research/Open.Db.Viewer/src/renderer/app/app-layout.css C:/Code_Research/Open.Db.Viewer/tests/renderer/styles/fluent-shell-theme.spec.ts
git commit -m "style: normalize control density and emphasis"
```

## Plan Self-Review

Spec coverage:

- SQLite-only scope: Task 1 and Task 2
- home redesign: Task 1 and Task 3
- calmer dialog and shell: Task 4
- denser browser workspace: Task 5
- cleaner control hierarchy: Task 6

Placeholder scan:

- No `TODO`, `TBD`, or "implement later" placeholders remain

Type consistency:

- `home-launch-surface`, `browser-workspace-shell`, and `browser-workspace-tabs` are consistent between tests and implementation snippets
