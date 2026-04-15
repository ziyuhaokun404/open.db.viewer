import type { ConnectionProfile } from "../../shared/models/connection";
import { ConnectionRepository } from "../storage/connection-repository";

export class ConnectionStorageService {
  constructor(private readonly repository = new ConnectionRepository()) {}

  listSavedConnections(): Promise<ConnectionProfile[]> {
    return this.repository.list();
  }

  saveConnection(profile: ConnectionProfile): Promise<void> {
    return this.repository.save(profile);
  }

  deleteConnection(id: string): Promise<void> {
    return this.repository.remove(id);
  }
}
