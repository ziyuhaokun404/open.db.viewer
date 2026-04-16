import { useEffect } from "react";
import type { SystemThemeSnapshot } from "../../shared/models/system-theme";

export function applySystemTheme(
  snapshot: SystemThemeSnapshot,
  root: HTMLElement = document.documentElement
) {
  root.dataset.theme = snapshot.mode;
  root.style.colorScheme = snapshot.mode;
  root.style.setProperty("--accent", snapshot.accent);
  root.style.setProperty("--accent-hover", snapshot.accentHover);
  root.style.setProperty("--accent-pressed", snapshot.accentPressed);
  root.style.setProperty("--accent-soft", snapshot.accentSoft);
  root.style.setProperty("--accent-focus-ring", snapshot.accentFocusRing);
}

export function ThemeSync() {
  useEffect(() => {
    let disposed = false;
    const unsubscribe = window.electron.theme.onThemeChange((snapshot) => {
      applySystemTheme(snapshot);
    });

    void window.electron.theme.getThemeSnapshot().then((snapshot) => {
      if (!disposed) {
        applySystemTheme(snapshot);
      }
    });

    return () => {
      disposed = true;
      unsubscribe();
    };
  }, []);

  return null;
}
