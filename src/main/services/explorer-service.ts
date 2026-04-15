import type { ConnectionProfile } from "../../shared/models/connection";
import type { DatabaseObjectNode } from "../../shared/models/database-object";
import { resolveAdapter } from "../database/adapters/adapter-factory";

export class ExplorerService {
  loadTree(profile: ConnectionProfile): Promise<DatabaseObjectNode[]> {
    return resolveAdapter(profile.type).listObjects(profile);
  }
}
