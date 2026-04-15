import { SegmentedControl } from "../../components/segmented-control";
import { StatusPanel } from "../../components/status-panel";
import { useConnectionStore } from "../../stores/connection-store";

function DatabaseFields() {
  const formValues = useConnectionStore((state) => state.formValues);
  const updateForm = useConnectionStore((state) => state.updateForm);

  if (formValues.type === "sqlite") {
    return (
      <label className="connection-form__field">
        数据库文件
        <input
          aria-label="数据库文件"
          value={formValues.filePath}
          onChange={(event) => updateForm("filePath", event.target.value)}
        />
      </label>
    );
  }

  return (
    <>
      <label className="connection-form__field">
        主机地址
        <input
          aria-label="主机地址"
          value={formValues.host}
          onChange={(event) => updateForm("host", event.target.value)}
        />
      </label>
      <label className="connection-form__field">
        端口
        <input
          aria-label="端口"
          value={formValues.port}
          onChange={(event) => updateForm("port", event.target.value)}
        />
      </label>
      <label className="connection-form__field">
        用户名
        <input
          aria-label="用户名"
          value={formValues.username}
          onChange={(event) => updateForm("username", event.target.value)}
        />
      </label>
      <label className="connection-form__field">
        密码
        <input
          aria-label="密码"
          type="password"
          value={formValues.password}
          onChange={(event) => updateForm("password", event.target.value)}
        />
      </label>
      <label className="connection-form__field">
        数据库名
        <input
          aria-label="数据库名"
          value={formValues.database}
          onChange={(event) => updateForm("database", event.target.value)}
        />
      </label>
    </>
  );
}

export function ConnectionForm() {
  const {
    formValues,
    isTestingConnection,
    testResult,
    updateForm,
    testConnection,
    saveConnection,
    goHome
  } = useConnectionStore();

  return (
    <section className="connection-form" data-testid="connection-form-shell">
      <div className="connection-form__intro">
        <button className="connection-form__back" onClick={goHome} type="button">
          返回
        </button>
        <div>
          <h1>新建连接</h1>
          <p>填写必要信息，然后测试并保存。</p>
        </div>
      </div>

      <section className="connection-form__panel">
        <h2>基础信息</h2>
        <p className="connection-form__label">数据库类型</p>
        <SegmentedControl
          value={formValues.type}
          options={[
            { label: "SQLite", value: "sqlite" },
            { label: "MySQL", value: "mysql" },
            { label: "PostgreSQL", value: "postgresql" }
          ]}
          onChange={(value) => updateForm("type", value)}
        />

        <label className="connection-form__field">
          连接名称
          <input
            aria-label="连接名称"
            value={formValues.name}
            onChange={(event) => updateForm("name", event.target.value)}
          />
        </label>
      </section>

      <section className="connection-form__panel">
        <h2>连接信息</h2>
        <div className="connection-form__grid">
          <DatabaseFields />
        </div>
      </section>

      <div className="connection-form__actions" data-testid="connection-form-actions">
        <button onClick={() => void testConnection()} type="button">
          {isTestingConnection ? "测试中..." : "测试连接"}
        </button>
        <button onClick={() => void saveConnection()} type="button">
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
