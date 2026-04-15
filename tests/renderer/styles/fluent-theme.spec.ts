import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

describe("Fluent theme", () => {
  it("defines fluent-style tokens for mica and acrylic surfaces", () => {
    const tokenPath = resolve(process.cwd(), "src/renderer/styles/design-tokens.css");
    const tokens = readFileSync(tokenPath, "utf8");

    expect(tokens).toContain("--mica-base:");
    expect(tokens).toContain("--acrylic-panel:");
    expect(tokens).toContain("--stroke-soft:");
  });

  it("uses a fluent desktop background instead of the previous radial poster gradients", () => {
    const basePath = resolve(process.cwd(), "src/renderer/styles/base.css");
    const baseCss = readFileSync(basePath, "utf8");

    expect(baseCss).toContain("linear-gradient(180deg, #f3f3f3 0%, #eceff3 100%)");
    expect(baseCss).not.toContain("radial-gradient(circle at top left");
  });
});
