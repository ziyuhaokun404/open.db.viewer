import type { TableDataPage } from "../../../main/database/adapters/database-adapter";
import { StatusPanel } from "../../components/status-panel";

export function DataGrid({
  data,
  onExport
}: {
  data: TableDataPage | null;
  onExport: () => void;
}) {
  if (!data || !Array.isArray(data.columns) || !Array.isArray(data.rows)) {
    return (
      <StatusPanel variant="empty" title="未加载表数据" message="请选择左侧对象树中的表以查看数据。" />
    );
  }

  if (data.rows.length === 0) {
    return (
      <StatusPanel
        variant="empty"
        title="没有数据"
        message="当前表暂时没有查询到记录。"
        action={
          <button onClick={onExport} type="button">
            导出空结果
          </button>
        }
      />
    );
  }

  return (
    <section className="content-panel" data-testid="data-grid-shell">
      <header className="content-panel__header" data-testid="data-grid-toolbar">
        <div className="content-panel__title-group">
          <h2>表数据</h2>
          <p>{data.rows.length} rows</p>
        </div>
        <div className="content-toolbar">
          <span className="content-toolbar__meta">
            第 {data.page} 页 / {data.pageSize} 条
          </span>
          <button onClick={onExport} type="button">
            导出
          </button>
        </div>
      </header>
      <div className="data-table-shell" data-testid="data-grid-table">
        <table className="data-table">
          <thead>
            <tr>
              {data.columns.map((column) => (
                <th key={column}>{column}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.rows.map((row, index) => (
              <tr key={`${index}-${String(row[data.columns[0] ?? "row"])}`}>
                {data.columns.map((column) => (
                  <td key={`${index}-${column}`}>{String(row[column] ?? "")}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="content-toolbar content-toolbar--footer">
        <button type="button">刷新</button>
        <button type="button">上一页</button>
        <button type="button">下一页</button>
      </div>
    </section>
  );
}
