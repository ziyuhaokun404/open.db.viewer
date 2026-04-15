import { ipcMain } from "electron";
import type { ConnectionProfile } from "../../shared/models/connection";
import { TableDataService } from "../services/table-data-service";

const tableDataService = new TableDataService();

export function registerTableDataIpc() {
  ipcMain.handle(
    "table-data:get",
    (
      _,
      payload: {
        profile: ConnectionProfile;
        tableName: string;
        page: number;
        pageSize: number;
      }
    ) => {
      return tableDataService.getTableData(
        payload.profile,
        payload.tableName,
        payload.page,
        payload.pageSize
      );
    }
  );
}
