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
    ipcRenderer: {
      invoke: invokeMock
    }
  };
});

describe("QueryEditor", () => {
  it("executes sql and renders query results", async () => {
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
        return [
          {
            id: "db:demo.db",
            kind: "database",
            name: "demo.db",
            children: [
              {
                id: "table:sqlite_master",
                kind: "table",
                name: "sqlite_master",
                parentId: "db:demo.db"
              }
            ]
          }
        ];
      }

      if (channel === "query:execute") {
        return {
          columns: ["type", "name"],
          rows: [
            {
              type: "table",
              name: "sqlite_master"
            }
          ],
          rowCount: 1,
          durationMs: 12
        };
      }

      if (channel === "schema:get") {
        return {
          tableName: "sqlite_master",
          columns: []
        };
      }

      if (channel === "table-data:get") {
        return {
          columns: ["type", "name"],
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
    fireEvent.click(await screen.findByRole("button", { name: "查询" }));
    fireEvent.change(await screen.findByLabelText("SQL 输入区"), {
      target: { value: "select * from sqlite_master;" }
    });
    fireEvent.click(screen.getByRole("button", { name: "执行" }));

    expect(await screen.findByText("查询结果")).toBeInTheDocument();
    expect(screen.getAllByText("sqlite_master").length).toBeGreaterThan(0);
    expect(screen.getByText("1 rows / 12 ms")).toBeInTheDocument();
  });

  it("renders fluent query workspace shells", async () => {
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
        return [
          {
            id: "db:demo.db",
            kind: "database",
            name: "demo.db",
            children: [
              {
                id: "table:sqlite_master",
                kind: "table",
                name: "sqlite_master",
                parentId: "db:demo.db"
              }
            ]
          }
        ];
      }

      if (channel === "query:execute") {
        return {
          columns: ["type", "name"],
          rows: [
            {
              type: "table",
              name: "sqlite_master"
            }
          ],
          rowCount: 1,
          durationMs: 12
        };
      }

      if (channel === "schema:get") {
        return {
          tableName: "sqlite_master",
          columns: []
        };
      }

      if (channel === "table-data:get") {
        return {
          columns: ["type", "name"],
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
    fireEvent.click(await screen.findByRole("button", { name: "查询" }));

    expect(await screen.findByTestId("query-editor-shell")).toBeInTheDocument();
    expect(screen.getByTestId("query-editor-toolbar")).toBeInTheDocument();
    expect(screen.getByTestId("query-result-shell")).toBeInTheDocument();
  });
});
