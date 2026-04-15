import type { ConnectionProfile } from "../../../shared/models/connection";
import type { DatabaseObjectNode } from "../../../shared/models/database-object";

export const explorerApi = {
  loadTree(profile: ConnectionProfile): Promise<DatabaseObjectNode[]> {
    return window.electron.ipcRenderer.invoke("explorer:load", profile);
  }
};
