import type { ConnectionProfile } from "../../../shared/models/connection";
import type { TableSchema } from "../../../shared/models/schema";

export const schemaApi = {
  getSchema(profile: ConnectionProfile, tableName: string): Promise<TableSchema> {
    return window.electron.ipcRenderer.invoke("schema:get", {
      profile,
      tableName
    });
  }
};
