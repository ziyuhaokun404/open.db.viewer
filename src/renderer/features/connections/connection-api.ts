import type { ConnectionProfile, TestConnectionResult } from "../../../shared/models/connection";

export const connectionApi = {
  listConnections(): Promise<ConnectionProfile[]> {
    return window.electron.ipcRenderer.invoke("connection:list");
  },
  testConnection(profile: ConnectionProfile): Promise<TestConnectionResult> {
    return window.electron.ipcRenderer.invoke("connection:test", profile);
  },
  saveConnection(profile: ConnectionProfile): Promise<void> {
    return window.electron.ipcRenderer.invoke("connection:save", profile);
  },
  deleteConnection(id: string): Promise<void> {
    return window.electron.ipcRenderer.invoke("connection:delete", id);
  }
};
