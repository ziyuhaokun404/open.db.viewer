import { mkdtemp, rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { afterEach, describe, expect, it } from "vitest";
import type { ConnectionProfile } from "../../../src/shared/models/connection";
import { ConnectionRepository } from "../../../src/main/storage/connection-repository";
import { ConnectionStorageService } from "../../../src/main/services/connection-storage-service";

const tempDirs: string[] = [];

async function createRepository() {
  const dir = await mkdtemp(join(tmpdir(), "odbv-"));
  tempDirs.push(dir);

  return new ConnectionRepository(join(dir, "connections.json"));
}

afterEach(async () => {
  await Promise.all(
    tempDirs.splice(0, tempDirs.length).map((dir) => rm(dir, { recursive: true, force: true }))
  );
});

describe("ConnectionRepository", () => {
  it("stores and returns saved connections", async () => {
    const repository = await createRepository();
    const profile: ConnectionProfile = {
      id: "sqlite-1",
      type: "sqlite",
      name: "Local SQLite",
      filePath: "demo.db"
    };

    await repository.save(profile);

    await expect(repository.list()).resolves.toEqual([profile]);
  });

  it("replaces an existing connection when ids match", async () => {
    const repository = await createRepository();

    await repository.save({
      id: "conn-1",
      type: "sqlite",
      name: "Old Name",
      filePath: "demo.db"
    });

    await repository.save({
      id: "conn-1",
      type: "sqlite",
      name: "New Name",
      filePath: "demo.db"
    });

    await expect(repository.list()).resolves.toEqual([
      {
        id: "conn-1",
        type: "sqlite",
        name: "New Name",
        filePath: "demo.db"
      }
    ]);
  });

  it("deletes saved connections by id through the storage service", async () => {
    const repository = await createRepository();
    const service = new ConnectionStorageService(repository);

    await service.saveConnection({
      id: "mysql-1",
      type: "mysql",
      name: "Local MySQL",
      host: "127.0.0.1",
      port: 3306
    });

    await service.deleteConnection("mysql-1");

    await expect(service.listSavedConnections()).resolves.toEqual([]);
  });
});
