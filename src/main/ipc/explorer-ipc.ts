import { ipcMain } from "electron";
import type { ConnectionProfile } from "../../shared/models/connection";
import { ExplorerService } from "../services/explorer-service";

const explorerService = new ExplorerService();

export function registerExplorerIpc() {
  ipcMain.handle("explorer:load", (_, profile: ConnectionProfile) => {
    return explorerService.loadTree(profile);
  });
}
