import { render, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { SystemThemeSnapshot } from "../../../src/shared/models/system-theme";
import { ThemeSync } from "../../../src/renderer/app/theme-sync";

const lightSnapshot: SystemThemeSnapshot = {
  mode: "light",
  accent: "#112233",
  accentHover: "#445566",
  accentPressed: "#0f1f2f",
  accentSoft: "rgba(17, 34, 51, 0.14)",
  accentFocusRing: "rgba(17, 34, 51, 0.28)",
  titleBarColor: "#f5f5f5",
  titleBarSymbolColor: "#1a1a1a"
};

const darkSnapshot: SystemThemeSnapshot = {
  mode: "dark",
  accent: "#8899aa",
  accentHover: "#9caab8",
  accentPressed: "#5f6f7f",
  accentSoft: "rgba(136, 153, 170, 0.24)",
  accentFocusRing: "rgba(136, 153, 170, 0.34)",
  titleBarColor: "#1f1f1f",
  titleBarSymbolColor: "#f5f5f5"
};

describe("ThemeSync", () => {
  it("applies the initial snapshot to the document root", async () => {
    window.electron.theme.getThemeSnapshot = vi.fn().mockResolvedValue(lightSnapshot);
    window.electron.theme.onThemeChange = vi.fn().mockReturnValue(() => {});

    render(<ThemeSync />);

    await waitFor(() => {
      expect(document.documentElement.dataset.theme).toBe("light");
    });

    expect(document.documentElement.style.getPropertyValue("--accent")).toBe("#112233");
    expect(document.documentElement.style.colorScheme).toBe("light");
  });

  it("updates root variables when a theme change event arrives", async () => {
    let listener: ((snapshot: SystemThemeSnapshot) => void) | undefined;

    window.electron.theme.getThemeSnapshot = vi.fn().mockResolvedValue(lightSnapshot);
    window.electron.theme.onThemeChange = vi.fn().mockImplementation((nextListener) => {
      listener = nextListener;
      return () => {};
    });

    render(<ThemeSync />);

    await waitFor(() => {
      expect(document.documentElement.dataset.theme).toBe("light");
    });

    listener?.(darkSnapshot);

    expect(document.documentElement.dataset.theme).toBe("dark");
    expect(document.documentElement.style.getPropertyValue("--accent")).toBe("#8899aa");
    expect(document.documentElement.style.colorScheme).toBe("dark");
  });
});
