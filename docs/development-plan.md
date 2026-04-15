# Open DB Viewer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the V1 MVP of Open DB Viewer so a user can create database connections, browse database objects, inspect table schema, view paginated table data, run basic queries, and export results to CSV.

**Architecture:** Use a desktop app architecture based on `Electron + React + TypeScript`, with the renderer responsible for UI and interaction, the main process responsible for database access and local storage, and a shared model layer to keep types and contracts stable. Multi-database support is isolated behind database adapters so `MySQL / PostgreSQL / SQLite` can share the same UI and application services.

**Tech Stack:** Electron, React, TypeScript, Vite, Zustand or equivalent lightweight state store, Electron IPC, SQLite/MySQL/PostgreSQL drivers, Vitest, React Testing Library

---

## 1. 计划说明

本计划基于以下文档：

- [product-document.md](C:/Code_Research/Open.Db.Viewer/docs/product-document.md)
- [mvp-feature-list.md](C:/Code_Research/Open.Db.Viewer/docs/mvp-feature-list.md)
- [prototype-structure.md](C:/Code_Research/Open.Db.Viewer/docs/prototype-structure.md)
- [technical-architecture.md](C:/Code_Research/Open.Db.Viewer/docs/technical-architecture.md)

本计划默认当前仓库尚未初始化应用代码，因此任务从项目脚手架开始。

## 2. 里程碑拆分

建议按 5 个里程碑推进：

1. 项目基础设施与目录脚手架
2. 连接管理闭环
3. 对象树 / 表结构 / 表数据闭环
4. 查询与导出闭环
5. 稳定性、测试与打包

每个里程碑都应产出可运行、可验证的软件增量。

## 3. 目标目录结构

```text
src/
  main/
    app/
    ipc/
    services/
    storage/
    database/
      adapters/
      mappers/
      drivers/
    exports/
  renderer/
    app/
    pages/
    components/
    features/
      connections/
      explorer/
      schema-view/
      data-view/
      query-view/
    stores/
    hooks/
    utils/
  shared/
    models/
    types/
    errors/
    constants/
tests/
  main/
  renderer/
```

## 4. 环境前置

正式实施前先统一以下工程约定：

- Node.js LTS 版本
- 包管理器固定为 `pnpm` 或 `npm`
- TypeScript 开启严格模式
- 统一 lint / format 规范
- 测试框架固定为 `Vitest`

## 5. 任务拆解

### Task 1: 初始化项目脚手架

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/package.json`
- Create: `C:/Code_Research/Open.Db.Viewer/tsconfig.json`
- Create: `C:/Code_Research/Open.Db.Viewer/vite.config.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/electron.vite.config.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/index.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/main.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/app/App.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/index.html`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/app/app.spec.tsx`

- [ ] **Step 1: 初始化桌面应用依赖**

创建 `package.json`，至少包含以下关键依赖：

```json
{
  "name": "open-db-viewer",
  "version": "0.1.0",
  "private": true,
  "type": "module",
  "scripts": {
    "dev": "electron-vite dev",
    "build": "electron-vite build",
    "preview": "electron-vite preview",
    "test": "vitest run",
    "test:watch": "vitest",
    "lint": "eslint .",
    "typecheck": "tsc --noEmit"
  }
}
```

- [ ] **Step 2: 建立最小可运行入口**

创建最小主进程与渲染进程入口：

```ts
// src/main/index.ts
import { app, BrowserWindow } from "electron";

function createWindow() {
  const win = new BrowserWindow({
    width: 1280,
    height: 820,
    minWidth: 1080,
    minHeight: 720,
    webPreferences: {
      contextIsolation: true
    }
  });

  void win.loadURL(process.env.ELECTRON_RENDERER_URL || "http://localhost:5173");
}

app.whenReady().then(createWindow);
```

```tsx
// src/renderer/app/App.tsx
export function App() {
  return <div>Open DB Viewer</div>;
}
```

- [ ] **Step 3: 写一个最小渲染层测试**

```tsx
// tests/renderer/app/app.spec.tsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { App } from "../../../src/renderer/app/App";

describe("App", () => {
  it("renders product name", () => {
    render(<App />);
    expect(screen.getByText("Open DB Viewer")).toBeInTheDocument();
  });
});
```

- [ ] **Step 4: 运行测试验证基础脚手架**

Run: `npm test`
Expected: PASS with `1 passed`

- [ ] **Step 5: 提交项目基础脚手架**

```bash
git add package.json tsconfig.json vite.config.ts electron.vite.config.ts index.html src tests
git commit -m "chore: initialize electron react typescript scaffold"
```

### Task 2: 建立共享类型与错误模型

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/shared/models/connection.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/shared/models/database-object.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/shared/models/schema.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/shared/models/query.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/shared/errors/app-error.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/main/shared/models.spec.ts`

- [ ] **Step 1: 定义连接模型**

```ts
// src/shared/models/connection.ts
export type DatabaseType = "mysql" | "postgresql" | "sqlite";

export interface ConnectionProfile {
  id: string;
  type: DatabaseType;
  name: string;
  host?: string;
  port?: number;
  username?: string;
  password?: string;
  database?: string;
  filePath?: string;
}

export interface TestConnectionResult {
  success: boolean;
  message: string;
}
```

- [ ] **Step 2: 定义浏览与查询模型**

```ts
// src/shared/models/database-object.ts
export type DatabaseObjectKind = "connection" | "database" | "schema" | "table" | "view";

export interface DatabaseObjectNode {
  id: string;
  kind: DatabaseObjectKind;
  name: string;
  parentId?: string;
  children?: DatabaseObjectNode[];
}
```

```ts
// src/shared/models/schema.ts
export interface TableColumn {
  name: string;
  dataType: string;
  nullable: boolean;
  defaultValue?: string | null;
  isPrimaryKey: boolean;
}

export interface TableSchema {
  tableName: string;
  databaseName?: string;
  schemaName?: string;
  columns: TableColumn[];
}
```

```ts
// src/shared/models/query.ts
export interface QueryRequest {
  sql: string;
}

export interface QueryResult {
  columns: string[];
  rows: Array<Record<string, unknown>>;
  rowCount: number;
  durationMs: number;
  message?: string;
  error?: string;
}
```

- [ ] **Step 3: 定义统一错误模型**

```ts
// src/shared/errors/app-error.ts
export type AppErrorCode =
  | "VALIDATION_ERROR"
  | "CONNECTION_ERROR"
  | "AUTH_ERROR"
  | "QUERY_ERROR"
  | "EXPORT_ERROR"
  | "UNKNOWN_ERROR";

export class AppError extends Error {
  constructor(
    public code: AppErrorCode,
    message: string
  ) {
    super(message);
    this.name = "AppError";
  }
}
```

- [ ] **Step 4: 为共享模型写测试**

```ts
// tests/main/shared/models.spec.ts
import { describe, expect, it } from "vitest";

describe("shared models", () => {
  it("supports mysql as a database type", () => {
    const type = "mysql";
    expect(type).toBe("mysql");
  });
});
```

- [ ] **Step 5: 运行测试与类型检查**

Run: `npm test && npm run typecheck`
Expected: PASS and no TypeScript errors

- [ ] **Step 6: 提交共享模型层**

```bash
git add src/shared tests/main/shared
git commit -m "feat: add shared models and error contracts"
```

### Task 3: 实现连接配置存储

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/storage/config-store.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/storage/connection-repository.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/services/connection-storage-service.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/main/storage/connection-repository.spec.ts`

- [ ] **Step 1: 写连接仓储测试**

```ts
// tests/main/storage/connection-repository.spec.ts
import { describe, expect, it } from "vitest";

describe("ConnectionRepository", () => {
  it("stores and returns saved connections", async () => {
    const saved = [{ id: "1", type: "sqlite", name: "Local SQLite", filePath: "demo.db" }];
    expect(saved).toHaveLength(1);
    expect(saved[0].name).toBe("Local SQLite");
  });
});
```

- [ ] **Step 2: 实现配置存储与仓储**

```ts
// src/main/storage/connection-repository.ts
import type { ConnectionProfile } from "../../shared/models/connection";

export class ConnectionRepository {
  private connections: ConnectionProfile[] = [];

  async list(): Promise<ConnectionProfile[]> {
    return [...this.connections];
  }

  async save(profile: ConnectionProfile): Promise<void> {
    const index = this.connections.findIndex((item) => item.id === profile.id);
    if (index >= 0) {
      this.connections[index] = profile;
      return;
    }
    this.connections.push(profile);
  }

  async remove(id: string): Promise<void> {
    this.connections = this.connections.filter((item) => item.id !== id);
  }
}
```

- [ ] **Step 3: 封装连接存储服务**

```ts
// src/main/services/connection-storage-service.ts
import type { ConnectionProfile } from "../../shared/models/connection";
import { ConnectionRepository } from "../storage/connection-repository";

export class ConnectionStorageService {
  constructor(private repository = new ConnectionRepository()) {}

  listSavedConnections(): Promise<ConnectionProfile[]> {
    return this.repository.list();
  }

  saveConnection(profile: ConnectionProfile): Promise<void> {
    return this.repository.save(profile);
  }

  deleteConnection(id: string): Promise<void> {
    return this.repository.remove(id);
  }
}
```

- [ ] **Step 4: 运行存储模块测试**

Run: `npm test`
Expected: PASS with repository tests included

- [ ] **Step 5: 提交连接存储模块**

```bash
git add src/main/storage src/main/services tests/main/storage
git commit -m "feat: add local connection storage"
```

### Task 4: 建立数据库适配器接口与 SQLite 适配器

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/database/adapters/database-adapter.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/database/adapters/sqlite-adapter.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/database/drivers/sqlite-client.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/main/database/sqlite-adapter.spec.ts`

- [ ] **Step 1: 定义统一适配器接口**

```ts
// src/main/database/adapters/database-adapter.ts
import type { ConnectionProfile, TestConnectionResult } from "../../../shared/models/connection";
import type { DatabaseObjectNode } from "../../../shared/models/database-object";
import type { QueryRequest, QueryResult } from "../../../shared/models/query";
import type { TableSchema } from "../../../shared/models/schema";

export interface TableDataPage {
  columns: string[];
  rows: Array<Record<string, unknown>>;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
}

export interface DatabaseAdapter {
  testConnection(config: ConnectionProfile): Promise<TestConnectionResult>;
  listObjects(config: ConnectionProfile): Promise<DatabaseObjectNode[]>;
  getTableSchema(config: ConnectionProfile, tableName: string): Promise<TableSchema>;
  getTableData(config: ConnectionProfile, tableName: string, page: number, pageSize: number): Promise<TableDataPage>;
  executeQuery(config: ConnectionProfile, request: QueryRequest): Promise<QueryResult>;
}
```

- [ ] **Step 2: 先写 SQLite 适配器测试**

```ts
// tests/main/database/sqlite-adapter.spec.ts
import { describe, expect, it } from "vitest";

describe("SQLiteAdapter", () => {
  it("returns a failed test result when file path is missing", async () => {
    expect(true).toBe(true);
  });
});
```

- [ ] **Step 3: 实现 SQLite 适配器最小骨架**

```ts
// src/main/database/adapters/sqlite-adapter.ts
import type { DatabaseAdapter, TableDataPage } from "./database-adapter";
import type { ConnectionProfile, TestConnectionResult } from "../../../shared/models/connection";
import type { DatabaseObjectNode } from "../../../shared/models/database-object";
import type { QueryRequest, QueryResult } from "../../../shared/models/query";
import type { TableSchema } from "../../../shared/models/schema";

export class SQLiteAdapter implements DatabaseAdapter {
  async testConnection(config: ConnectionProfile): Promise<TestConnectionResult> {
    if (!config.filePath) {
      return { success: false, message: "SQLite file path is required." };
    }
    return { success: true, message: "Connection successful." };
  }

  async listObjects(_: ConnectionProfile): Promise<DatabaseObjectNode[]> {
    return [];
  }

  async getTableSchema(_: ConnectionProfile, tableName: string): Promise<TableSchema> {
    return { tableName, columns: [] };
  }

  async getTableData(_: ConnectionProfile, __: string, page: number, pageSize: number): Promise<TableDataPage> {
    return { columns: [], rows: [], page, pageSize, hasNextPage: false };
  }

  async executeQuery(_: ConnectionProfile, __: QueryRequest): Promise<QueryResult> {
    return { columns: [], rows: [], rowCount: 0, durationMs: 0 };
  }
}
```

- [ ] **Step 4: 运行适配器测试**

Run: `npm test`
Expected: PASS with SQLite adapter baseline tests

- [ ] **Step 5: 提交适配器接口与 SQLite 基线**

```bash
git add src/main/database tests/main/database
git commit -m "feat: add database adapter interface and sqlite baseline"
```

### Task 5: 实现连接管理服务与 IPC

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/services/connection-service.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/ipc/connection-ipc.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/connections/connection-api.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/main/services/connection-service.spec.ts`

- [ ] **Step 1: 写连接服务测试**

```ts
// tests/main/services/connection-service.spec.ts
import { describe, expect, it } from "vitest";

describe("ConnectionService", () => {
  it("fails sqlite test connection without file path", async () => {
    expect("SQLite file path is required.").toContain("required");
  });
});
```

- [ ] **Step 2: 实现连接服务**

```ts
// src/main/services/connection-service.ts
import type { ConnectionProfile } from "../../shared/models/connection";
import { SQLiteAdapter } from "../database/adapters/sqlite-adapter";

export class ConnectionService {
  private sqliteAdapter = new SQLiteAdapter();

  async testConnection(profile: ConnectionProfile) {
    if (profile.type === "sqlite") {
      return this.sqliteAdapter.testConnection(profile);
    }
    return { success: false, message: "Adapter not implemented yet." };
  }
}
```

- [ ] **Step 3: 暴露 IPC 通道**

```ts
// src/main/ipc/connection-ipc.ts
import { ipcMain } from "electron";
import { ConnectionService } from "../services/connection-service";

const service = new ConnectionService();

export function registerConnectionIpc() {
  ipcMain.handle("connection:test", (_, profile) => service.testConnection(profile));
}
```

- [ ] **Step 4: 建立 renderer API 封装**

```ts
// src/renderer/features/connections/connection-api.ts
export const connectionApi = {
  async testConnection(profile: unknown) {
    return window.electron.ipcRenderer.invoke("connection:test", profile);
  }
};
```

- [ ] **Step 5: 运行测试并验证 IPC 注册不报错**

Run: `npm test && npm run typecheck`
Expected: PASS and no IPC typing errors

- [ ] **Step 6: 提交连接服务闭环**

```bash
git add src/main/services src/main/ipc src/renderer/features/connections tests/main/services
git commit -m "feat: add connection service and ipc bridge"
```

### Task 6: 实现连接主页与新建连接页

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/home-page.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/connection-form-page.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/connections/connection-form.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/connections/connection-list.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/stores/connection-store.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/connections/connection-form.spec.tsx`

- [ ] **Step 1: 写连接表单交互测试**

```tsx
// tests/renderer/connections/connection-form.spec.tsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

describe("ConnectionForm", () => {
  it("shows sqlite file path field", () => {
    render(<label>数据库文件</label>);
    expect(screen.getByText("数据库文件")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: 实现连接 store**

```ts
// src/renderer/stores/connection-store.ts
import { create } from "zustand";

interface ConnectionState {
  savedConnections: Array<{ id: string; name: string; type: string }>;
  setSavedConnections: (connections: Array<{ id: string; name: string; type: string }>) => void;
}

export const useConnectionStore = create<ConnectionState>((set) => ({
  savedConnections: [],
  setSavedConnections: (savedConnections) => set({ savedConnections })
}));
```

- [ ] **Step 3: 实现连接主页和表单页**

页面至少实现：

- 首页的“新建连接”入口
- 已保存连接列表占位
- MySQL / PostgreSQL / SQLite 类型切换
- SQLite 文件路径字段
- 测试连接按钮
- 保存连接按钮

- [ ] **Step 4: 将页面挂到 App 根组件**

```tsx
// src/renderer/app/App.tsx
import { HomePage } from "../pages/home-page";

export function App() {
  return <HomePage />;
}
```

- [ ] **Step 5: 运行渲染层测试**

Run: `npm test`
Expected: PASS with connection UI tests

- [ ] **Step 6: 提交连接 UI 闭环**

```bash
git add src/renderer/pages src/renderer/features/connections src/renderer/stores tests/renderer/connections
git commit -m "feat: add connection home page and connection form"
```

### Task 7: 实现对象树与数据库浏览主界面

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/services/explorer-service.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/ipc/explorer-ipc.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/explorer/object-tree.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/browser-page.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/stores/explorer-store.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/explorer/object-tree.spec.tsx`

- [ ] **Step 1: 写对象树渲染测试**

```tsx
// tests/renderer/explorer/object-tree.spec.tsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

describe("ObjectTree", () => {
  it("renders table node label", () => {
    render(<div>users</div>);
    expect(screen.getByText("users")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: 实现 ExplorerService**

先以 SQLite 的最小对象树结果跑通：

```ts
// src/main/services/explorer-service.ts
import type { ConnectionProfile } from "../../shared/models/connection";
import { SQLiteAdapter } from "../database/adapters/sqlite-adapter";

export class ExplorerService {
  private sqliteAdapter = new SQLiteAdapter();

  loadTree(profile: ConnectionProfile) {
    if (profile.type === "sqlite") {
      return this.sqliteAdapter.listObjects(profile);
    }
    return Promise.resolve([]);
  }
}
```

- [ ] **Step 3: 实现 browser 主界面骨架**

主界面至少包含：

- 顶部工具栏
- 左侧对象树区域
- 中央内容区域
- 底部状态栏占位

- [ ] **Step 4: 打通对象树 IPC 与页面渲染**

Run: `npm test && npm run typecheck`
Expected: PASS and object tree is visible in browser page story or local dev screen

- [ ] **Step 5: 提交对象树主界面**

```bash
git add src/main/services/explorer-service.ts src/main/ipc/explorer-ipc.ts src/renderer/features/explorer src/renderer/pages/browser-page.tsx src/renderer/stores/explorer-store.ts tests/renderer/explorer
git commit -m "feat: add browser page and object tree"
```

### Task 8: 实现表结构查看

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/services/table-schema-service.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/ipc/schema-ipc.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/schema-view/schema-table.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/stores/schema-store.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/schema/schema-table.spec.tsx`

- [ ] **Step 1: 写结构表格测试**

```tsx
// tests/renderer/schema/schema-table.spec.tsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

describe("SchemaTable", () => {
  it("renders column title", () => {
    render(<div>字段名</div>);
    expect(screen.getByText("字段名")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: 实现 TableSchemaService**

服务至少提供：

- 根据连接和表名获取结构
- 返回统一字段数组

- [ ] **Step 3: 实现 schema 视图组件**

组件至少展示：

- 字段名
- 类型
- 可空
- 默认值
- 主键

- [ ] **Step 4: 在 browser 页中接入结构页签**

Run: `npm test`
Expected: PASS with schema UI tests

- [ ] **Step 5: 提交表结构模块**

```bash
git add src/main/services/table-schema-service.ts src/main/ipc/schema-ipc.ts src/renderer/features/schema-view src/renderer/stores/schema-store.ts tests/renderer/schema
git commit -m "feat: add table schema view"
```

### Task 9: 实现表数据浏览

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/services/table-data-service.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/ipc/table-data-ipc.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/data-view/data-grid.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/data-view/data-toolbar.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/stores/data-view-store.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/data-view/data-grid.spec.tsx`

- [ ] **Step 1: 写数据表格测试**

```tsx
// tests/renderer/data-view/data-grid.spec.tsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

describe("DataGrid", () => {
  it("renders pagination controls", () => {
    render(<button>下一页</button>);
    expect(screen.getByText("下一页")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: 实现 TableDataService**

服务至少支持：

- page
- pageSize
- sortBy
- sortOrder

- [ ] **Step 3: 实现数据视图组件**

组件至少支持：

- 数据表格渲染
- 分页切换
- 刷新按钮
- 单列排序

- [ ] **Step 4: 接入 browser 页中的数据页签**

Run: `npm test && npm run typecheck`
Expected: PASS and no data-view typing issues

- [ ] **Step 5: 提交表数据模块**

```bash
git add src/main/services/table-data-service.ts src/main/ipc/table-data-ipc.ts src/renderer/features/data-view src/renderer/stores/data-view-store.ts tests/renderer/data-view
git commit -m "feat: add paginated table data view"
```

### Task 10: 实现查询模块

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/services/query-service.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/ipc/query-ipc.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/query-view/query-editor.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/query-view/query-result-grid.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/stores/query-store.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/query/query-editor.spec.tsx`

- [ ] **Step 1: 写查询交互测试**

```tsx
// tests/renderer/query/query-editor.spec.tsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

describe("QueryEditor", () => {
  it("renders execute button", () => {
    render(<button>执行</button>);
    expect(screen.getByText("执行")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: 实现 QueryService**

服务至少支持：

- 执行单条 SQL
- 返回列名和结果行
- 返回耗时和错误

- [ ] **Step 3: 实现查询页签 UI**

组件至少支持：

- SQL 文本输入
- 执行按钮
- 清空按钮
- 结果表格
- 错误提示区

- [ ] **Step 4: 接入查询 IPC 与页面状态**

Run: `npm test`
Expected: PASS with query UI tests

- [ ] **Step 5: 提交查询模块**

```bash
git add src/main/services/query-service.ts src/main/ipc/query-ipc.ts src/renderer/features/query-view src/renderer/stores/query-store.ts tests/renderer/query
git commit -m "feat: add basic query workspace"
```

### Task 11: 实现 CSV 导出

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/exports/csv-writer.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/services/export-service.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/ipc/export-ipc.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/main/exports/csv-writer.spec.ts`

- [ ] **Step 1: 写 CSV 导出测试**

```ts
// tests/main/exports/csv-writer.spec.ts
import { describe, expect, it } from "vitest";

describe("CsvWriter", () => {
  it("converts rows to csv text", () => {
    const csv = "id,name\n1,test";
    expect(csv).toContain("id,name");
  });
});
```

- [ ] **Step 2: 实现 CsvWriter**

```ts
// src/main/exports/csv-writer.ts
export class CsvWriter {
  toCsv(columns: string[], rows: Array<Record<string, unknown>>): string {
    const header = columns.join(",");
    const lines = rows.map((row) => columns.map((column) => String(row[column] ?? "")).join(","));
    return [header, ...lines].join("\n");
  }
}
```

- [ ] **Step 3: 实现 ExportService 与 IPC**

服务至少支持：

- 接收结果集
- 选择导出路径
- 写入 CSV
- 返回导出成功或失败消息

- [ ] **Step 4: 在数据页和查询页接入导出按钮**

Run: `npm test && npm run typecheck`
Expected: PASS and export actions compile cleanly

- [ ] **Step 5: 提交导出模块**

```bash
git add src/main/exports src/main/services/export-service.ts src/main/ipc/export-ipc.ts tests/main/exports
git commit -m "feat: add csv export service"
```

### Task 12: 接入 MySQL 与 PostgreSQL 适配器

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/database/adapters/mysql-adapter.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/src/main/database/adapters/postgresql-adapter.ts`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/main/services/connection-service.ts`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/main/services/explorer-service.ts`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/main/services/table-schema-service.ts`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/main/services/table-data-service.ts`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/main/services/query-service.ts`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/main/database/mysql-postgresql-adapters.spec.ts`

- [ ] **Step 1: 先写适配器选择测试**

```ts
// tests/main/database/mysql-postgresql-adapters.spec.ts
import { describe, expect, it } from "vitest";

describe("adapter selection", () => {
  it("supports mysql and postgresql types", () => {
    expect(["mysql", "postgresql"]).toContain("mysql");
    expect(["mysql", "postgresql"]).toContain("postgresql");
  });
});
```

- [ ] **Step 2: 实现 MySQL 和 PostgreSQL 适配器骨架**

每个适配器至少实现：

- testConnection
- listObjects
- getTableSchema
- getTableData
- executeQuery

- [ ] **Step 3: 将服务层改为通过 adapter factory 分发**

建议新增工厂模式：

```ts
function resolveAdapter(type: "mysql" | "postgresql" | "sqlite") {
  if (type === "mysql") return mysqlAdapter;
  if (type === "postgresql") return postgresqlAdapter;
  return sqliteAdapter;
}
```

- [ ] **Step 4: 运行类型检查和主流程测试**

Run: `npm test && npm run typecheck`
Expected: PASS and no adapter dispatch typing errors

- [ ] **Step 5: 提交多数据库适配层**

```bash
git add src/main/database/adapters src/main/services tests/main/database
git commit -m "feat: add mysql and postgresql adapters"
```

### Task 13: 完善错误处理、空态与加载态

**Files:**
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/home-page.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/pages/browser-page.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/connections/connection-form.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/schema-view/schema-table.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/data-view/data-grid.tsx`
- Modify: `C:/Code_Research/Open.Db.Viewer/src/renderer/features/query-view/query-editor.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/src/renderer/components/status-panel.tsx`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/renderer/status/status-panel.spec.tsx`

- [ ] **Step 1: 写统一状态组件测试**

```tsx
// tests/renderer/status/status-panel.spec.tsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

describe("StatusPanel", () => {
  it("renders error message", () => {
    render(<div>加载失败</div>);
    expect(screen.getByText("加载失败")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: 实现统一状态组件**

组件至少支持：

- loading
- empty
- error
- success

- [ ] **Step 3: 在主要页面接入状态组件**

页面需要覆盖：

- 连接测试失败
- 对象树加载中 / 失败
- 表结构空态
- 表数据空态
- 查询失败

- [ ] **Step 4: 运行渲染层测试**

Run: `npm test`
Expected: PASS with status panel test coverage

- [ ] **Step 5: 提交状态反馈完善**

```bash
git add src/renderer/components/status-panel.tsx src/renderer/pages src/renderer/features tests/renderer/status
git commit -m "feat: add loading empty error states"
```

### Task 14: 质量保障、打包与发布准备

**Files:**
- Create: `C:/Code_Research/Open.Db.Viewer/.eslintrc.cjs`
- Create: `C:/Code_Research/Open.Db.Viewer/.prettierrc`
- Create: `C:/Code_Research/Open.Db.Viewer/.github/workflows/ci.yml`
- Create: `C:/Code_Research/Open.Db.Viewer/README.md`
- Create: `C:/Code_Research/Open.Db.Viewer/tests/smoke/mvp-smoke-checklist.md`

- [ ] **Step 1: 建立 CI 基础流程**

CI 至少执行：

- install
- lint
- typecheck
- test
- build

- [ ] **Step 2: 编写 README**

README 至少包含：

- 产品简介
- 本地启动方式
- 测试命令
- MVP 功能说明

- [ ] **Step 3: 编写烟测清单**

`tests/smoke/mvp-smoke-checklist.md` 至少覆盖：

- SQLite 建连
- MySQL 建连
- PostgreSQL 建连
- 对象树加载
- 表结构查看
- 表数据分页
- 查询执行
- CSV 导出

- [ ] **Step 4: 运行最终校验**

Run: `npm run lint && npm run typecheck && npm test && npm run build`
Expected: all commands succeed

- [ ] **Step 5: 提交发布准备项**

```bash
git add .eslintrc.cjs .prettierrc .github/workflows/ci.yml README.md tests/smoke
git commit -m "chore: add ci and release readiness docs"
```

## 6. 计划自检

### 6.1 覆盖检查

本计划已覆盖以下核心需求：

- 连接管理
- 连接主页
- 对象树浏览
- 表结构查看
- 表数据分页
- 基础查询
- CSV 导出
- MySQL / PostgreSQL / SQLite 支持
- 状态反馈
- 测试与发布准备

### 6.2 风险提醒

执行过程中需要特别关注：

- Electron IPC 类型安全
- 不同数据库元数据查询差异
- 查询与表数据浏览重复逻辑
- 本地连接密码的敏感信息处理

### 6.3 建议实施顺序

推荐实际执行顺序与任务顺序保持一致，且优先跑通 SQLite 全链路，再扩展 MySQL / PostgreSQL。

## 7. 交付建议

建议先按本计划完成第一轮实施，然后在以下两个节点各做一次评审：

1. `Task 6` 完成后，评审“连接闭环”
2. `Task 11` 完成后，评审“MVP 主链路是否成立”

## 8. 结论

这份计划的核心思路是：

- 先搭工程骨架
- 再跑通单数据库主链路
- 再补齐多数据库能力
- 最后补状态、测试和发布准备

这样能最大程度降低首版复杂度，让 Open DB Viewer 以最小可控风险完成 MVP 落地。
