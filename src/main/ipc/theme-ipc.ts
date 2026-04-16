import { BrowserWindow, ipcMain } from "electron";
import type { SystemThemeSnapshot } from "../../shared/models/system-theme";
import type { SystemThemeService } from "../services/system-theme-service";

export function registerThemeIpc(themeService: SystemThemeService) {
  ipcMain.handle("theme:get-snapshot", () => {
    return themeService.getSnapshot();
  });

  themeService.subscribe((snapshot: SystemThemeSnapshot) => {
    for (const win of BrowserWindow.getAllWindows()) {
      if (!win.isDestroyed()) {
        win.webContents.send("theme:changed", snapshot);
      }
    }
  });
}
