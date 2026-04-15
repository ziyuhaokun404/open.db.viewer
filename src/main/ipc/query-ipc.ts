import { ipcMain } from "electron";
import type { ConnectionProfile } from "../../shared/models/connection";
import type { QueryRequest } from "../../shared/models/query";
import { QueryService } from "../services/query-service";

const queryService = new QueryService();

export function registerQueryIpc() {
  ipcMain.handle(
    "query:execute",
    (_, payload: { profile: ConnectionProfile; request: QueryRequest }) => {
      return queryService.execute(payload.profile, payload.request);
    }
  );
}
