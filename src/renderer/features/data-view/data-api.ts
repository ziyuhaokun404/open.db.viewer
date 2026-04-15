import type { ConnectionProfile } from "../../../shared/models/connection";
import type { TableDataPage } from "../../../main/database/adapters/database-adapter";

export const dataApi = {
  getTableData(
    profile: ConnectionProfile,
    tableName: string,
    page: number,
    pageSize: number
  ): Promise<TableDataPage> {
    return window.electron.ipcRenderer.invoke("table-data:get", {
      profile,
      tableName,
      page,
      pageSize
    });
  }
};
