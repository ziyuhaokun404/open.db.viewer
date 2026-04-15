import type { QueryResult } from "../../../shared/models/query";
import { StatusPanel } from "../../components/status-panel";

export function QueryResultGrid({
  result,
  onExport
}: {
  result: QueryResult | null;
  onExport: () => void;
}) {
  if (!result) {
    return <StatusPanel variant="empty" title="暂无查询结果" message="执行 SQL 后会在这里显示结果。" />;
  }

  if (result.rows.length === 0) {
    return (
      <StatusPanel
        variant="empty"
        title="查询已完成"
        message={`本次查询没有返回记录，耗时 ${result.durationMs}ms。`}
      />
    );
  }

  return (
    <section className="content-panel query-result-shell" data-testid="query-result-shell">
      <header className="content-panel__header" data-testid="query-result-toolbar">
        <div className="content-panel__title-group">
          <h2>查询结果</h2>
          <p>
            {result.rowCount} rows / {result.durationMs} ms
          </p>
        </div>
        <div className="content-toolbar">
          <button onClick={onExport} type="button">
            导出
          </button>
        </div>
      </header>
      <div className="data-table-shell" data-testid="query-result-table">
        <table className="data-table">
          <thead>
            <tr>
              {result.columns.map((column) => (
                <th key={column}>{column}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {result.rows.map((row, index) => (
              <tr key={`${index}-${String(row[result.columns[0] ?? "row"])}`}>
                {result.columns.map((column) => (
                  <td key={`${index}-${column}`}>{String(row[column] ?? "")}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
