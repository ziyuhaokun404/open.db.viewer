import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

describe("Connection dialog visual rhythm", () => {
  it("uses a distinct header and action bar treatment for the modal form", () => {
    const cssPath = resolve(process.cwd(), "src/renderer/app/app-layout.css");
    const css = readFileSync(cssPath, "utf8");

    const overlayRule = css.match(/\.dialog-layer\s*\{[\s\S]*?\}/)?.[0] ?? "";
    const headRule = css.match(/\.connection-form__dialog-head\s*\{[\s\S]*?\}/)?.[0] ?? "";
    const closeRule = css.match(/\.connection-form__close\s*\{[\s\S]*?\}/)?.[0] ?? "";
    const panelRule = css.match(/\.connection-form__panel\s*\{[\s\S]*?\}/)?.[0] ?? "";
    const actionsRule = css.match(/\.connection-form__actions\s*\{[\s\S]*?\}/)?.[0] ?? "";

    expect(overlayRule).toContain("background: var(--overlay-scrim);");
    expect(overlayRule).not.toContain("var(--accent-soft)");
    expect(overlayRule).not.toContain("linear-gradient");
    expect(headRule).toContain("padding-bottom: 14px");
    expect(headRule).toContain("border-bottom: 1px solid");
    expect(closeRule).toContain("border-radius: var(--radius-sm)");
    expect(closeRule).toContain("height: 32px");
    expect(panelRule).toContain("padding: 16px");
    expect(actionsRule).toContain("padding-top: 12px");
    expect(actionsRule).toContain("justify-content: flex-end");
  });
});
