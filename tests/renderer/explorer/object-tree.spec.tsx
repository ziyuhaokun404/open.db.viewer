import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { App } from "../../../src/renderer/app/App";
import { useConnectionStore } from "../../../src/renderer/stores/connection-store";
import { useExplorerStore } from "../../../src/renderer/stores/explorer-store";

const invokeMock = vi.fn();

beforeEach(() => {
  invokeMock.mockReset();
  useConnectionStore.getState().reset();
  useExplorerStore.getState().reset();
  window.electron = {
    ipcRenderer: {
      invoke: invokeMock
    }
  };
});

describe("ObjectTree", () => {
  it("opens the browser page and renders sqlite object nodes", async () => {
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

      return [];
    });

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "打开 Local SQLite" }));

    expect(await screen.findByRole("button", { name: "选择 sqlite_master" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "返回首页" })).toBeInTheDocument();
  });
});
