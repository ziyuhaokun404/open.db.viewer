import { app, BrowserWindow } from "electron";
import { join } from "node:path";
import { registerConnectionIpc } from "./ipc/connection-ipc";
import { registerExportIpc } from "./ipc/export-ipc";
import { registerExplorerIpc } from "./ipc/explorer-ipc";
import { registerQueryIpc } from "./ipc/query-ipc";
import { registerSchemaIpc } from "./ipc/schema-ipc";
import { registerTableDataIpc } from "./ipc/table-data-ipc";

function createWindow() {
  const win = new BrowserWindow({
    autoHideMenuBar: true,
    width: 1280,
    height: 820,
    minWidth: 1080,
    minHeight: 720,
    titleBarStyle: "hidden",
    titleBarOverlay: {
      color: "#eef2f5",
      symbolColor: "#41505d",
      height: 34
    },
    webPreferences: {
      contextIsolation: true,
      preload: join(__dirname, "../preload/preload.mjs")
    }
  });

  const devServerUrl = process.env.ELECTRON_RENDERER_URL;
  if (devServerUrl) {
    void win.loadURL(devServerUrl);
    return;
  }

  void win.loadFile(join(__dirname, "../renderer/index.html"));
}

app.whenReady().then(() => {
  registerConnectionIpc();
  registerExportIpc();
  registerExplorerIpc();
  registerQueryIpc();
  registerSchemaIpc();
  registerTableDataIpc();
  createWindow();

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});
