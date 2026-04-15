import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { QueryResultGrid } from "../../../src/renderer/features/query-view/query-result-grid";

describe("QueryResultGrid", () => {
  it("renders fluent result shells when rows are present", () => {
    render(
      <QueryResultGrid
        result={{
          columns: ["id", "name"],
          rows: [{ id: 1, name: "users" }],
          rowCount: 1,
          durationMs: 9
        }}
        onExport={vi.fn()}
      />
    );

    expect(screen.getByTestId("query-result-shell")).toBeInTheDocument();
    expect(screen.getByTestId("query-result-toolbar")).toBeInTheDocument();
    expect(screen.getByTestId("query-result-table")).toBeInTheDocument();
  });
});
