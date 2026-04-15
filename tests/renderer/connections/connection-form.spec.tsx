import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { App } from "../../../src/renderer/app/App";
import { useConnectionStore } from "../../../src/renderer/stores/connection-store";

const invokeMock = vi.fn();

beforeEach(() => {
  invokeMock.mockReset();
  useConnectionStore.getState().reset();
  window.electron = {
    ipcRenderer: {
      invoke: invokeMock
    }
  };
});

afterEach(() => {
  vi.clearAllMocks();
});

describe("Connection flow", () => {
  it("shows sqlite file path field in the connection form", async () => {
    invokeMock.mockResolvedValue([]);

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "新建连接" }));

    expect(screen.getByLabelText("数据库文件")).toBeInTheDocument();
  });

  it("saves a sqlite connection and returns to the home page list", async () => {
    let savedConnections: Array<{ id: string; name: string; type: string; filePath?: string }> = [];

    invokeMock.mockImplementation(async (channel: string) => {
      if (channel === "connection:list") {
        return savedConnections;
      }

      if (channel === "connection:test") {
        return { success: true, message: "Connection successful." };
      }

      if (channel === "connection:save") {
        savedConnections = [
          {
            id: "sqlite-1",
            name: "Local SQLite",
            type: "sqlite",
            filePath: "demo.db"
          }
        ];
        return undefined;
      }

      return [];
    });

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "新建连接" }));
    fireEvent.change(screen.getByLabelText("连接名称"), {
      target: { value: "Local SQLite" }
    });
    fireEvent.change(screen.getByLabelText("数据库文件"), {
      target: { value: "demo.db" }
    });

    fireEvent.click(screen.getByRole("button", { name: "测试连接" }));

    await screen.findByText("Connection successful.");

    fireEvent.click(screen.getByRole("button", { name: "保存连接" }));

    await waitFor(() => {
      expect(screen.getByText("Local SQLite")).toBeInTheDocument();
    });

    expect(invokeMock).toHaveBeenCalledWith(
      "connection:save",
      expect.objectContaining({
        name: "Local SQLite",
        type: "sqlite",
        filePath: "demo.db"
      })
    );
  });
});
