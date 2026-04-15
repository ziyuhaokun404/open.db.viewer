import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { EmptyState } from "../../../src/renderer/components/empty-state";

describe("EmptyState", () => {
  it("renders title and message", () => {
    render(<EmptyState title="暂无内容" message="请选择左侧连接。" />);
    expect(screen.getByText("暂无内容")).toBeInTheDocument();
    expect(screen.getByText("请选择左侧连接。")).toBeInTheDocument();
  });
});
