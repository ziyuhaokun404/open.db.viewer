# Open DB Viewer WPF 重写设计

## 1. 文档信息

- 产品名称：Open DB Viewer
- 文档类型：WPF 重写设计
- 当前目标版本：V1 MVP
- 文档日期：2026-04-17
- 技术方向：WPF + WPF UI + SQLite-only

## 2. 目标与范围

本次重写目标不是复刻 Electron 版本的全部技术细节，而是基于现有产品边界，用 WPF 重建一个可稳定使用的 SQLite 桌面浏览工具。

V1 MVP 只聚焦 SQLite，本阶段必须跑通以下闭环：

1. 打开 SQLite 数据库文件
2. 保存并复用常用数据库入口
3. 浏览数据库对象树
4. 查看表结构
5. 查看表数据
6. 执行基础 SQL 查询
7. 导出当前结果为 CSV

本阶段明确不做：

- MySQL / PostgreSQL 可用支持
- 插件系统
- 多标签数据库工作区
- 高级 SQL IDE 能力
- 可停靠面板和复杂布局系统

## 3. 产品形态结论

本次 WPF 重写采用以下产品决策：

- 数据库范围：SQLite-only
- 入口模式：单文件快速打开优先
- UI 风格：使用 WPF UI 提供 Fluent 桌面感

这意味着首屏应以“打开 SQLite 文件”为主动作，而不是先进入连接配置中心。保存连接入口保留，但作为成功打开数据库后的次级能力存在。

## 4. 总体架构

建议采用标准分层 MVVM 路线，在保持首版交付速度的前提下，避免单项目快速膨胀。

推荐项目结构：

- `OpenDbViewer.Wpf`
  - WPF UI、页面、控件、资源、主题、ViewModel、导航
- `OpenDbViewer.Application`
  - 用例编排与应用服务
- `OpenDbViewer.Domain`
  - 领域模型与结果模型
- `OpenDbViewer.Infrastructure.Sqlite`
  - SQLite 访问、CSV 导出、本地配置存储

设计原则：

- View 不直接写数据库访问逻辑
- ViewModel 不直接持有 `SqliteConnection`
- 所有业务能力都通过应用服务层暴露
- 虽然首版只支持 SQLite，但命名与边界允许未来扩展，不提前实现多数据库抽象

## 5. UI 与信息架构

### 5.1 首页

首页只承载进入数据库的核心动作，不做重型管理中心。

首页内容建议按优先级排列：

1. `打开 SQLite 文件`
2. 最近使用
3. 已保存入口
4. 次级操作，如移除、重命名、在资源管理器中显示

目标是让用户在首次打开应用时，能立即理解下一步动作。

### 5.2 主工作区

数据库工作区采用稳定桌面工具布局：

- 顶部：当前数据库信息、返回首页、刷新、导出等命令
- 左侧：对象树
- 右侧：主内容区
- 主内容区标签页：`结构 / 数据 / 查询`
- 底部：状态栏

工作流设计如下：

- 用户在对象树选中表
- 主内容区默认进入“结构”
- 可切换到“数据”
- 需要灵活处理时进入“查询”
- 当前页结果可导出为 CSV

### 5.3 WPF UI 控件建议

- Shell: `NavigationView` 或统一 Shell + Page 承载
- 顶部命令区：`TitleBar` + 命令按钮
- 左侧对象区：`TreeView`
- 结构页：`DataGrid`
- 数据页：`DataGrid`
- 查询页：上部 SQL 编辑区，下部结果 `DataGrid`
- 反馈：`InfoBar` / `Snackbar`

首版不引入复杂 docking 系统，优先保证稳定、清晰、易上手。

## 6. 核心模块与 ViewModel 拆分

### 6.1 ViewModel 结构

- `ShellViewModel`
  - 全局页面状态、当前数据库、主题、全局通知
- `HomeViewModel`
  - 打开文件、最近使用、已保存入口管理
- `DatabaseWorkspaceViewModel`
  - 工作区协调、当前表、当前标签页、刷新
- `ObjectExplorerViewModel`
  - 对象树加载、筛选、选中、刷新
- `SchemaViewModel`
  - 表结构展示
- `DataViewModel`
  - 表数据分页、排序、刷新、导出
- `QueryViewModel`
  - SQL 文本、执行、错误展示、结果导出

### 6.2 应用服务

- `DatabaseEntryService`
  - 打开 SQLite 文件、校验文件、维护最近使用与已保存入口
- `ExplorerService`
  - 读取数据库对象树
- `SchemaService`
  - 读取表结构
- `TableDataService`
  - 分页读取表数据、排序、刷新
- `QueryService`
  - 执行 SQL
- `ExportService`
  - 导出 CSV

### 6.3 Domain 模型

- `DatabaseEntry`
  - `Id, Name, FilePath, LastOpenedAt, IsPinned`
- `DatabaseObjectNode`
  - `Name, Kind, Children`
- `TableColumnInfo`
  - `Name, DataType, IsNullable, DefaultValue, IsPrimaryKey`
- `TableSchema`
  - `TableName, Columns`
- `TablePageResult`
  - `Columns, Rows, PageNumber, PageSize, HasNextPage, SortColumn, SortDirection`
- `QueryExecutionRequest`
  - `Sql`
- `QueryExecutionResult`
  - `Columns, Rows, AffectedRows, Duration, Message`
- `OperationResult`
  - `IsSuccess, Message, ErrorCode`

说明：

- “表数据页”和“查询结果页”都可以复用通用表格结果结构
- 但在上层语义上仍保留为两个明确能力，避免做成万能大模型

## 7. SQLite 基础设施与技术选型

推荐技术选型：

- UI：WPF + WPF UI
- 架构：MVVM
- SQLite 驱动：`Microsoft.Data.Sqlite`
- DI：`Microsoft.Extensions.DependencyInjection`
- Logging：`Microsoft.Extensions.Logging`
- 配置存储：本地 JSON 文件
- CSV 导出：自定义轻量 `CsvWriter`

不推荐首版引入 ORM。该产品核心是浏览和执行 SQL，不是实体映射。

### 7.1 基础设施职责拆分

- `SqliteConnectionFactory`
  - 创建连接、统一 PRAGMA、连接生命周期
- `SqliteDatabaseInspector`
  - 读取表列表、对象树、表结构
- `SqliteTableDataReader`
  - 表数据分页、排序、刷新
- `SqliteQueryExecutor`
  - 执行自由 SQL
- `CsvExportWriter`
  - 导出 CSV
- `DatabaseEntryRepository`
  - 最近使用与已保存入口的持久化

### 7.2 对象树范围

首版对象树以“表”为主，视图可作为可选补充项，但不作为核心浏览重点。

建议层级：

- Database
  - Tables
    - `users`
    - `orders`
    - `products`

首版不做重型对象宇宙，不把索引、触发器、视图浏览扩展为复杂系统。

### 7.3 表结构读取

结构页需稳定呈现：

- 字段名称
- 类型
- 是否主键
- 是否可为空
- 默认值

应用层返回的结构模型必须面向 UI 可读性，而不是直接暴露底层原始字段。

### 7.4 表数据策略

建议首版采用：

- 固定页大小，如 `100`
- `LIMIT + OFFSET` 分页
- 单列排序
- 刷新时重查当前页

产品语义上应明确：这是“有限结果浏览”，不是一次性载入整表。

### 7.5 SQL 执行策略

查询页首版只支持执行当前文本，不做 IDE 化高级能力。

结果分为两类：

- `SELECT`：返回列与行
- 非查询类 SQL：返回影响行数与执行消息

必须覆盖：

- 语法错误
- 数据库锁定
- 文件不可访问
- 执行异常

### 7.6 CSV 导出策略

导出应满足：

- 输出列头
- 对包含逗号、双引号、换行的值做正确转义
- 对 `null` 有一致输出策略
- 使用 `UTF-8 with BOM`，提升 Excel 兼容性

导出输入可统一为：

- 列名集合
- 行值集合

这样表数据页和查询页可以共用一套导出逻辑。

### 7.7 配置存储

建议将以下两类数据分开存储：

- 最近使用：自动维护，按最近打开时间排序
- 已保存入口：用户主动保存，允许重命名和删除

存储位置建议为本地应用数据目录下的应用专属配置目录，格式使用 JSON。

## 8. 交互流程设计

### 8.1 首次打开数据库

1. 用户启动应用，进入首页
2. 点击 `打开 SQLite 文件`
3. 选择数据库文件
4. 应用做基础校验
5. 成功后直接进入工作区
6. 自动加入最近使用
7. 提供“保存为常用入口”

原则：保存入口不是门槛，而是成功打开后的顺手动作。

### 8.2 从首页打开最近项或已保存入口

1. 用户点击最近使用或已保存入口
2. 应用直接打开数据库
3. 若路径失效，允许：
   - 从列表移除
   - 重新定位文件
   - 取消

### 8.3 浏览表

1. 工作区左侧加载对象树
2. 用户选中某张表
3. 右侧默认进入“结构”
4. 后台预取第一页数据

预取仅限第一页，避免过度加载。

### 8.4 查看数据

数据页支持：

- 当前页结果展示
- 上一页 / 下一页
- 单列排序
- 刷新
- 导出当前结果

### 8.5 执行 SQL

查询页支持：

- 输入 SQL
- 点击执行
- 展示结果表格或状态消息
- 错误内联显示
- 导出当前查询结果

首版不做多语句分段执行、查询历史和高级编辑能力。

## 9. 异常处理与状态反馈

建议将异常分为四类：

1. 文件级错误
   - 文件不存在、无权限、损坏、不是合法 SQLite
2. 元数据读取错误
   - 对象树或结构读取失败
3. 查询执行错误
   - SQL 语法错误、锁冲突、执行失败
4. 导出错误
   - 文件占用、路径无权限、写入失败

呈现原则：

- 局部错误局部显示
- 查询错误不打断整个工作区
- 阻断式错误只用于无法继续的关键场景

建议状态类型：

- 页面空态
- 局部加载态
- 内联错误
- 全局轻提示
- 阻断式对话框

## 10. MVP 验收标准

V1 MVP 完成标准：

1. 用户能在 10 秒内理解如何打开 SQLite 文件
2. 合法 SQLite 文件能稳定进入工作区
3. 对象树能显示表列表并支持刷新
4. 结构页能稳定显示字段信息
5. 数据页能稳定展示第一页并支持翻页、排序、刷新
6. 查询页能执行简单 SQL
7. 非查询 SQL 能返回清晰状态
8. CSV 导出能生成 Excel 可直接打开的合法文件
9. 常见失败场景都有可理解提示
10. 用户无需额外教程即可完成主闭环

## 11. 实施边界与后续扩展口

虽然首版不做以下能力，但设计上要避免把未来路堵死：

- 查询历史
- 多数据库标签页
- 视图 / 索引 / 触发器浏览
- 更强的结果复制能力
- 更好的 SQL 编辑体验

原则是：允许未来扩展，但不为未来过度付款。

## 12. 结论

本次 WPF 重写应以“SQLite-only 的可用版”为唯一优先级，围绕“快速打开、清晰浏览、稳定查询、低成本导出”建立 MVP。

最合适的实施路线不是单项目快速堆叠，也不是提前进入插件化和多数据库平台，而是采用分层 MVVM 架构，以 WPF UI 提供 Fluent 桌面体验，用最克制的技术组合把主闭环做稳。
