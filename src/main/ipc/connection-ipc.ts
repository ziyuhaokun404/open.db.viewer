import { ipcMain } from "electron";
import type { ConnectionProfile } from "../../shared/models/connection";
import { ConnectionService } from "../services/connection-service";
import { ConnectionStorageService } from "../services/connection-storage-service";

const connectionService = new ConnectionService();
const connectionStorageService = new ConnectionStorageService();

export function registerConnectionIpc() {
  ipcMain.handle("connection:test", (_, profile: ConnectionProfile) => {
    return connectionService.testConnection(profile);
  });
  ipcMain.handle("connection:list", () => {
    return connectionStorageService.listSavedConnections();
  });
  ipcMain.handle("connection:save", (_, profile: ConnectionProfile) => {
    return connectionStorageService.saveConnection(profile);
  });
  ipcMain.handle("connection:delete", (_, id: string) => {
    return connectionStorageService.deleteConnection(id);
  });
}
