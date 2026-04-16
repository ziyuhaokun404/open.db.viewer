import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

describe("Fluent theme", () => {
  it("defines neutral token families for both light and dark themes", () => {
    const tokenPath = resolve(process.cwd(), "src/renderer/styles/design-tokens.css");
    const tokens = readFileSync(tokenPath, "utf8");

    expect(tokens).toContain(':root[data-theme="light"]');
    expect(tokens).toContain(':root[data-theme="dark"]');
    expect(tokens).toContain("--color-bg-canvas:");
    expect(tokens).toContain("--color-surface-2:");
    expect(tokens).toContain("--accent-focus-ring:");
  });

  it("uses semantic accent variables in base styles instead of the fixed blue focus ring", () => {
    const basePath = resolve(process.cwd(), "src/renderer/styles/base.css");
    const baseCss = readFileSync(basePath, "utf8");

    expect(baseCss).toContain("color-scheme: light");
    expect(baseCss).toContain(':root[data-theme="dark"]');
    expect(baseCss).toContain("var(--accent-focus-ring)");
    expect(baseCss).toContain(
      "linear-gradient(180deg, var(--color-bg-canvas) 0%, var(--color-bg-shell) 100%)"
    );
    expect(baseCss).not.toContain("rgba(15, 108, 189, 0.42)");
    expect(baseCss).not.toContain("radial-gradient(circle at 12% 8%");
  });
});
