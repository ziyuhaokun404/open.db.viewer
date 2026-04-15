import type { ConnectionProfile, TestConnectionResult } from "../../shared/models/connection";
import { resolveAdapter } from "../database/adapters/adapter-factory";

export class ConnectionService {
  async testConnection(profile: ConnectionProfile): Promise<TestConnectionResult> {
    return resolveAdapter(profile.type).testConnection(profile);
  }
}
