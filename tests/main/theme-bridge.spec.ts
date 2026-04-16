import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

describe("theme bridge", () => {
  it("registers theme snapshot IPC in the main process", () => {
    const source = readFileSync(resolve(process.cwd(), "src/main/ipc/theme-ipc.ts"), "utf8");

    expect(source).toContain('ipcMain.handle("theme:get-snapshot"');
    expect(source).toContain('win.webContents.send("theme:changed"');
  });

  it("exposes a theme API to the renderer preload and global window types", () => {
    const preload = readFileSync(resolve(process.cwd(), "src/main/preload.ts"), "utf8");
    const globals = readFileSync(resolve(process.cwd(), "src/renderer/types/global.d.ts"), "utf8");

    expect(preload).toContain("getThemeSnapshot()");
    expect(preload).toContain("onThemeChange");
    expect(globals).toContain("getThemeSnapshot(): Promise<SystemThemeSnapshot>");
    expect(globals).toContain("onThemeChange");
  });
});
