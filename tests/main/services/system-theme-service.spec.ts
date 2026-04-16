import { describe, expect, it } from "vitest";
import { buildSystemThemeSnapshot } from "../../../src/main/services/system-theme-service";

describe("SystemThemeService", () => {
  it("builds a light snapshot from a Windows accent color", () => {
    expect(
      buildSystemThemeSnapshot({
        platform: "win32",
        shouldUseDarkColors: false,
        accentColor: "#112233ff"
      })
    ).toMatchObject({
      mode: "light",
      accent: "#112233",
      accentHover: "#3b5874",
      accentPressed: "#0e1b29",
      accentSoft: "rgba(17, 34, 51, 0.14)",
      accentFocusRing: "rgba(17, 34, 51, 0.28)",
      titleBarColor: "#f5f5f5",
      titleBarSymbolColor: "#1a1a1a"
    });
  });

  it("falls back to Fluent blue when no usable accent color exists", () => {
    expect(
      buildSystemThemeSnapshot({
        platform: "linux",
        shouldUseDarkColors: true,
        accentColor: ""
      })
    ).toMatchObject({
      mode: "dark",
      accent: "#0f6cbd",
      titleBarColor: "#1f1f1f",
      titleBarSymbolColor: "#f5f5f5"
    });
  });
});
