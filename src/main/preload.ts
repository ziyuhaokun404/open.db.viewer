import { contextBridge, ipcRenderer, type IpcRendererEvent } from "electron";
import type { SystemThemeSnapshot } from "../shared/models/system-theme";

contextBridge.exposeInMainWorld("electron", {
  ipcRenderer: {
    invoke(channel: string, payload?: unknown) {
      return ipcRenderer.invoke(channel, payload);
    }
  },
  theme: {
    getThemeSnapshot(): Promise<SystemThemeSnapshot> {
      return ipcRenderer.invoke("theme:get-snapshot");
    },
    onThemeChange(listener: (snapshot: SystemThemeSnapshot) => void) {
      const handler = (_event: IpcRendererEvent, snapshot: SystemThemeSnapshot) => {
        listener(snapshot);
      };

      ipcRenderer.on("theme:changed", handler);

      return () => {
        ipcRenderer.removeListener("theme:changed", handler);
      };
    }
  }
});
