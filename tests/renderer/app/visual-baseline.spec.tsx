import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

function VisualProbe() {
  return <div data-testid="visual-probe">Open DB Viewer</div>;
}

describe("visual baseline", () => {
  it("renders probe node for style bootstrap", () => {
    render(<VisualProbe />);
    expect(screen.getByTestId("visual-probe")).toBeInTheDocument();
  });
});
