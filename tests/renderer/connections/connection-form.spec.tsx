import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { App } from "../../../src/renderer/app/App";
import { useConnectionStore } from "../../../src/renderer/stores/connection-store";

const invokeMock = vi.fn();

beforeEach(() => {
  invokeMock.mockReset();
  useConnectionStore.getState().reset();
  window.electron = {
    ...window.electron,
    ipcRenderer: {
      invoke: invokeMock
    }
  };
});

afterEach(() => {
  vi.clearAllMocks();
});

describe("Connection flow", () => {
  it("opens the connection form inside a dialog from the home workspace", async () => {
    invokeMock.mockResolvedValue([]);

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "打开 SQLite 文件" }));

    const dialog = screen.getByRole("dialog", { name: "打开一个 SQLite 数据库" });

    expect(dialog).toBeInTheDocument();
    expect(within(dialog).getByRole("heading", { name: "打开一个 SQLite 数据库" })).toBeInTheDocument();
    expect(screen.getByLabelText("数据库文件")).toBeInTheDocument();
  });

  it("closes the dialog and returns focus to the home workspace", async () => {
    invokeMock.mockResolvedValue([]);

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "打开 SQLite 文件" }));
    fireEvent.click(screen.getByRole("button", { name: "关闭新建连接" }));

    await waitFor(() => {
      expect(screen.queryByRole("dialog", { name: "打开一个 SQLite 数据库" })).not.toBeInTheDocument();
    });

    expect(screen.getByText("打开一个 SQLite 数据库")).toBeInTheDocument();
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

    fireEvent.click(await screen.findByRole("button", { name: "打开 SQLite 文件" }));
    fireEvent.change(screen.getByLabelText("数据库名称"), {
      target: { value: "Local SQLite" }
    });
    fireEvent.change(screen.getByLabelText("数据库文件"), {
      target: { value: "demo.db" }
    });

    fireEvent.click(screen.getByRole("button", { name: "测试连接" }));

    await screen.findByText("Connection successful.");

    fireEvent.click(screen.getByRole("button", { name: "保存连接" }));

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "打开 Local SQLite" })).toBeInTheDocument();
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

  it("fills the sqlite file path from the native file picker", async () => {
    invokeMock.mockImplementation(async (channel: string) => {
      if (channel === "connection:list") {
        return [];
      }

      if (channel === "connection:select-sqlite-file") {
        return "C:\\Data\\sample.db";
      }

      return [];
    });

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "打开 SQLite 文件" }));
    fireEvent.click(screen.getByRole("button", { name: "选择数据库文件" }));

    await waitFor(() => {
      expect(screen.getByLabelText("数据库文件")).toHaveValue("C:\\Data\\sample.db");
    });

    expect(invokeMock).toHaveBeenCalledWith("connection:select-sqlite-file");
  });

  it("derives the sqlite connection name from the selected file when the name is empty", async () => {
    invokeMock.mockImplementation(async (channel: string) => {
      if (channel === "connection:list") {
        return [];
      }

      if (channel === "connection:select-sqlite-file") {
        return "C:\\Data\\sample.db";
      }

      return [];
    });

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "打开 SQLite 文件" }));
    fireEvent.click(screen.getByRole("button", { name: "选择数据库文件" }));

    await waitFor(() => {
      expect(screen.getByLabelText("数据库文件")).toHaveValue("C:\\Data\\sample.db");
      expect(screen.getByLabelText("数据库名称")).toHaveValue("sample");
    });
  });

  it("does not overwrite a custom sqlite connection name after choosing a file", async () => {
    invokeMock.mockImplementation(async (channel: string) => {
      if (channel === "connection:list") {
        return [];
      }

      if (channel === "connection:select-sqlite-file") {
        return "C:\\Data\\northwind.sqlite";
      }

      return [];
    });

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "打开 SQLite 文件" }));
    fireEvent.change(screen.getByLabelText("数据库名称"), {
      target: { value: "My Local DB" }
    });
    fireEvent.click(screen.getByRole("button", { name: "选择数据库文件" }));

    await waitFor(() => {
      expect(screen.getByLabelText("数据库文件")).toHaveValue("C:\\Data\\northwind.sqlite");
      expect(screen.getByLabelText("数据库名称")).toHaveValue("My Local DB");
    });
  });

  it("shows only the SQLite option in the active form flow", async () => {
    invokeMock.mockResolvedValue([]);

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "打开 SQLite 文件" }));

    const dialog = screen.getByRole("dialog", { name: "打开一个 SQLite 数据库" });

    expect(within(dialog).getByText("SQLite")).toBeInTheDocument();
    expect(within(dialog).queryByText("MySQL")).not.toBeInTheDocument();
    expect(within(dialog).queryByText("PostgreSQL")).not.toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: "选择数据库文件" })).toBeInTheDocument();
  });
});
