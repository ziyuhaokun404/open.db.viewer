import type { ConnectionProfile } from "../../shared/models/connection";
import type { QueryRequest, QueryResult } from "../../shared/models/query";
import { resolveAdapter } from "../database/adapters/adapter-factory";

export class QueryService {
  execute(profile: ConnectionProfile, request: QueryRequest): Promise<QueryResult> {
    return resolveAdapter(profile.type).executeQuery(profile, request);
  }
}
