import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { App } from "../../../src/renderer/app/App";
import { useConnectionStore } from "../../../src/renderer/stores/connection-store";
import { useDataViewStore } from "../../../src/renderer/stores/data-view-store";
import { useExplorerStore } from "../../../src/renderer/stores/explorer-store";

const invokeMock = vi.fn();

beforeEach(() => {
  invokeMock.mockReset();
  useConnectionStore.getState().reset();
  useDataViewStore.getState().reset();
  useExplorerStore.getState().reset();
  window.electron = {
    ipcRenderer: {
      invoke: invokeMock
    }
  };
});

describe("SchemaTable", () => {
  it("loads schema columns after selecting a table node", async () => {
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
          columns: [
            {
              name: "type",
              dataType: "text",
              nullable: false,
              defaultValue: null,
              isPrimaryKey: false
            }
          ]
        };
      }

      return [];
    });

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "打开 Local SQLite" }));
    fireEvent.click(await screen.findByRole("button", { name: "选择 sqlite_master" }));

    expect(await screen.findByText("字段名")).toBeInTheDocument();
    expect(screen.getByText("type")).toBeInTheDocument();
    expect(screen.getByText("text")).toBeInTheDocument();
  });

  it("renders fluent schema shells after a table is selected", async () => {
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
          columns: [
            {
              name: "type",
              dataType: "text",
              nullable: false,
              defaultValue: null,
              isPrimaryKey: false
            }
          ]
        };
      }

      return [];
    });

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "打开 Local SQLite" }));
    fireEvent.click(await screen.findByRole("button", { name: "选择 sqlite_master" }));

    expect(await screen.findByTestId("schema-table-shell")).toBeInTheDocument();
    expect(screen.getByTestId("schema-table-header")).toBeInTheDocument();
    expect(screen.getByTestId("schema-table-grid")).toBeInTheDocument();
  });
});
