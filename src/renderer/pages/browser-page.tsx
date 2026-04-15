import { useEffect, useState } from "react";
import { AppShell } from "../app/app-shell";
import { PageHeader } from "../components/page-header";
import { StatusPanel } from "../components/status-panel";
import { TabBar } from "../components/tab-bar";
import { ObjectTree } from "../features/explorer/object-tree";
import { DataGrid } from "../features/data-view/data-grid";
import { useExportStore } from "../stores/export-store";
import { SchemaTable } from "../features/schema-view/schema-table";
import { QueryEditor } from "../features/query-view/query-editor";
import { QueryResultGrid } from "../features/query-view/query-result-grid";
import { useConnectionStore } from "../stores/connection-store";
import { useDataViewStore } from "../stores/data-view-store";
import { useExplorerStore } from "../stores/explorer-store";
import { useQueryStore } from "../stores/query-store";
import { useSchemaStore } from "../stores/schema-store";

export function BrowserPage() {
  const [activeTab, setActiveTab] = useState("结构");
  const activeConnection = useConnectionStore((state) => state.activeConnection);
  const goHome = useConnectionStore((state) => state.goHome);
  const { tree, selectedNode, isLoading, error, loadTree, selectNode } = useExplorerStore();
  const {
    currentSchema,
    isLoading: isLoadingSchema,
    error: schemaError,
    loadSchema
  } = useSchemaStore();
  const {
    currentTableData,
    isLoading: isLoadingData,
    error: dataError,
    loadTableData
  } = useDataViewStore();
  const {
    result: queryResult,
    error: queryError,
    execute: executeQuery
  } = useQueryStore();
  const { message: exportMessage, exportQueryResult, exportTableData } = useExportStore();

  useEffect(() => {
    if (activeConnection) {
      void loadTree(activeConnection);
    }
  }, [activeConnection, loadTree]);

  useEffect(() => {
    if (activeConnection && selectedNode?.kind === "table") {
      setActiveTab("结构");
      void loadSchema(activeConnection, selectedNode.name);
      void loadTableData(activeConnection, selectedNode.name);
    }
  }, [activeConnection, selectedNode, loadSchema, loadTableData]);

  if (!activeConnection) {
    return (
      <AppShell sidebar={<p>请先从首页选择一个已保存连接。</p>}>
        <StatusPanel
          variant="empty"
          title="没有活动连接"
          message="请先从首页选择一个已保存连接。"
          action={
            <button onClick={goHome} type="button">
              返回首页
            </button>
          }
        />
      </AppShell>
    );
  }

  const handleExecuteQuery = () => {
    void executeQuery(activeConnection);
  };

  const handleExportQueryResult = () => {
    if (queryResult) {
      void exportQueryResult(queryResult.columns, queryResult.rows);
    }
  };

  const handleExportTableData = () => {
    if (currentTableData) {
      void exportTableData(currentTableData.columns, currentTableData.rows);
    }
  };

  const sidebar = (
    <section className="workspace-sidebar">
      <h2>对象树</h2>
      {isLoading ? <StatusPanel variant="loading" message="正在加载对象树..." /> : null}
      {error ? <StatusPanel variant="error" message={error} /> : null}
      {!isLoading && !error ? <ObjectTree nodes={tree} onSelect={selectNode} /> : null}
    </section>
  );

  const renderActiveTab = () => {
    if (activeTab === "数据") {
      if (isLoadingData) {
        return <StatusPanel variant="loading" message="正在加载表数据..." />;
      }

      if (dataError) {
        return <StatusPanel variant="error" message={dataError} />;
      }

      return <DataGrid data={currentTableData} onExport={handleExportTableData} />;
    }

    if (activeTab === "查询") {
      return (
        <>
          <QueryEditor onExecute={handleExecuteQuery} />
          {queryError ? <StatusPanel variant="error" message={queryError} /> : null}
          <QueryResultGrid result={queryResult} onExport={handleExportQueryResult} />
        </>
      );
    }

    if (isLoadingSchema) {
      return <StatusPanel variant="loading" message="正在加载表结构..." />;
    }

    if (schemaError) {
      return <StatusPanel variant="error" message={schemaError} />;
    }

    return <SchemaTable schema={currentSchema} />;
  };

  return (
    <AppShell
      sidebar={sidebar}
      footer={
        exportMessage ? (
          <StatusPanel variant="success" title="导出完成" message={exportMessage} />
        ) : selectedNode ? (
          <p>{activeConnection.name} / {selectedNode.name} / {activeTab}</p>
        ) : (
          <p>选择左侧对象开始浏览。</p>
        )
      }
    >
      <section className="workspace-shell">
      <PageHeader
        title={selectedNode?.name ?? "请选择一个表"}
        description={
          selectedNode?.kind === "table"
            ? `${selectedNode.name} 工作区`
            : "从左侧选择一个表"
        }
        action={
          <button className="workspace-header__back" onClick={goHome} type="button">
            返回首页
          </button>
        }
      />
      <TabBar tabs={["结构", "数据", "查询"]} activeTab={activeTab} onChange={setActiveTab} />
      {renderActiveTab()}
      </section>
    </AppShell>
  );
}
