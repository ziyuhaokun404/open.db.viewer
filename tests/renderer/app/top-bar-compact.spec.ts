import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

describe("TopBar compact sizing", () => {
  it("keeps the shell title bar visually compact", () => {
    const cssPath = resolve(process.cwd(), "src/renderer/app/app-layout.css");
    const css = readFileSync(cssPath, "utf8");

    expect(css).toContain("min-height: 34px");
    expect(css).toContain("padding: 0 134px 0 10px");
  });
});
