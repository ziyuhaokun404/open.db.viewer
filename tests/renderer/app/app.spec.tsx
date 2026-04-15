import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { App } from "../../../src/renderer/app/App";
import { useConnectionStore } from "../../../src/renderer/stores/connection-store";

beforeEach(() => {
  useConnectionStore.getState().reset();
});

describe("App", () => {
  it("renders product name", async () => {
    render(<App />);
    expect(await screen.findByText("Open DB Viewer")).toBeInTheDocument();
  });
});
