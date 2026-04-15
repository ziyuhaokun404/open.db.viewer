import type { ConnectionProfile } from "../../shared/models/connection";
import type { TableDataPage } from "../database/adapters/database-adapter";
import { resolveAdapter } from "../database/adapters/adapter-factory";

export class TableDataService {
  getTableData(
    profile: ConnectionProfile,
    tableName: string,
    page: number,
    pageSize: number
  ): Promise<TableDataPage> {
    return resolveAdapter(profile.type).getTableData(profile, tableName, page, pageSize);
  }
}
