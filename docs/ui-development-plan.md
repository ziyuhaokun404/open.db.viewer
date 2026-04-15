# Open DB Viewer UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the Open DB Viewer renderer UI into a lightweight, easy-to-learn desktop workbench with a stable shell, calmer visual system, and a clearer connection-to-browse workflow.

**Architecture:** Keep the existing Electron main-process and Zustand data flow, but introduce a renderer-side UI shell, shared visual tokens, and reusable layout/state components. The first implementation pass should focus on structure and interaction hierarchy rather than visual ornament, then refine empty states, query workspace, and table presentation within that framework.

**Tech Stack:** Electron, React, TypeScript, Vite, Zustand, Vitest, React Testing Library

---

## 1. 计划说明

本计划基于以下文档：

- [ui-product-plan.md](C:/Code_Research/Open.Db.Viewer/docs/ui-product-plan.md)
- [product-document.md](C:/Code_Research/Open.Db.Viewer/docs/product-document.md)
- [prototype-structure.md](C:/Code_Research/Open.Db.Viewer/docs/prototype-structure.md)

本计划只覆盖 UI 补齐与前端体验重构，不改变主进程服务边界，不在本阶段接入新的数据库驱动能力。

## 2. UI 重构范围

本轮实施重点覆盖以下 6 个方面：

1. 建立全局视觉变量与基础样式
2. 引入统一的应用壳层 `AppShell`
3. 重做首页为“工作台首页”
4. 重做浏览页为稳定的 tab 工作区
5. 升级连接表单、查询区、表格区的视觉层级
6. 补齐统一的空态 / 加载态 / 反馈组件

## 3. 目标文件结构

```text
src/
  renderer/
    app/
      App.tsx
      app-shell.tsx
      app-layout.css
    components/
      status-panel.tsx
      empty-state.tsx
      page-header.tsx
      segmented-control.tsx
      tab-bar.tsx
      top-bar.tsx
    pages/
      home-page.tsx
      browser-page.tsx
      connection-form-page.tsx
    features/
      connections/
        connection-form.tsx
        connection-list.tsx
      explorer/
        object-tree.tsx
      schema-view/
        schema-table.tsx
      data-view/
        data-grid.tsx
      query-view/
        query-editor.tsx
        query-result-grid.tsx
    styles/
      design-tokens.css
      base.css
tests/
  renderer/
    app/
    components/
    pages/
```

## 4. 里程碑拆分

建议拆成 4 个 UI 里程碑：

1. 视觉基线与应用壳层
2. 首页工作台与连接体验
3. 浏览页工作区与表格/查询重构
4. 状态系统与体验收尾

## 5. 任务拆解

### Task 1: 建立视觉基线与全局样式入口

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/styles/design-tokens.css`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/styles/base.css`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/main.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/app/visual-baseline.spec.tsx`

- [ ] **Step 1: 写视觉基线存在性测试**

```tsx
// tests/renderer/app/visual-baseline.spec.tsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

function VisualProbe() {
  return <div data-testid="visual-probe">Open DB Viewer</div>;
}

describe("visual baseline", () => {
  it("renders probe node for style bootstrap", () => {
    render(<VisualProbe />);
    expect(screen.getByTestId("visual-probe")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: 运行测试确认基线测试可执行**

Run: `npm test -- visual-baseline.spec.tsx`
Expected: PASS with `1 passed`

- [ ] **Step 3: 创建设计令牌文件**

```css
/* src/renderer/styles/design-tokens.css */
:root {
  --app-bg: #eef2f6;
  --surface-1: #f8fafc;
  --surface-2: #ffffff;
  --surface-3: #edf3f8;
  --border-subtle: #d7e0e8;
  --text-strong: #18222d;
  --text-body: #415161;
  --text-muted: #6a7a8b;
  --accent: #1f7a8c;
  --accent-soft: #d9eef2;
  --success-bg: #eaf8ef;
  --success-text: #1f6b45;
  --error-bg: #fff1f1;
  --error-text: #8c2f39;
  --radius-sm: 10px;
  --radius-md: 16px;
  --radius-lg: 22px;
  --space-2: 8px;
  --space-3: 12px;
  --space-4: 16px;
  --space-5: 20px;
  --space-6: 24px;
  --space-8: 32px;
  --shadow-soft: 0 10px 30px rgba(31, 44, 61, 0.06);
  --font-sans: "Segoe UI", "PingFang SC", "Microsoft YaHei", sans-serif;
  --font-mono: "Cascadia Code", "SFMono-Regular", Consolas, monospace;
}
```

- [ ] **Step 4: 创建基础样式文件**

```css
/* src/renderer/styles/base.css */
@import "./design-tokens.css";

* {
  box-sizing: border-box;
}

html,
body,
#root {
  margin: 0;
  min-height: 100%;
}

body {
  background:
    radial-gradient(circle at top left, rgba(31, 122, 140, 0.08), transparent 28%),
    linear-gradient(180deg, #f8fbfd 0%, var(--app-bg) 100%);
  color: var(--text-strong);
  font-family: var(--font-sans);
}

button,
input,
select,
textarea {
  font: inherit;
}
```

- [ ] **Step 5: 在渲染入口接入全局样式**

```tsx
// src/renderer/main.tsx
import React from "react";
import ReactDOM from "react-dom/client";
import { App } from "./app/App";
import "./styles/base.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
```

- [ ] **Step 6: 运行测试、类型检查与构建**

Run: `npm test && npm run typecheck && npm run build`
Expected: all commands succeed

### Task 2: 建立应用壳层与顶部/侧栏骨架

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/app/app-shell.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/app/app-layout.css`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/components/top-bar.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/app/App.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/app/app-shell.spec.tsx`

- [ ] **Step 1: 写壳层渲染测试**

```tsx
// tests/renderer/app/app-shell.spec.tsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { AppShell } from "../../../src/renderer/app/app-shell";

describe("AppShell", () => {
  it("renders top bar and main area", () => {
    render(
      <AppShell title="首页" sidebar={<div>侧栏</div>}>
        <div>主区域</div>
      </AppShell>
    );

    expect(screen.getByText("首页")).toBeInTheDocument();
    expect(screen.getByText("侧栏")).toBeInTheDocument();
    expect(screen.getByText("主区域")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: 运行测试确认测试先通过最小渲染**

Run: `npm test -- app-shell.spec.tsx`
Expected: FAIL with module not found for `app-shell`

- [ ] **Step 3: 实现顶部栏组件**

```tsx
// src/renderer/components/top-bar.tsx
import type { ReactNode } from "react";

export function TopBar({
  title,
  subtitle,
  actions
}: {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
}) {
  return (
    <header className="top-bar">
      <div>
        <p className="top-bar__eyebrow">Open DB Viewer</p>
        <h1 className="top-bar__title">{title}</h1>
        {subtitle ? <p className="top-bar__subtitle">{subtitle}</p> : null}
      </div>
      {actions ? <div className="top-bar__actions">{actions}</div> : null}
    </header>
  );
}
```

- [ ] **Step 4: 实现应用壳层**

```tsx
// src/renderer/app/app-shell.tsx
import type { ReactNode } from "react";
import { TopBar } from "../components/top-bar";
import "./app-layout.css";

export function AppShell({
  title,
  subtitle,
  sidebar,
  actions,
  children,
  footer
}: {
  title: string;
  subtitle?: string;
  sidebar: ReactNode;
  actions?: ReactNode;
  children: ReactNode;
  footer?: ReactNode;
}) {
  return (
    <div className="app-shell">
      <TopBar title={title} subtitle={subtitle} actions={actions} />
      <div className="app-shell__body">
        <aside className="app-shell__sidebar">{sidebar}</aside>
        <main className="app-shell__main">{children}</main>
      </div>
      {footer ? <footer className="app-shell__footer">{footer}</footer> : null}
    </div>
  );
}
```

- [ ] **Step 5: 创建壳层样式**

```css
/* src/renderer/app/app-layout.css */
.app-shell {
  display: grid;
  gap: var(--space-6);
  min-height: 100vh;
  padding: var(--space-6);
}

.app-shell__body {
  display: grid;
  gap: var(--space-6);
  grid-template-columns: 320px minmax(0, 1fr);
}

.app-shell__sidebar,
.app-shell__main,
.app-shell__footer,
.top-bar {
  background: rgba(255, 255, 255, 0.82);
  backdrop-filter: blur(10px);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-soft);
}

.app-shell__sidebar,
.app-shell__main,
.app-shell__footer {
  padding: var(--space-6);
}

.top-bar {
  align-items: center;
  display: flex;
  justify-content: space-between;
  padding: var(--space-5) var(--space-6);
}
```

- [ ] **Step 6: 在 `App.tsx` 中保留现有路由分支，但由各页面接入 `AppShell`**

```tsx
// src/renderer/app/App.tsx
import { BrowserPage } from "../pages/browser-page";
import { ConnectionFormPage } from "../pages/connection-form-page";
import { HomePage } from "../pages/home-page";
import { useConnectionStore } from "../stores/connection-store";

export function App() {
  const currentView = useConnectionStore((state) => state.currentView);

  if (currentView === "new-connection") {
    return <ConnectionFormPage />;
  }

  if (currentView === "browser") {
    return <BrowserPage />;
  }

  return <HomePage />;
}
```

- [ ] **Step 7: 运行壳层测试、全量测试与构建**

Run: `npm test && npm run build`
Expected: all tests pass and build succeeds

### Task 3: 重做首页为工作台首页

**Files:**
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/home-page.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/connections/connection-list.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/components/empty-state.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/pages/home-page-layout.spec.tsx`

- [ ] **Step 1: 写首页布局测试**

```tsx
// tests/renderer/pages/home-page-layout.spec.tsx
import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { HomePage } from "../../../src/renderer/pages/home-page";
import { useConnectionStore } from "../../../src/renderer/stores/connection-store";

beforeEach(() => {
  useConnectionStore.getState().reset();
});

describe("HomePage layout", () => {
  it("renders workspace hero and connection sidebar sections", async () => {
    render(<HomePage />);

    expect(await screen.findByText("轻量数据库工作台")).toBeInTheDocument();
    expect(screen.getByText("已保存连接")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: 运行测试确认当前首页尚未满足新结构**

Run: `npm test -- home-page-layout.spec.tsx`
Expected: FAIL with missing text `轻量数据库工作台`

- [ ] **Step 3: 创建统一空态组件**

```tsx
// src/renderer/components/empty-state.tsx
import type { ReactNode } from "react";

export function EmptyState({
  title,
  message,
  action
}: {
  title: string;
  message: string;
  action?: ReactNode;
}) {
  return (
    <section>
      <h2>{title}</h2>
      <p>{message}</p>
      {action ? <div>{action}</div> : null}
    </section>
  );
}
```

- [ ] **Step 4: 重写首页为工作台结构**

```tsx
// src/renderer/pages/home-page.tsx
import { useEffect } from "react";
import { AppShell } from "../app/app-shell";
import { EmptyState } from "../components/empty-state";
import { StatusPanel } from "../components/status-panel";
import { ConnectionList } from "../features/connections/connection-list";
import { useConnectionStore } from "../stores/connection-store";

export function HomePage() {
  const {
    savedConnections,
    isLoadingList,
    saveMessage,
    loadConnections,
    openNewConnection,
    openConnection
  } = useConnectionStore();

  useEffect(() => {
    void loadConnections();
  }, [loadConnections]);

  const sidebar = (
    <section>
      <h2>已保存连接</h2>
      {isLoadingList ? (
        <StatusPanel variant="loading" message="正在加载连接列表..." />
      ) : savedConnections.length > 0 ? (
        <ConnectionList connections={savedConnections} onOpen={openConnection} />
      ) : (
        <EmptyState
          title="还没有连接"
          message="从左侧开始新建第一个数据库连接。"
          action={
            <button onClick={openNewConnection} type="button">
              新建连接
            </button>
          }
        />
      )}
    </section>
  );

  return (
    <AppShell
      title="轻量数据库工作台"
      subtitle="用更低的学习成本完成连接、浏览、查看和导出。"
      sidebar={sidebar}
      actions={
        <button onClick={openNewConnection} type="button">
          新建连接
        </button>
      }
      footer={saveMessage ? <StatusPanel variant="success" message={saveMessage} /> : null}
    >
      <section>
        <p>欢迎回来</p>
        <h2>从左侧打开已有连接，或创建一个新的连接。</h2>
      </section>
    </AppShell>
  );
}
```

- [ ] **Step 5: 调整连接列表组件以匹配卡片化工作台布局**

```tsx
// src/renderer/features/connections/connection-list.tsx
import type { ConnectionProfile } from "../../../shared/models/connection";

export function ConnectionList({
  connections,
  onOpen
}: {
  connections: ConnectionProfile[];
  onOpen: (connection: ConnectionProfile) => void;
}) {
  return (
    <ul>
      {connections.map((connection) => (
        <li key={connection.id}>
          <button onClick={() => onOpen(connection)} type="button">
            <strong>{connection.name}</strong>
            <span>{connection.type}</span>
          </button>
        </li>
      ))}
    </ul>
  );
}
```

- [ ] **Step 6: 运行首页相关测试**

Run: `npm test -- home-page-layout.spec.tsx connection-form.spec.tsx app.spec.tsx`
Expected: PASS with updated homepage assertions

### Task 4: 重做连接表单为低压力连接面板

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/components/segmented-control.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/connections/connection-form.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/connection-form-page.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/connections/connection-form-layout.spec.tsx`

- [ ] **Step 1: 写连接面板结构测试**

```tsx
// tests/renderer/connections/connection-form-layout.spec.tsx
import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { ConnectionForm } from "../../../src/renderer/features/connections/connection-form";
import { useConnectionStore } from "../../../src/renderer/stores/connection-store";

beforeEach(() => {
  useConnectionStore.getState().reset();
});

describe("ConnectionForm layout", () => {
  it("renders grouped sections and segmented type selector", () => {
    render(<ConnectionForm />);
    expect(screen.getByText("数据库类型")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "SQLite" })).toBeInTheDocument();
    expect(screen.getByText("连接信息")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: 运行测试确认现有结构未满足**

Run: `npm test -- connection-form-layout.spec.tsx`
Expected: FAIL with missing button `SQLite`

- [ ] **Step 3: 实现分段选择组件**

```tsx
// src/renderer/components/segmented-control.tsx
export function SegmentedControl<T extends string>({
  value,
  options,
  onChange
}: {
  value: T;
  options: Array<{ label: string; value: T }>;
  onChange: (value: T) => void;
}) {
  return (
    <div role="group" aria-label="segmented-control">
      {options.map((option) => (
        <button
          key={option.value}
          aria-pressed={option.value === value}
          onClick={() => onChange(option.value)}
          type="button"
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}
```

- [ ] **Step 4: 重构连接表单布局**

```tsx
// src/renderer/features/connections/connection-form.tsx
import type { ChangeEvent } from "react";
import { SegmentedControl } from "../../components/segmented-control";
import { StatusPanel } from "../../components/status-panel";
import { useConnectionStore } from "../../stores/connection-store";

// 保留 DatabaseFields，但在组件内改成“基础信息 / 连接信息”分组展示
// 顶部使用 SegmentedControl 替换 select，下方保留测试和保存按钮
// 页面文案增加“先测试连接，再保存到左侧工作台”
```

- [ ] **Step 5: 用工作台壳层包裹连接页**

```tsx
// src/renderer/pages/connection-form-page.tsx
import { AppShell } from "../app/app-shell";
import { ConnectionForm } from "../features/connections/connection-form";
import { useConnectionStore } from "../stores/connection-store";

export function ConnectionFormPage() {
  const goHome = useConnectionStore((state) => state.goHome);

  return (
    <AppShell
      title="新建连接"
      subtitle="先完成最少必要信息，再测试连接。"
      sidebar={<p>连接创建后会出现在左侧连接列表中。</p>}
      actions={
        <button onClick={goHome} type="button">
          返回首页
        </button>
      }
    >
      <ConnectionForm />
    </AppShell>
  );
}
```

- [ ] **Step 6: 运行连接相关测试**

Run: `npm test -- connection-form.spec.tsx connection-form-layout.spec.tsx`
Expected: PASS

### Task 5: 重做浏览页为 tab 工作区

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/components/tab-bar.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/components/page-header.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/browser-page.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/stores/explorer-store.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/pages/browser-workspace.spec.tsx`

- [ ] **Step 1: 写浏览页工作区测试**

```tsx
// tests/renderer/pages/browser-workspace.spec.tsx
import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { App } from "../../../src/renderer/app/App";
import { useConnectionStore } from "../../../src/renderer/stores/connection-store";
import { useDataViewStore } from "../../../src/renderer/stores/data-view-store";
import { useExplorerStore } from "../../../src/renderer/stores/explorer-store";
import { useSchemaStore } from "../../../src/renderer/stores/schema-store";

const invokeMock = vi.fn();

beforeEach(() => {
  invokeMock.mockReset();
  useConnectionStore.getState().reset();
  useDataViewStore.getState().reset();
  useExplorerStore.getState().reset();
  useSchemaStore.getState().reset();
  window.electron = { ipcRenderer: { invoke: invokeMock } };
});

describe("Browser workspace", () => {
  it("switches between 结构 and 数据 tabs", async () => {
    invokeMock.mockImplementation(async (channel: string) => {
      if (channel === "connection:list") {
        return [{ id: "sqlite-1", type: "sqlite", name: "Local SQLite", filePath: "demo.db" }];
      }
      if (channel === "explorer:load") {
        return [{ id: "table:users", kind: "table", name: "users" }];
      }
      if (channel === "schema:get") {
        return { tableName: "users", columns: [{ name: "id", dataType: "integer", nullable: false, defaultValue: null, isPrimaryKey: true }] };
      }
      if (channel === "table-data:get") {
        return { columns: ["id"], rows: [{ id: 1 }], page: 1, pageSize: 50, hasNextPage: false };
      }
      return [];
    });

    render(<App />);
    fireEvent.click(await screen.findByRole("button", { name: "打开 Local SQLite" }));
    fireEvent.click(await screen.findByRole("button", { name: "选择 users" }));
    fireEvent.click(await screen.findByRole("button", { name: "数据" }));

    expect(await screen.findByText("表数据")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: 运行测试确认浏览页还未支持 tab 工作区**

Run: `npm test -- browser-workspace.spec.tsx`
Expected: FAIL with missing button `数据`

- [ ] **Step 3: 创建页头与 tab 组件**

```tsx
// src/renderer/components/page-header.tsx
export function PageHeader({
  title,
  description
}: {
  title: string;
  description?: string;
}) {
  return (
    <section>
      <p>当前对象</p>
      <h2>{title}</h2>
      {description ? <p>{description}</p> : null}
    </section>
  );
}

// src/renderer/components/tab-bar.tsx
export function TabBar({
  tabs,
  activeTab,
  onChange
}: {
  tabs: string[];
  activeTab: string;
  onChange: (tab: string) => void;
}) {
  return (
    <div role="tablist" aria-label="workspace-tabs">
      {tabs.map((tab) => (
        <button
          key={tab}
          aria-pressed={tab === activeTab}
          onClick={() => onChange(tab)}
          type="button"
        >
          {tab}
        </button>
      ))}
    </div>
  );
}
```

- [ ] **Step 4: 在浏览页中引入本地 tab 状态并只渲染一个主内容区**

```tsx
// src/renderer/pages/browser-page.tsx
import { useEffect, useState } from "react";
// ...

export function BrowserPage() {
  const [activeTab, setActiveTab] = useState("结构");
  // 保留现有 store 调用逻辑
  // 选中表时默认 setActiveTab("结构")
  // 主内容区改成：页头 + TabBar + activeTab 对应内容
}
```

- [ ] **Step 5: 在 explorer store 中保留当前选中节点，避免 tab 切换误清空**

```ts
// src/renderer/stores/explorer-store.ts
selectNode(node) {
  set({ selectedNode: node });
}
```

- [ ] **Step 6: 运行浏览页相关测试**

Run: `npm test -- browser-workspace.spec.tsx schema-table.spec.tsx data-grid.spec.tsx query-editor.spec.tsx`
Expected: PASS

### Task 6: 升级表格与查询区域的视觉层级

**Files:**
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/schema-view/schema-table.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/data-view/data-grid.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/query-view/query-editor.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/query-view/query-result-grid.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/pages/query-workspace-layout.spec.tsx`

- [ ] **Step 1: 写查询工作区布局测试**

```tsx
// tests/renderer/pages/query-workspace-layout.spec.tsx
import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { QueryEditor } from "../../../src/renderer/features/query-view/query-editor";
import { useQueryStore } from "../../../src/renderer/stores/query-store";

beforeEach(() => {
  useQueryStore.getState().reset();
});

describe("QueryEditor layout", () => {
  it("renders execute, clear, and helper text", () => {
    render(<QueryEditor onExecute={() => undefined} />);
    expect(screen.getByRole("button", { name: "执行" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "清空" })).toBeInTheDocument();
    expect(screen.getByText("输入一条简单 SQL")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: 运行测试确认当前查询编辑器缺少清空按钮**

Run: `npm test -- query-workspace-layout.spec.tsx`
Expected: FAIL with missing button `清空`

- [ ] **Step 3: 重构查询编辑器为上下结构**

```tsx
// src/renderer/features/query-view/query-editor.tsx
import type { ChangeEvent } from "react";
import { StatusPanel } from "../../components/status-panel";
import { useQueryStore } from "../../stores/query-store";

export function QueryEditor({ onExecute }: { onExecute: () => void }) {
  const sqlText = useQueryStore((state) => state.sqlText);
  const isExecuting = useQueryStore((state) => state.isExecuting);
  const result = useQueryStore((state) => state.result);
  const updateSqlText = useQueryStore((state) => state.updateSqlText);

  const handleChange = (event: ChangeEvent<HTMLTextAreaElement>) => {
    updateSqlText(event.target.value);
  };

  return (
    <section>
      <h3>查询工作区</h3>
      <p>输入一条简单 SQL，然后在下方查看结果。</p>
      <div>
        <button onClick={onExecute} type="button">
          {isExecuting ? "执行中..." : "执行"}
        </button>
        <button onClick={() => updateSqlText("")} type="button">
          清空
        </button>
      </div>
      <textarea aria-label="SQL 输入区" rows={8} value={sqlText} onChange={handleChange} />
      {isExecuting ? <StatusPanel variant="loading" message="SQL 正在执行，请稍候。" /> : null}
      {!isExecuting && result ? <StatusPanel variant="success" message={`已返回 ${result.rowCount} 行。`} /> : null}
    </section>
  );
}
```

- [ ] **Step 4: 提升表结构与数据表格的阅读层级**

```tsx
// src/renderer/features/schema-view/schema-table.tsx
// 为标题区增加表名说明，并用更清楚的表头文案保留当前功能

// src/renderer/features/data-view/data-grid.tsx
// 为工具条增加“刷新 / 导出 / 分页信息”布局，并保留现有分页占位

// src/renderer/features/query-view/query-result-grid.tsx
// 增加“结果摘要区”，将耗时和行数并列显示
```

- [ ] **Step 5: 运行表格与查询测试**

Run: `npm test -- query-editor.spec.tsx query-workspace-layout.spec.tsx data-grid.spec.tsx schema-table.spec.tsx`
Expected: PASS

### Task 7: 收口状态系统与底部反馈区

**Files:**
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/components/status-panel.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/home-page.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/browser-page.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/components/empty-state.spec.tsx`

- [ ] **Step 1: 写空态组件测试**

```tsx
// tests/renderer/components/empty-state.spec.tsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { EmptyState } from "../../../src/renderer/components/empty-state";

describe("EmptyState", () => {
  it("renders title and message", () => {
    render(<EmptyState title="暂无内容" message="请选择左侧连接。" />);
    expect(screen.getByText("暂无内容")).toBeInTheDocument();
    expect(screen.getByText("请选择左侧连接。")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: 运行测试确认空态组件持续可用**

Run: `npm test -- empty-state.spec.tsx status-panel.spec.tsx`
Expected: PASS

- [ ] **Step 3: 强化状态组件的标题、说明和动作布局**

```tsx
// src/renderer/components/status-panel.tsx
// 保留现有 variant 设计，但统一 title/message/action 间距
// 让 loading / empty / error / success 在视觉上更像同一产品系统
```

- [ ] **Step 4: 将首页和浏览页的 footer 改为真正的反馈带**

```tsx
// src/renderer/pages/home-page.tsx
// footer 中优先显示保存成功、加载状态等反馈

// src/renderer/pages/browser-page.tsx
// footer 中优先显示当前连接、选中对象、导出结果、查询反馈
```

- [ ] **Step 5: 跑最终 UI 校验**

Run: `npm run lint && npm run typecheck && npm test && npm run build`
Expected: all commands succeed

## 6. 计划自检

### 6.1 覆盖检查

本计划已覆盖以下 UI 需求：

- 视觉基线与设计令牌
- 稳定工作台壳层
- 工作台首页
- 连接面板重构
- 浏览页 tab 化
- 表结构 / 表数据 / 查询区视觉升级
- 空态 / 加载态 / 成功态 / 错误态统一

### 6.2 风险提醒

执行过程中需要特别关注：

- 现有测试文案依赖较强，UI 重构会导致测试同步更新较多
- 浏览页从纵向堆叠改为 tab 切换后，要避免数据加载和显示状态互相影响
- 不能为了视觉效果破坏现有 IPC 和 Zustand 数据流
- 样式文件引入后要确认 Electron 渲染构建没有路径问题

### 6.3 建议实施顺序

建议严格按任务顺序执行：

1. 先做视觉变量和壳层
2. 再做首页和连接体验
3. 再做浏览页 tab 化
4. 最后统一状态和细节

## 7. 结论

这份 UI 实施计划的核心思路是：

- 先做结构
- 再做主路径
- 最后做细节

这样可以最快把 Open DB Viewer 从“功能 demo”提升成“极简效率型数据库工作台”。
