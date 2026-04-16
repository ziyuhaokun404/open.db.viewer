import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

describe("Fluent shell token usage", () => {
  it("defines compact radius tokens for the refreshed shell", () => {
    const cssPath = resolve(process.cwd(), "src/renderer/styles/design-tokens.css");
    const css = readFileSync(cssPath, "utf8");

    expect(css).toContain("--radius-sm: 10px;");
    expect(css).toContain("--radius-md: 16px;");
    expect(css).toContain("--radius-lg: 22px;");
  });

  it("applies neutral surface tokens to shell panels, tabs, tables, and dialog chrome", () => {
    const cssPath = resolve(process.cwd(), "src/renderer/app/app-layout.css");
    const css = readFileSync(cssPath, "utf8");

    expect(css).toContain("background: var(--surface-2);");
    expect(css).toContain("border: 1px solid var(--border-subtle);");
    expect(css).toContain("color: var(--color-text-primary);");
    expect(css).toContain("background: var(--surface-3);");
    expect(css).not.toContain("rgba(244, 247, 251, 0.9)");
  });

  it("uses accent variables for selected tabs and primary actions", () => {
    const cssPath = resolve(process.cwd(), "src/renderer/app/app-layout.css");
    const css = readFileSync(cssPath, "utf8");

    expect(css).toContain("background: var(--accent-soft);");
    expect(css).toContain("border-color: var(--accent-focus-ring);");
    expect(css).toContain("background: linear-gradient(180deg, var(--accent), var(--accent-hover));");
    expect(css).not.toContain("background: linear-gradient(180deg, #0f6cbd 0%, #0b5aa1 100%);");
  });

  it("keeps primary actions accented and secondary actions neutral", () => {
    const cssPath = resolve(process.cwd(), "src/renderer/app/app-layout.css");
    const css = readFileSync(cssPath, "utf8");

    expect(css).toMatch(/\.connection-form__primary-action\s*\{[^}]*background:\s*var\(--accent\)/s);
    expect(css).toMatch(/\.connection-form__secondary-action\s*\{[^}]*background:\s*var\(--surface-3\)/s);
  });
});
