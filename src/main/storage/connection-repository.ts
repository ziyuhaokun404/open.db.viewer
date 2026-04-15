import { join } from "node:path";
import type { ConnectionProfile } from "../../shared/models/connection";
import { ConfigStore } from "./config-store";

interface ConnectionStoreShape {
  connections: ConnectionProfile[];
}

export class ConnectionRepository {
  private readonly store: ConfigStore<ConnectionStoreShape>;

  constructor(filePath = join(process.cwd(), ".open-db-viewer", "connections.json")) {
    this.store = new ConfigStore(filePath, { connections: [] });
  }

  async list(): Promise<ConnectionProfile[]> {
    const data = await this.store.read();
    return data.connections;
  }

  async save(profile: ConnectionProfile): Promise<void> {
    const data = await this.store.read();
    const index = data.connections.findIndex((item) => item.id === profile.id);

    if (index >= 0) {
      data.connections[index] = profile;
    } else {
      data.connections.push(profile);
    }

    await this.store.write(data);
  }

  async remove(id: string): Promise<void> {
    const data = await this.store.read();
    await this.store.write({
      connections: data.connections.filter((item) => item.id !== id)
    });
  }
}
