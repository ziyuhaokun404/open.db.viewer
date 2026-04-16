import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { HomePage } from "../../../src/renderer/pages/home-page";
import { useConnectionStore } from "../../../src/renderer/stores/connection-store";

beforeEach(() => {
  useConnectionStore.getState().reset();
});

describe("HomePage layout", () => {
  it("renders a SQLite-first home workspace", async () => {
    render(<HomePage />);

    expect(await screen.findByText("Open DB Viewer")).toBeInTheDocument();
    expect(await screen.findByTestId("home-launch-surface")).toBeInTheDocument();
    expect(screen.getByText("已保存数据库", { selector: ".sidebar-panel__title" })).toBeInTheDocument();
    expect(screen.getByText("打开一个 SQLite 数据库")).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: "打开 SQLite 文件" }).length).toBeGreaterThan(0);
    expect(screen.getByText("Library")).toBeInTheDocument();
    expect(screen.getByText("短期目标只聚焦 SQLite，先把浏览体验和界面细节做扎实。")).toBeInTheDocument();
    expect(screen.queryByText("下一步")).not.toBeInTheDocument();
    expect(screen.queryByText("使用提示")).not.toBeInTheDocument();
  });

  it("renders SQLite-first copy", async () => {
    render(<HomePage />);

    expect(await screen.findByRole("heading", { name: "打开一个 SQLite 数据库" })).toBeInTheDocument();
    expect(screen.getByText("一个轻量、清晰的 SQLite 浏览工具。")).toBeInTheDocument();
    expect(screen.queryByText("MySQL / PostgreSQL / SQLite")).not.toBeInTheDocument();
  });

  it("removes dashboard summary cards from the home page", async () => {
    render(<HomePage />);

    expect(await screen.findByTestId("home-launch-surface")).toBeInTheDocument();
    expect(screen.queryByText("连接总览")).not.toBeInTheDocument();
    expect(screen.queryByText("当前工作台状态")).not.toBeInTheDocument();
    expect(screen.queryByText("当前范围")).not.toBeInTheDocument();
  });
});
