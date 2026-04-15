import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { HomePage } from "../../../src/renderer/pages/home-page";
import { useConnectionStore } from "../../../src/renderer/stores/connection-store";

beforeEach(() => {
  useConnectionStore.getState().reset();
});

describe("HomePage layout", () => {
  it("renders workspace hero and connection sidebar sections", async () => {
    render(<HomePage />);

    expect(await screen.findByText("Open DB Viewer")).toBeInTheDocument();
    expect(screen.getByText("已保存连接")).toBeInTheDocument();
  });
});
