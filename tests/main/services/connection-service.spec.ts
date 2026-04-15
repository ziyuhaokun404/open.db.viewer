import { describe, expect, it } from "vitest";
import { ConnectionService } from "../../../src/main/services/connection-service";

describe("ConnectionService", () => {
  it("fails sqlite test connection without file path", async () => {
    const service = new ConnectionService();

    await expect(
      service.testConnection({
        id: "sqlite-1",
        type: "sqlite",
        name: "Local SQLite"
      })
    ).resolves.toEqual({
      success: false,
      message: "SQLite file path is required."
    });
  });

  it("validates required mysql fields through the adapter factory", async () => {
    const service = new ConnectionService();

    await expect(
      service.testConnection({
        id: "mysql-1",
        type: "mysql",
        name: "Local MySQL"
      })
    ).resolves.toEqual({
      success: false,
      message: "MySQL host and username are required."
    });
  });
});
