import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { App } from "../../../src/renderer/app/App";
import { useConnectionStore } from "../../../src/renderer/stores/connection-store";
import { useDataViewStore } from "../../../src/renderer/stores/data-view-store";
import { useExplorerStore } from "../../../src/renderer/stores/explorer-store";
import { useQueryStore } from "../../../src/renderer/stores/query-store";
import { useSchemaStore } from "../../../src/renderer/stores/schema-store";

const invokeMock = vi.fn();

beforeEach(() => {
  invokeMock.mockReset();
  useConnectionStore.getState().reset();
  useDataViewStore.getState().reset();
  useExplorerStore.getState().reset();
  useQueryStore.getState().reset();
  useSchemaStore.getState().reset();
  window.electron = {
    ipcRenderer: {
      invoke: invokeMock
    }
  };
});

describe("Export actions", () => {
  it("exports query results after executing sql", async () => {
    invokeMock.mockImplementation(async (channel: string) => {
      if (channel === "connection:list") {
        return [
          {
            id: "sqlite-1",
            type: "sqlite",
            name: "Local SQLite",
            filePath: "demo.db"
          }
        ];
      }

      if (channel === "explorer:load") {
        return [];
      }

      if (channel === "query:execute") {
        return {
          columns: ["type", "name"],
          rows: [{ type: "table", name: "sqlite_master" }],
          rowCount: 1,
          durationMs: 12
        };
      }

      if (channel === "export:csv") {
        return {
          success: true,
          message: "CSV 导出成功"
        };
      }

      return [];
    });

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "打开 Local SQLite" }));
    fireEvent.click(await screen.findByRole("button", { name: "查询" }));
    fireEvent.click(screen.getByRole("button", { name: "执行" }));
    await screen.findByText("查询结果");

    fireEvent.click(screen.getByRole("button", { name: "导出" }));

    expect(await screen.findByText("CSV 导出成功")).toBeInTheDocument();
  });
});
