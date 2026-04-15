import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

describe("window frame config", () => {
  it("uses native title bar overlay instead of custom window IPC buttons", () => {
    const sourcePath = resolve(process.cwd(), "src/main/index.ts");
    const source = readFileSync(sourcePath, "utf8");

    expect(source).toContain('titleBarStyle: "hidden"');
    expect(source).toContain("titleBarOverlay:");
    expect(source).not.toContain("frame: false");
    expect(source).not.toContain('ipcMain.handle("window:minimize"');
  });

  it("keeps native title bar overlay compact", () => {
    const sourcePath = resolve(process.cwd(), "src/main/index.ts");
    const source = readFileSync(sourcePath, "utf8");

    expect(source).toContain("height: 34");
    expect(source).toContain('color: "#eef2f5"');
  });
});
