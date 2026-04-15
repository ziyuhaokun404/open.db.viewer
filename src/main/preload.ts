import { contextBridge, ipcRenderer } from "electron";

contextBridge.exposeInMainWorld("electron", {
  ipcRenderer: {
    invoke(channel: string, payload?: unknown) {
      return ipcRenderer.invoke(channel, payload);
    }
  }
});
