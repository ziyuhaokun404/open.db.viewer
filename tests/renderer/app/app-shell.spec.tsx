import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { AppShell } from "../../../src/renderer/app/app-shell";

describe("AppShell", () => {
  it("renders top bar and main area", () => {
    render(
      <AppShell sidebar={<div>侧栏</div>}>
        <div>主区域</div>
      </AppShell>
    );

    expect(screen.getByText("Open DB Viewer")).toBeInTheDocument();
    expect(screen.getByText("侧栏")).toBeInTheDocument();
    expect(screen.getByText("主区域")).toBeInTheDocument();
  });
});
