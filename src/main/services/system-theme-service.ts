import type { BrowserWindow, NativeTheme, SystemPreferences } from "electron";
import type { SystemThemeSnapshot } from "../../shared/models/system-theme";

const FALLBACK_ACCENT = "#0f6cbd";

export interface BuildSystemThemeSnapshotOptions {
  platform: NodeJS.Platform;
  shouldUseDarkColors: boolean;
  accentColor?: string | null;
}

interface SystemThemeServiceDependencies {
  platform: NodeJS.Platform;
  nativeTheme: Pick<NativeTheme, "shouldUseDarkColors" | "on">;
  systemPreferences: Pick<SystemPreferences, "getAccentColor" | "on">;
}

type ThemeWindow = Pick<BrowserWindow, "setTitleBarOverlay">;
type ThemeListener = (snapshot: SystemThemeSnapshot) => void;

function normalizeAccentColor(input?: string | null): string {
  const value = (input ?? "").trim().replace(/^#/, "");

  if (value.length === 6) {
    return `#${value.toLowerCase()}`;
  }

  if (value.length === 8) {
    return `#${value.slice(0, 6).toLowerCase()}`;
  }

  return FALLBACK_ACCENT;
}

function channel(hex: string, start: number) {
  return Number.parseInt(hex.slice(start, start + 2), 16);
}

function mixChannel(source: number, target: number, ratio: number) {
  return Math.round(source + (target - source) * ratio);
}

function toHex(value: number) {
  return value.toString(16).padStart(2, "0");
}

function mixHex(hex: string, target: "black" | "white", ratio: number) {
  const normalized = normalizeAccentColor(hex).replace("#", "");
  const targetValue = target === "white" ? 255 : 0;

  return `#${toHex(mixChannel(channel(normalized, 0), targetValue, ratio))}${toHex(
    mixChannel(channel(normalized, 2), targetValue, ratio)
  )}${toHex(mixChannel(channel(normalized, 4), targetValue, ratio))}`;
}

function buildAccentHover(hex: string, mode: "light" | "dark") {
  if (mode === "dark") {
    return mixHex(hex, "white", 0.12);
  }

  const normalized = normalizeAccentColor(hex).replace("#", "");

  return `#${toHex(mixChannel(channel(normalized, 0), 255, 0.176))}${toHex(
    mixChannel(channel(normalized, 2), 255, 0.244)
  )}${toHex(mixChannel(channel(normalized, 4), 255, 0.319))}`;
}

function toRgba(hex: string, alpha: number) {
  const normalized = normalizeAccentColor(hex).replace("#", "");

  return `rgba(${channel(normalized, 0)}, ${channel(normalized, 2)}, ${channel(normalized, 4)}, ${alpha})`;
}

export function buildSystemThemeSnapshot(
  options: BuildSystemThemeSnapshotOptions
): SystemThemeSnapshot {
  const mode = options.shouldUseDarkColors ? "dark" : "light";
  const accent = normalizeAccentColor(options.accentColor);

  return {
    mode,
    accent,
    accentHover: buildAccentHover(accent, mode),
    accentPressed: mixHex(accent, "black", mode === "light" ? 0.2 : 0.28),
    accentSoft: toRgba(accent, mode === "light" ? 0.14 : 0.24),
    accentFocusRing: toRgba(accent, mode === "light" ? 0.28 : 0.34),
    titleBarColor: mode === "light" ? "#f5f5f5" : "#1f1f1f",
    titleBarSymbolColor: mode === "light" ? "#1a1a1a" : "#f5f5f5"
  };
}

export class SystemThemeService {
  private readonly listeners = new Set<ThemeListener>();
  private readonly windows = new Set<ThemeWindow>();

  constructor(private readonly dependencies: SystemThemeServiceDependencies) {}

  getSnapshot(): SystemThemeSnapshot {
    return buildSystemThemeSnapshot({
      platform: this.dependencies.platform,
      shouldUseDarkColors: this.dependencies.nativeTheme.shouldUseDarkColors,
      accentColor:
        this.dependencies.platform === "win32"
          ? this.dependencies.systemPreferences.getAccentColor()
          : FALLBACK_ACCENT
    });
  }

  start() {
    this.dependencies.nativeTheme.on("updated", () => this.emitChange());

    if (this.dependencies.platform === "win32") {
      this.dependencies.systemPreferences.on("accent-color-changed", () => this.emitChange());
    }
  }

  subscribe(listener: ThemeListener) {
    this.listeners.add(listener);

    return () => {
      this.listeners.delete(listener);
    };
  }

  attachWindow(window: ThemeWindow) {
    this.windows.add(window);
    this.applyWindowOverlay(window, this.getSnapshot());
  }

  private emitChange() {
    const snapshot = this.getSnapshot();

    for (const window of this.windows) {
      this.applyWindowOverlay(window, snapshot);
    }

    for (const listener of this.listeners) {
      listener(snapshot);
    }
  }

  private applyWindowOverlay(window: ThemeWindow, snapshot: SystemThemeSnapshot) {
    window.setTitleBarOverlay({
      color: snapshot.titleBarColor,
      symbolColor: snapshot.titleBarSymbolColor,
      height: 34
    });
  }
}
