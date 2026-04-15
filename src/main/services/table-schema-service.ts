import type { ConnectionProfile } from "../../shared/models/connection";
import type { TableSchema } from "../../shared/models/schema";
import { resolveAdapter } from "../database/adapters/adapter-factory";

export class TableSchemaService {
  getSchema(profile: ConnectionProfile, tableName: string): Promise<TableSchema> {
    return resolveAdapter(profile.type).getTableSchema(profile, tableName);
  }
}
