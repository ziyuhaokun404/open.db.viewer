import { connectionApi } from "./connection-api";
import { StatusPanel } from "../../components/status-panel";
import { useConnectionStore } from "../../stores/connection-store";

function inferConnectionNameFromFilePath(filePath: string) {
  const fileName = filePath.split(/[/\\]/).pop() ?? "";
  return fileName.replace(/\.(db|sqlite|sqlite3)$/i, "") || fileName;
}

function SQLiteFields() {
  const formValues = useConnectionStore((state) => state.formValues);
  const updateForm = useConnectionStore((state) => state.updateForm);

  const browseSQLiteFile = async () => {
    const selectedFile = await connectionApi.selectSQLiteFile();
    if (selectedFile) {
      const nextSuggestedName = inferConnectionNameFromFilePath(selectedFile);
      const currentSuggestedName = inferConnectionNameFromFilePath(formValues.filePath);

      updateForm("filePath", selectedFile);

      if (!formValues.name || formValues.name === currentSuggestedName) {
        updateForm("name", nextSuggestedName);
      }
    }
  };

  return (
    <label className="connection-form__field">
      数据库文件
      <div className="connection-form__file-picker">
        <input
          aria-label="数据库文件"
          value={formValues.filePath}
          onChange={(event) => updateForm("filePath", event.target.value)}
        />
        <button className="connection-form__browse" onClick={() => void browseSQLiteFile()} type="button">
          选择数据库文件
        </button>
      </div>
    </label>
  );
}

export function ConnectionForm({ variant = "page" }: { variant?: "page" | "dialog" }) {
  const {
    formValues,
    isTestingConnection,
    testResult,
    updateForm,
    testConnection,
    saveConnection,
    goHome
  } = useConnectionStore();
  const isDialog = variant === "dialog";

  return (
    <section
      className={`connection-form ${isDialog ? "connection-form--dialog" : ""}`}
      data-testid="connection-form-shell"
    >
      <div className="connection-form__intro">
        {isDialog ? (
          <div className="connection-form__dialog-head" data-testid="connection-form-dialog-head">
            <div className="connection-form__dialog-copy">
              <p className="connection-form__eyebrow">SQLite</p>
              <h1 id="new-connection-dialog-title">打开一个 SQLite 数据库</h1>
              <p>选择数据库文件，测试通过后保存到常用入口。</p>
            </div>
            <button aria-label="关闭新建连接" className="connection-form__close" onClick={goHome} type="button">
              ×
            </button>
          </div>
        ) : (
          <>
            <button className="connection-form__back" onClick={goHome} type="button">
              返回
            </button>
            <div>
              <h1>打开一个 SQLite 数据库</h1>
              <p>选择数据库文件，然后测试并保存。</p>
            </div>
          </>
        )}
      </div>

      <section className="connection-form__panel">
        <h2>SQLite 数据库</h2>
        <p className="connection-form__label">当前版本仅提供 SQLite 工作流。</p>

        <label className="connection-form__field">
          数据库名称
          <input
            aria-label="数据库名称"
            value={formValues.name}
            onChange={(event) => updateForm("name", event.target.value)}
          />
        </label>
      </section>

      <section className="connection-form__panel">
        <h2>文件位置</h2>
        <div className="connection-form__grid">
          <SQLiteFields />
        </div>
      </section>

      <div className="connection-form__actions" data-testid="connection-form-actions">
        <button className="connection-form__secondary-action" onClick={() => void testConnection()} type="button">
          {isTestingConnection ? "测试中..." : "测试连接"}
        </button>
        <button className="connection-form__primary-action" onClick={() => void saveConnection()} type="button">
          保存连接
        </button>
      </div>

      {testResult ? (
        <StatusPanel
          variant={testResult.success ? "success" : "error"}
          title={testResult.success ? "连接测试通过" : "连接测试失败"}
          message={testResult.message}
        />
      ) : null}
    </section>
  );
}
