import type { ConnectionProfile } from "../../../shared/models/connection";
import type { QueryRequest, QueryResult } from "../../../shared/models/query";

export const queryApi = {
  execute(profile: ConnectionProfile, request: QueryRequest): Promise<QueryResult> {
    return window.electron.ipcRenderer.invoke("query:execute", {
      profile,
      request
    });
  }
};
