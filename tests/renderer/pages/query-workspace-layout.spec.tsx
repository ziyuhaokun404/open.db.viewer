import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { QueryEditor } from "../../../src/renderer/features/query-view/query-editor";
import { useQueryStore } from "../../../src/renderer/stores/query-store";

beforeEach(() => {
  useQueryStore.getState().reset();
});

describe("QueryEditor layout", () => {
  it("renders execute, clear, and compact fluent toolbar", () => {
    render(<QueryEditor onExecute={() => undefined} />);
    expect(screen.getByRole("button", { name: "执行" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "清空" })).toBeInTheDocument();
    expect(screen.getByText("Ready")).toBeInTheDocument();
    expect(screen.getByTestId("query-editor-toolbar")).toBeInTheDocument();
  });
});
