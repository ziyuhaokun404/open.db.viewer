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

describe("DataGrid", () => {
  it("loads table rows and pagination controls after selecting a table", async () => {
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

      if (channel === "schema:get") {
        return {
          tableName: "sqlite_master",
          columns: []
        };
      }

      if (channel === "table-data:get") {
        return {
          columns: ["type", "name"],
          rows: [
            {
              type: "table",
              name: "sqlite_master"
            }
          ],
          page: 1,
          pageSize: 50,
          hasNextPage: false
        };
      }

      return [];
    });

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "打开 Local SQLite" }));
    fireEvent.click(await screen.findByRole("button", { name: "选择 sqlite_master" }));
    fireEvent.click(await screen.findByRole("button", { name: "数据" }));

    expect(await screen.findByText("表数据")).toBeInTheDocument();
    expect(screen.getAllByText("sqlite_master").length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: "上一页" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "下一页" })).toBeInTheDocument();
  });

  it("renders fluent data panel shells", async () => {
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

      if (channel === "schema:get") {
        return {
          tableName: "sqlite_master",
          columns: []
        };
      }

      if (channel === "table-data:get") {
        return {
          columns: ["type", "name"],
          rows: [
            {
              type: "table",
              name: "sqlite_master"
            }
          ],
          page: 1,
          pageSize: 50,
          hasNextPage: false
        };
      }

      return [];
    });

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "打开 Local SQLite" }));
    fireEvent.click(await screen.findByRole("button", { name: "选择 sqlite_master" }));
    fireEvent.click(await screen.findByRole("button", { name: "数据" }));

    expect(await screen.findByTestId("data-grid-shell")).toBeInTheDocument();
    expect(screen.getByTestId("data-grid-toolbar")).toBeInTheDocument();
    expect(screen.getByTestId("data-grid-table")).toBeInTheDocument();
  });
});
