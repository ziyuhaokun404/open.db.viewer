import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

describe("AppShell grid layout", () => {
  it("pins grid rows to content instead of stretching the title bar row", () => {
    const cssPath = resolve(process.cwd(), "src/renderer/app/app-layout.css");
    const css = readFileSync(cssPath, "utf8");

    expect(css).toContain("align-content: start");
    expect(css).toContain("grid-template-rows: auto minmax(0, 1fr) auto");
  });
});
