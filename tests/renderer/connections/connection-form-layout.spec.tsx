import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { ConnectionForm } from "../../../src/renderer/features/connections/connection-form";
import { useConnectionStore } from "../../../src/renderer/stores/connection-store";

beforeEach(() => {
  useConnectionStore.getState().reset();
});

describe("ConnectionForm layout", () => {
  it("renders grouped sections and segmented type selector", () => {
    render(<ConnectionForm />);
    expect(screen.getByText("数据库类型")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "SQLite" })).toBeInTheDocument();
    expect(screen.getByText("连接信息")).toBeInTheDocument();
  });

  it("renders fluent form shells for content and actions", () => {
    render(<ConnectionForm />);

    expect(screen.getByTestId("connection-form-shell")).toBeInTheDocument();
    expect(screen.getByTestId("connection-form-actions")).toBeInTheDocument();
  });
});
