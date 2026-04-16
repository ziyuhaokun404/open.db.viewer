import "@testing-library/jest-dom/vitest";
import type { SystemThemeSnapshot } from "../src/shared/models/system-theme";

const defaultThemeSnapshot: SystemThemeSnapshot = {
  mode: "light",
  accent: "#0f6cbd",
  accentHover: "#3d84c8",
  accentPressed: "#0c5697",
  accentSoft: "rgba(15, 108, 189, 0.14)",
  accentFocusRing: "rgba(15, 108, 189, 0.28)",
  titleBarColor: "#f5f5f5",
  titleBarSymbolColor: "#1a1a1a"
};

const defaultInvoke = (async (channel: string) => {
  if (channel === "connection:test") {
    return {
      success: false,
      message: "Not implemented in test setup."
    };
  }

  if (channel === "theme:get-snapshot") {
    return defaultThemeSnapshot;
  }

  return [];
}) as Window["electron"]["ipcRenderer"]["invoke"];

window.electron = {
  ipcRenderer: {
    invoke: defaultInvoke
  },
  theme: {
    getThemeSnapshot: async () => defaultThemeSnapshot,
    onThemeChange: () => () => {}
  }
};
