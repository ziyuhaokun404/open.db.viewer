import type { ChangeEvent } from "react";
import { StatusPanel } from "../../components/status-panel";
import { useQueryStore } from "../../stores/query-store";

export function QueryEditor({ onExecute }: { onExecute: () => void }) {
  const sqlText = useQueryStore((state) => state.sqlText);
  const isExecuting = useQueryStore((state) => state.isExecuting);
  const updateSqlText = useQueryStore((state) => state.updateSqlText);
  const result = useQueryStore((state) => state.result);

  const handleChange = (event: ChangeEvent<HTMLTextAreaElement>) => {
    updateSqlText(event.target.value);
  };

  return (
    <section className="content-panel editor-shell" data-testid="query-editor-shell">
      <header className="content-panel__header" data-testid="query-editor-toolbar">
        <div className="content-panel__title-group">
          <h2>SQL</h2>
          <p>Ready</p>
        </div>
        <div className="content-toolbar">
          <span className="content-toolbar__meta">{isExecuting ? "Running" : "Editable"}</span>
          <button onClick={() => updateSqlText("")} type="button">
            清空
          </button>
        </div>
      </header>
      <div className="content-toolbar">
        <button onClick={onExecute} type="button">
          {isExecuting ? "执行中..." : "执行"}
        </button>
      </div>
      <label className="editor-shell__input">
        <span className="sr-only">SQL 输入区</span>
        <textarea aria-label="SQL 输入区" rows={8} value={sqlText} onChange={handleChange} />
      </label>
      {isExecuting ? <StatusPanel variant="loading" message="SQL 正在执行，请稍候。" /> : null}
      {!isExecuting && result ? (
        <StatusPanel
          variant="success"
          title="查询执行完成"
          message={`已返回 ${result.rowCount} 行，耗时 ${result.durationMs}ms。`}
        />
      ) : null}
    </section>
  );
}
