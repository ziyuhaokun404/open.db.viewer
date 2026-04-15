import { useEffect } from "react";
import { AppShell } from "../app/app-shell";
import { EmptyState } from "../components/empty-state";
import { StatusPanel } from "../components/status-panel";
import { ConnectionList } from "../features/connections/connection-list";
import { useConnectionStore } from "../stores/connection-store";

export function HomePage() {
  const {
    savedConnections,
    isLoadingList,
    saveMessage,
    loadConnections,
    openNewConnection,
    openConnection
  } = useConnectionStore();

  useEffect(() => {
    void loadConnections();
  }, [loadConnections]);

  const sidebar = (
    <section className="sidebar-panel">
      <p className="sidebar-panel__title">已保存连接</p>
      <p className="sidebar-panel__hint">常用连接会固定在这里，方便你直接进入工作区。</p>
      {isLoadingList ? (
        <StatusPanel variant="loading" message="正在加载连接列表..." />
      ) : savedConnections.length > 0 ? (
        <ConnectionList connections={savedConnections} onOpen={openConnection} />
      ) : (
        <EmptyState
          title="还没有连接"
          message="从左侧开始新建第一个数据库连接。"
          action={
            <button onClick={openNewConnection} type="button">
              新建连接
            </button>
          }
        />
      )}
    </section>
  );

  return (
    <AppShell
      sidebar={sidebar}
      footer={
        saveMessage ? (
          <StatusPanel variant="success" title="连接已保存" message={saveMessage} />
        ) : (
          <p>就绪：选择连接或新建连接。</p>
        )
      }
    >
      <section className="home-workspace">
        <section className="home-hero">
          <div className="home-hero__header">
            <p className="home-hero__eyebrow">Connections</p>
            <div className="home-hero__status">MySQL / PostgreSQL / SQLite</div>
          </div>
          <h2 className="home-hero__title">直接开始连接数据库</h2>
          <div className="home-hero__actions">
            <button onClick={openNewConnection} type="button">
              创建连接
            </button>
            <span>已支持 3 种主流关系型数据库</span>
          </div>
          <div className="home-hero__metrics">
            <div className="home-metric">
              <strong>连接</strong>
              <span>保存常用库</span>
            </div>
            <div className="home-metric">
              <strong>浏览</strong>
              <span>切换结构与数据</span>
            </div>
            <div className="home-metric">
              <strong>导出</strong>
              <span>按需导出结果</span>
            </div>
          </div>
        </section>

        <section className="home-strip">
          <span>轻量</span>
          <span>直接</span>
          <span>低干扰</span>
        </section>
      </section>
    </AppShell>
  );
}
