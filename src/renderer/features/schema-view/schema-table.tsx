import type { TableSchema } from "../../../shared/models/schema";
import { StatusPanel } from "../../components/status-panel";

export function SchemaTable({ schema }: { schema: TableSchema | null }) {
  if (!schema) {
    return (
      <StatusPanel variant="empty" title="未选择表" message="请选择左侧对象树中的表以查看结构。" />
    );
  }

  if (schema.columns.length === 0) {
    return (
      <StatusPanel
        variant="empty"
        title="没有字段信息"
        message={`表 ${schema.tableName} 暂时没有可展示的字段。`}
      />
    );
  }

  return (
    <section className="content-panel" data-testid="schema-table-shell">
      <header className="content-panel__header" data-testid="schema-table-header">
        <div className="content-panel__title-group">
          <h2>表结构</h2>
          <p>{schema.tableName}</p>
        </div>
        <div className="content-toolbar">
          <span className="content-toolbar__meta">{schema.columns.length} columns</span>
        </div>
      </header>
      <div className="data-table-shell" data-testid="schema-table-grid">
        <table className="data-table">
          <thead>
            <tr>
              <th>字段名</th>
              <th>类型</th>
              <th>可空</th>
              <th>默认值</th>
              <th>主键</th>
            </tr>
          </thead>
          <tbody>
            {schema.columns.map((column) => (
              <tr key={column.name}>
                <td>{column.name}</td>
                <td>{column.dataType}</td>
                <td>{column.nullable ? "是" : "否"}</td>
                <td>{column.defaultValue ?? "-"}</td>
                <td>{column.isPrimaryKey ? "是" : "否"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
