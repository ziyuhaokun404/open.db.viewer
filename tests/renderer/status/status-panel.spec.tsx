import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { StatusPanel } from "../../../src/renderer/components/status-panel";

describe("StatusPanel", () => {
  it("renders error message with default title", () => {
    render(<StatusPanel variant="error" message="加载失败" />);

    expect(screen.getByText("发生错误")).toBeInTheDocument();
    expect(screen.getByText("加载失败")).toBeInTheDocument();
  });

  it("renders custom title and action content", () => {
    render(
      <StatusPanel
        variant="success"
        title="连接已通过"
        message="可以继续保存这个连接。"
        action={<button type="button">继续</button>}
      />
    );

    expect(screen.getByText("连接已通过")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "继续" })).toBeInTheDocument();
  });
});
