import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

describe("TopBar layout CSS", () => {
  it("uses a dedicated drag region instead of making the whole title bar draggable", () => {
    const cssPath = resolve(process.cwd(), "src/renderer/app/app-layout.css");
    const css = readFileSync(cssPath, "utf8");

    const topBarRule = css.match(/\.top-bar\s*\{[\s\S]*?\}/)?.[0] ?? "";
    const dragRegionRule = css.match(/\.top-bar__drag-region\s*\{[\s\S]*?\}/)?.[0] ?? "";

    expect(topBarRule).not.toContain("-webkit-app-region: drag");
    expect(dragRegionRule).toContain("-webkit-app-region: drag");
  });

  it("uses a soft fade and divider to blend into native window controls", () => {
    const cssPath = resolve(process.cwd(), "src/renderer/app/app-layout.css");
    const css = readFileSync(cssPath, "utf8");

    const topBarRule = css.match(/\.top-bar\s*\{[\s\S]*?\}/)?.[0] ?? "";
    const afterRule = css.match(/\.top-bar::after\s*\{[\s\S]*?\}/)?.[0] ?? "";

    expect(topBarRule).toContain("padding: 0 134px 0 10px");
    expect(topBarRule).toContain("rgba(238, 242, 245, 0.92)");
    expect(afterRule).toContain("linear-gradient(90deg");
    expect(afterRule).toContain("right: 0");
  });
});
