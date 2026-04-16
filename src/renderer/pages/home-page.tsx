import { useEffect } from "react";
import { AppShell } from "../app/app-shell";
import { EmptyState } from "../components/empty-state";
import { StatusPanel } from "../components/status-panel";
import { ConnectionForm } from "../features/connections/connection-form";
import { ConnectionList } from "../features/connections/connection-list";
import { useConnectionStore } from "../stores/connection-store";

export function HomePage() {
  const {
    currentView,
    savedConnections,
    isLoadingList,
    listErrorMessage,
    saveMessage,
    loadConnections,
    openNewConnection,
    openConnection
  } = useConnectionStore();
  const isConnectionDialogOpen = currentView === "new-connection";
  const connectionCount = savedConnections.length;
  const connectionSummary =
    connectionCount > 0 ? `已保存 ${connectionCount} 个 SQLite 数据库。` : "打开一个 SQLite 文件后，这里会保留常用数据库入口。";

  useEffect(() => {
    void loadConnections();
  }, [loadConnections]);

  const sidebar = (
    <section className="sidebar-panel">
      <p className="sidebar-panel__title">已保存数据库</p>
      <p className="sidebar-panel__hint">常用的 SQLite 文件会保留在这里，方便再次打开。</p>
      {isLoadingList ? (
        <StatusPanel variant="loading" message="正在加载连接列表..." />
      ) : listErrorMessage ? (
        <StatusPanel variant="error" title="连接列表不可用" message={listErrorMessage} />
      ) : savedConnections.length > 0 ? (
        <ConnectionList connections={savedConnections} onOpen={openConnection} />
      ) : (
        <EmptyState
          title="还没有数据库"
          message="打开第一个 SQLite 文件后，这里会显示常用入口。"
          action={
            <button onClick={openNewConnection} type="button">
              打开 SQLite 文件
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
          <p>就绪：打开一个 SQLite 文件开始浏览。</p>
        )
      }
      overlay={
        isConnectionDialogOpen ? (
          <div className="dialog-layer">
            <div
              aria-labelledby="new-connection-dialog-title"
              aria-modal="true"
              className="dialog-layer__surface"
              role="dialog"
            >
              <ConnectionForm variant="dialog" />
            </div>
          </div>
        ) : null
      }
    >
      <section className="home-workspace">
        <section className="home-launch" data-testid="home-launch-surface">
          <div className="home-launch__header">
            <p className="home-launch__eyebrow">SQLite Viewer</p>
            <div className="home-launch__status">当前目标：SQLite</div>
          </div>
          <div className="home-launch__intro">
            <div className="home-launch__title-wrap">
              <h2 className="home-launch__title">打开一个 SQLite 数据库</h2>
              <p className="home-launch__subtitle">一个轻量、清晰的 SQLite 浏览工具。</p>
            </div>
            <span className="home-launch__hint">直接浏览结构、数据和查询结果</span>
          </div>

          <div className="home-launch__actions">
            <button onClick={openNewConnection} type="button">
              打开 SQLite 文件
            </button>
          </div>
        </section>

        <section className="home-library">
          <div className="home-library__header">
            <div>
              <p className="home-library__eyebrow">Library</p>
              <h3>已保存数据库</h3>
            </div>
            <span className="home-library__meta">{connectionCount} 个条目</span>
          </div>
          <p className="home-library__summary">{connectionSummary}</p>
          <div className="home-library__notes">
            <p>短期目标只聚焦 SQLite，先把浏览体验和界面细节做扎实。</p>
            <p>从左侧可以再次打开常用数据库，从上方主入口可以继续添加新的 SQLite 文件。</p>
          </div>
        </section>
      </section>
    </AppShell>
  );
}
