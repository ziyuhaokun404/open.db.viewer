import { ipcMain } from "electron";
import type { ConnectionProfile } from "../../shared/models/connection";
import { TableSchemaService } from "../services/table-schema-service";

const tableSchemaService = new TableSchemaService();

export function registerSchemaIpc() {
  ipcMain.handle("schema:get", (_, payload: { profile: ConnectionProfile; tableName: string }) => {
    return tableSchemaService.getSchema(payload.profile, payload.tableName);
  });
}
