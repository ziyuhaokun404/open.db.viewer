import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { TopBar } from "../../../src/renderer/components/top-bar";

describe("TopBar", () => {
  it("renders compact brand-only title bar", () => {
    render(<TopBar />);

    expect(screen.getByText("Open DB Viewer")).toBeInTheDocument();
  });

  it("keeps title bar free of page-level actions", () => {
    render(<TopBar />);

    expect(screen.getByTestId("top-bar-brand")).toBeInTheDocument();
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });
});
