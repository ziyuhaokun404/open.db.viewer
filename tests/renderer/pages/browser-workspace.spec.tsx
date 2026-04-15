import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { App } from "../../../src/renderer/app/App";
import { useConnectionStore } from "../../../src/renderer/stores/connection-store";
import { useDataViewStore } from "../../../src/renderer/stores/data-view-store";
import { useExplorerStore } from "../../../src/renderer/stores/explorer-store";
import { useSchemaStore } from "../../../src/renderer/stores/schema-store";

const invokeMock = vi.fn();

beforeEach(() => {
  invokeMock.mockReset();
  useConnectionStore.getState().reset();
  useDataViewStore.getState().reset();
  useExplorerStore.getState().reset();
  useSchemaStore.getState().reset();
  window.electron = {
    ipcRenderer: { invoke: invokeMock }
  };
});

describe("Browser workspace", () => {
  it("switches between 结构 and 数据 tabs", async () => {
    invokeMock.mockImplementation(async (channel: string) => {
      if (channel === "connection:list") {
        return [{ id: "sqlite-1", type: "sqlite", name: "Local SQLite", filePath: "demo.db" }];
      }

      if (channel === "explorer:load") {
        return [{ id: "table:users", kind: "table", name: "users" }];
      }

      if (channel === "schema:get") {
        return {
          tableName: "users",
          columns: [
            {
              name: "id",
              dataType: "integer",
              nullable: false,
              defaultValue: null,
              isPrimaryKey: true
            }
          ]
        };
      }

      if (channel === "table-data:get") {
        return {
          columns: ["id"],
          rows: [{ id: 1 }],
          page: 1,
          pageSize: 50,
          hasNextPage: false
        };
      }

      return [];
    });

    render(<App />);
    fireEvent.click(await screen.findByRole("button", { name: "打开 Local SQLite" }));
    fireEvent.click(await screen.findByRole("button", { name: "选择 users" }));
    fireEvent.click(await screen.findByRole("button", { name: "数据" }));

    expect(await screen.findByText("表数据")).toBeInTheDocument();
  });

  it("renders a dedicated workspace header shell", async () => {
    invokeMock.mockImplementation(async (channel: string) => {
      if (channel === "connection:list") {
        return [{ id: "sqlite-1", type: "sqlite", name: "Local SQLite", filePath: "demo.db" }];
      }

      if (channel === "explorer:load") {
        return [{ id: "table:users", kind: "table", name: "users" }];
      }

      if (channel === "schema:get") {
        return {
          tableName: "users",
          columns: []
        };
      }

      if (channel === "table-data:get") {
        return {
          columns: [],
          rows: [],
          page: 1,
          pageSize: 50,
          hasNextPage: false
        };
      }

      return [];
    });

    render(<App />);
    fireEvent.click(await screen.findByRole("button", { name: "打开 Local SQLite" }));

    expect(await screen.findByTestId("workspace-header")).toBeInTheDocument();
    expect(screen.getByTestId("workspace-tabs")).toBeInTheDocument();
  });
});
