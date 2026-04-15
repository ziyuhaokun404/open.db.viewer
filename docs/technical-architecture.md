# Open DB Viewer 技术架构与模块设计

## 1. 文档信息

- 产品名称：Open DB Viewer
- 文档类型：技术架构与模块设计
- 对应文档：[product-document.md](C:/Code_Research/Open.Db.Viewer/docs/product-document.md)
- 对应文档：[mvp-feature-list.md](C:/Code_Research/Open.Db.Viewer/docs/mvp-feature-list.md)
- 对应文档：[prototype-structure.md](C:/Code_Research/Open.Db.Viewer/docs/prototype-structure.md)
- 目标版本：V1.0
- 文档日期：2026-04-15

## 2. 文档目标

本文件用于定义 Open DB Viewer 首版的技术实现方向与模块边界，重点解决以下问题：

- 首版采用什么样的总体架构
- 前端、应用层、数据库访问层如何分层
- 连接管理、对象树、表结构、表数据、查询、导出分别由哪些模块负责
- 如何在支持 MySQL、PostgreSQL、SQLite 的同时保持架构清晰
- 哪些能力属于 V1 必做，哪些能力应为后续版本预留扩展位

本文件不是最终代码实现方案，但应足够指导后续模块拆分、任务分解和代码目录设计。

## 3. 技术目标

### 3.1 核心目标

首版技术架构应优先满足：

- 易于快速开发 MVP
- 支持桌面应用交付
- 保持多数据库支持的最小统一抽象
- 保持 UI 层与数据库驱动层解耦
- 方便后续增加查询历史、多标签页、收藏对象等功能

### 3.2 非目标

首版技术架构暂不追求：

- 插件化平台级架构
- 分布式或远程协同能力
- 企业级权限体系
- 大规模数据库种类兼容框架
- 高复杂度 SQL IDE 引擎

## 4. 总体架构建议

### 4.1 架构风格

建议采用“桌面应用 + 本地应用服务层 + 数据库适配层”的分层架构。

整体分为四层：

1. 表现层（UI Layer）
2. 应用服务层（Application Layer）
3. 领域与状态层（Domain / State Layer）
4. 基础设施层（Infrastructure Layer）

### 4.2 高层结构图

```text
+--------------------------------------------------------------+
| UI Layer                                                     |
| 页面、组件、交互状态、表格、表单、通知                       |
+--------------------------------------------------------------+
| Application Layer                                            |
| 连接服务、对象浏览服务、结构服务、数据服务、查询服务、导出服务 |
+--------------------------------------------------------------+
| Domain / State Layer                                         |
| 连接模型、数据库对象模型、查询模型、结果模型、状态模型         |
+--------------------------------------------------------------+
| Infrastructure Layer                                         |
| 驱动适配器、配置存储、文件系统、日志、错误映射               |
+--------------------------------------------------------------+
```

### 4.3 分层原则

- UI 层不直接访问数据库驱动
- 数据库类型差异收敛在适配层，不扩散到页面层
- 所有主流程通过应用服务层暴露统一接口
- 状态模型尽量围绕用户任务组织，而不是围绕底层技术组织

## 5. 技术选型建议

由于当前项目尚未进入具体代码实现阶段，这里给出首版推荐技术方向，后续可再细化为实施方案。

### 5.1 客户端框架

建议方向：

- 桌面壳：Electron 或 Tauri
- 前端：React
- 语言：TypeScript

### 5.2 推荐理由

Electron 路线优点：

- 生态成熟
- 数据库驱动、文件系统、桌面能力接入更直接
- 对 MVP 开发速度友好

Tauri 路线优点：

- 安装包更轻
- 性能和资源占用更好

首版建议优先考虑：

- 如果目标是尽快完成 MVP，优先 `Electron + React + TypeScript`
- 如果团队有较强 Rust / Tauri 经验，也可选择 Tauri，但不建议为了“更先进”而增加首版复杂度

### 5.3 本文默认假设

为便于后续模块设计，本文默认首版技术基线为：

- Electron
- React
- TypeScript

这个假设不会影响产品结构文档的复用，但会影响代码目录和职责划分示例。

## 6. 分层设计

## 6.1 UI Layer

职责：

- 渲染页面与组件
- 管理视图状态
- 响应用户交互
- 展示加载、空态、错误、成功反馈

不负责：

- 直接连接数据库
- 直接拼接数据库方言逻辑
- 直接管理持久化细节

建议模块：

- 页面容器
- 通用组件
- 表单组件
- 表格组件
- 全局通知与状态提示组件

## 6.2 Application Layer

职责：

- 承接用户动作
- 编排多模块协作
- 调用数据库适配器和本地存储
- 输出页面所需结构化结果

建议服务：

- ConnectionService
- ConnectionStorageService
- ExplorerService
- TableSchemaService
- TableDataService
- QueryService
- ExportService

## 6.3 Domain / State Layer

职责：

- 定义业务对象模型
- 定义统一状态结构
- 管理连接、对象树、查询结果等业务状态

建议模型：

- ConnectionProfile
- ConnectionSession
- DatabaseObjectNode
- TableSchema
- TableColumn
- TableDataPage
- QueryRequest
- QueryResult
- AppError

## 6.4 Infrastructure Layer

职责：

- 实际连接数据库
- 读取元数据
- 执行查询
- 访问本地文件系统
- 处理配置持久化
- 错误映射与日志记录

建议模块：

- MySQLAdapter
- PostgreSQLAdapter
- SQLiteAdapter
- ConnectionRepository
- ConfigStore
- CsvWriter
- Logger

## 7. 核心模块设计

## 7.1 连接管理模块

### 模块目标

负责连接配置的创建、编辑、删除、测试、持久化和打开。

### 模块拆分

- `ConnectionFormModel`
- `ConnectionValidator`
- `ConnectionService`
- `ConnectionStorageService`
- `ConnectionRepository`

### 关键职责

`ConnectionService`

- 测试连接参数是否合法
- 建立短连接用于测试
- 打开正式连接会话

`ConnectionStorageService`

- 保存连接配置
- 读取连接列表
- 删除连接配置

`ConnectionRepository`

- 处理配置文件或本地存储的读写

### 输入输出建议

输入：

- 数据库类型
- 连接名称
- 主机 / 端口 / 用户名 / 密码 / 数据库名
- SQLite 文件路径

输出：

- 连接测试结果
- 保存结果
- 已保存连接列表
- 连接会话对象

## 7.2 数据库对象浏览模块

### 模块目标

负责生成左侧对象树，并向 UI 提供统一的节点结构。

### 模块拆分

- `ExplorerService`
- `DatabaseObjectTreeBuilder`
- `DatabaseObjectMapper`

### 关键职责

`ExplorerService`

- 获取数据库列表
- 获取 Schema 列表
- 获取表和视图列表
- 输出统一节点结构

`DatabaseObjectTreeBuilder`

- 将不同数据库返回的数据组织成树形结构

`DatabaseObjectMapper`

- 把 MySQL / PostgreSQL / SQLite 的对象元信息映射为统一模型

### 设计要点

- PostgreSQL 支持 `database -> schema -> table`
- MySQL 可以收敛为 `database -> table`
- SQLite 可以收敛为 `database(file) -> table`

UI 层只认统一对象树，不感知数据库具体差异。

## 7.3 表结构查看模块

### 模块目标

负责读取并展示表字段结构信息。

### 模块拆分

- `TableSchemaService`
- `SchemaAdapterFacade`
- `ColumnMetadataMapper`

### 关键职责

- 根据当前选中的对象读取表结构
- 统一输出字段列表
- 统一映射主键、默认值、可空等属性

### 输出模型建议

`TableSchema`

- tableName
- databaseName
- schemaName
- columns

`TableColumn`

- name
- dataType
- nullable
- defaultValue
- isPrimaryKey

## 7.4 表数据浏览模块

### 模块目标

负责读取表数据、做分页、排序和基础过滤。

### 模块拆分

- `TableDataService`
- `PaginationModel`
- `SortModel`
- `FilterModel`

### 关键职责

- 按页读取数据
- 控制默认返回条数
- 支持单列排序
- 支持关键字过滤

### 设计要点

- 首版应由服务层统一控制分页策略
- 默认限制返回条数，避免 UI 卡顿
- 不建议 UI 自行拼接复杂 SQL，应由服务层统一生成安全查询语句

### 输出模型建议

`TableDataPage`

- columns
- rows
- page
- pageSize
- hasNextPage
- totalKnown

## 7.5 查询模块

### 模块目标

负责执行用户输入的基础查询，并返回结构化结果和错误信息。

### 模块拆分

- `QueryService`
- `QueryExecutor`
- `QueryResultMapper`
- `QueryGuard`

### 关键职责

- 接收 SQL 文本
- 执行查询
- 返回结果表格结构
- 返回执行耗时、行数和错误

### 首版约束建议

- 首版只支持单条查询
- 首版优先支持 `SELECT`
- 如果要提升安全性，可在首版默认拦截危险写操作，或仅在查询页开放读取型语句

### 输出模型建议

`QueryResult`

- columns
- rows
- rowCount
- durationMs
- message
- error

## 7.6 导出模块

### 模块目标

负责将当前结果集导出为 CSV 文件。

### 模块拆分

- `ExportService`
- `CsvWriter`
- `ExportFileResolver`

### 关键职责

- 接收结果集
- 生成 CSV
- 写入指定位置
- 向 UI 返回成功 / 失败状态

### 设计要点

- 导出模块不直接依赖页面组件
- 导出输入可以来自表数据浏览，也可以来自查询结果
- 统一走同一套结果模型，减少重复代码

## 8. 数据库适配层设计

## 8.1 设计目标

为 MySQL、PostgreSQL、SQLite 提供统一访问接口，同时保留必要的数据库差异处理能力。

## 8.2 推荐接口

建议定义统一适配器接口：

```ts
interface DatabaseAdapter {
  testConnection(config: ConnectionProfile): Promise<TestConnectionResult>;
  connect(config: ConnectionProfile): Promise<ConnectionSession>;
  listDatabases(session: ConnectionSession): Promise<DatabaseInfo[]>;
  listSchemas(session: ConnectionSession, database?: string): Promise<SchemaInfo[]>;
  listTables(session: ConnectionSession, target: ObjectTarget): Promise<TableInfo[]>;
  getTableSchema(session: ConnectionSession, target: TableTarget): Promise<TableSchema>;
  getTableData(session: ConnectionSession, request: TableDataRequest): Promise<TableDataPage>;
  executeQuery(session: ConnectionSession, request: QueryRequest): Promise<QueryResult>;
}
```

## 8.3 适配器实现建议

- `MySQLAdapter`
- `PostgreSQLAdapter`
- `SQLiteAdapter`

### 每个适配器负责

- 驱动初始化
- 连接生命周期管理
- 元数据查询
- 数据读取
- 差异 SQL 处理
- 错误转换

### 不负责

- 页面状态管理
- UI 交互逻辑
- 连接配置持久化

## 8.4 差异收敛原则

数据库差异主要收敛在以下几类：

- 连接参数格式不同
- 元数据读取方式不同
- 分页和排序 SQL 细节不同
- Schema 概念是否存在

收敛策略：

- UI 永远使用统一模型
- 应用层使用统一服务接口
- 只有适配层感知差异 SQL 和驱动特性

## 9. 状态管理设计

## 9.1 状态分层建议

建议将状态拆成三类：

- 持久状态
- 会话状态
- 页面状态

### 持久状态

- 已保存连接
- 最近连接
- 用户设置

### 会话状态

- 当前活动连接
- 当前对象树
- 当前打开的表
- 当前查询上下文

### 页面状态

- loading
- empty
- error
- success
- 当前分页
- 当前排序

## 9.2 推荐状态树

```text
appState
  connections
    savedList
    recentList
    activeConnectionId
  explorer
    tree
    selectedNode
    loading
    error
  schemaView
    currentSchema
    loading
    error
  dataView
    currentTableData
    pagination
    sorting
    loading
    error
  queryView
    sqlText
    queryResult
    executing
    error
  ui
    notifications
    dialogs
    globalStatus
```

## 9.3 设计原则

- 连接状态与页面状态分离
- 查询状态与表数据状态分离
- 每个视图有独立 loading / error，避免互相污染

## 10. 数据流设计

## 10.1 新建连接流程

```text
用户填写连接表单
  -> UI 校验必填项
  -> ConnectionService.testConnection()
  -> Adapter.testConnection()
  -> 返回测试结果
  -> ConnectionStorageService.save()
  -> ConnectionRepository.persist()
  -> UI 跳转到数据库浏览页
```

## 10.2 对象树加载流程

```text
用户打开连接
  -> ExplorerService.loadTree()
  -> Adapter.listDatabases()
  -> Adapter.listSchemas()/listTables()
  -> DatabaseObjectTreeBuilder.build()
  -> UI 渲染对象树
```

## 10.3 表结构加载流程

```text
用户点击表
  -> TableSchemaService.getSchema()
  -> Adapter.getTableSchema()
  -> ColumnMetadataMapper.map()
  -> UI 渲染结构表格
```

## 10.4 表数据加载流程

```text
用户点击数据页签
  -> TableDataService.getPage()
  -> Adapter.getTableData()
  -> 返回分页结果
  -> UI 渲染数据表格
```

## 10.5 查询执行流程

```text
用户输入 SQL
  -> QueryService.execute()
  -> QueryGuard.check()
  -> Adapter.executeQuery()
  -> QueryResultMapper.map()
  -> UI 渲染结果和状态
```

## 10.6 导出流程

```text
用户点击导出
  -> ExportService.exportCsv()
  -> ExportFileResolver.pickPath()
  -> CsvWriter.write()
  -> UI 提示导出成功或失败
```

## 11. 错误处理设计

## 11.1 错误分类

建议统一错误模型，至少包含以下类别：

- ValidationError
- ConnectionError
- AuthenticationError
- PermissionError
- QueryError
- TimeoutError
- ExportError
- UnknownError

## 11.2 错误处理原则

- 底层错误先在适配层归一化
- 应用层只处理统一错误类型
- UI 层只展示用户可理解的信息

## 11.3 错误展示原则

- 对用户展示简洁描述
- 对日志记录更完整技术信息
- 错误要能定位到动作上下文，如“连接测试失败”或“加载表结构失败”

## 12. 配置与本地存储设计

## 12.1 存储内容

首版本地存储建议包括：

- 已保存连接
- 最近连接
- 用户基础设置

## 12.2 存储形式

建议使用本地 JSON / SQLite / Electron Store 一类稳定方案。

首版优先建议：

- 连接配置：本地配置存储
- 最近连接：本地轻量存储
- 设置项：本地轻量存储

## 12.3 安全建议

- 密码字段避免明文裸露在 UI 日志中
- 如果时间允许，连接密码应使用系统安全存储或做本地加密
- 至少要避免错误日志把敏感凭据直接输出

## 13. 目录结构建议

如果采用 `Electron + React + TypeScript`，建议参考如下目录结构：

```text
src/
  main/
    app/
    ipc/
    storage/
    database/
      adapters/
      drivers/
      mappers/
    services/
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
    constants/
    errors/
```

## 14. 模块边界建议

### 14.1 shared

放置：

- 类型定义
- 通用模型
- 错误模型
- 常量

### 14.2 main

放置：

- Electron 主进程逻辑
- 数据库适配器
- 文件系统与配置存储
- 导出逻辑

### 14.3 renderer

放置：

- 页面
- 组件
- 前端状态管理
- 交互逻辑

## 15. IPC 通信建议

如果使用 Electron，建议通过 IPC 将 UI 与主进程隔离。

建议通道示例：

- `connection:test`
- `connection:save`
- `connection:list`
- `explorer:load`
- `schema:get`
- `table-data:get`
- `query:execute`
- `export:csv`

设计原则：

- IPC 请求参数尽量结构化
- 返回统一结果体
- 不把驱动对象直接暴露给 renderer

## 16. 性能设计建议

### 16.1 首版重点

- 对象树按需加载
- 表数据默认分页
- 查询结果限制默认返回条数
- 避免在 renderer 中做大规模数据计算

### 16.2 优化方向

- 懒加载对象树
- 结果表格虚拟滚动
- 查询执行与 UI 渲染解耦

## 17. 可扩展性设计

虽然首版不做插件化，但架构上应预留后续扩展空间。

### 17.1 易扩展点

- 新增数据库类型时，只需新增 Adapter
- 新增结果导出格式时，只需扩展 ExportService
- 新增查询历史时，可挂接 QueryService
- 新增多标签页时，主要扩展 UI 状态层

### 17.2 未来可扩展模块

- QueryHistoryService
- FavoriteObjectService
- TabSessionService
- ReadonlyGuardService

## 18. 首版技术风险

### 18.1 多数据库差异复杂度

风险：

- 元数据获取方式差异较大

应对：

- 坚持适配层隔离
- 首版统一最小对象模型

### 18.2 查询与数据浏览边界混乱

风险：

- 容易把“表数据浏览”和“自由查询”做成两套重复逻辑

应对：

- 统一结果模型
- 统一表格渲染能力

### 18.3 状态管理耦合

风险：

- 连接、对象树、查询、表数据状态容易互相污染

应对：

- 明确分域状态
- 每个模块单独管理 loading / error / data

### 18.4 安全与存储细节被忽略

风险：

- 连接密码和错误日志处理不当

应对：

- 首版就定义敏感字段处理规则
- 错误日志脱敏

## 19. 首版实施建议

从实现顺序看，建议按模块逐步建立：

1. shared 模型与错误定义
2. 连接配置存储
3. 数据库适配器接口与 SQLite 适配器
4. 连接管理服务
5. 对象树服务
6. 表结构服务
7. 表数据服务
8. 查询服务
9. 导出服务
10. UI 页面接入
11. MySQL 与 PostgreSQL 适配器补齐

之所以建议先做 SQLite，是因为：

- 本地验证更简单
- 调试成本低
- 更适合作为第一条跑通主链路的数据库类型

## 20. 结论

Open DB Viewer 首版技术架构的关键，不是技术栈是否“最先进”，而是边界是否清晰。

一个合适的首版架构应该做到：

- UI 不直接碰数据库驱动
- 多数据库差异被收敛在适配层
- 核心能力围绕服务层展开
- 查询结果和表数据结果尽量统一建模
- 本地存储、导出、错误处理都具备清晰归属

如果按本文的分层和模块方式推进，首版可以在不做过度设计的前提下，建立一个足够稳定、可扩展、便于持续演进的数据库浏览应用基础。
