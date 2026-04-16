# Open DB Viewer

Open DB Viewer 是一个偏轻量、易上手的 SQLite 数据库浏览工具，面向需要快速查看结构、跑查询、导出结果的开发者和数据使用者。产品定位参考 Navicat、DBeaver、DataGrip，但当前阶段更强调本地可用、学习成本低、主流程干净。

## 当前阶段范围

- 当前短期目标聚焦 SQLite：连接管理、对象树、表结构、表数据、SQL 查询、CSV 导出
- MySQL / PostgreSQL 适配器保留在架构层，但不作为当前产品 UI 的活跃范围
- 当前重点是把 SQLite 浏览体验和桌面端 UI 质量打磨到可持续迭代的水平

## 本地启动

```bash
npm install
npm run dev
```

## 工程命令

```bash
npm run lint
npm run typecheck
npm test
npm run build
```

## MVP 功能

- 新建并保存数据库连接
- 打开已保存连接进入浏览页
- 查看对象树
- 查看表结构
- 查看表数据分页结果
- 执行基础 SQL 查询
- 将表数据或查询结果导出为 CSV

## 文档目录

- [产品文档](./docs/product-document.md)
- [MVP 功能清单](./docs/mvp-feature-list.md)
- [产品原型结构稿](./docs/prototype-structure.md)
- [技术架构与模块设计](./docs/technical-architecture.md)
- [研发任务拆解 / 开发计划](./docs/development-plan.md)

## 当前技术栈

- Electron
- React
- TypeScript
- Vite / electron-vite
- Zustand
- Vitest + Testing Library
