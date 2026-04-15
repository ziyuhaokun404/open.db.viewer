import { describe, expect, it } from "vitest";
import { SQLiteAdapter } from "../../../src/main/database/adapters/sqlite-adapter";

describe("SQLiteAdapter", () => {
  it("returns a failed test result when file path is missing", async () => {
    const adapter = new SQLiteAdapter();

    await expect(
      adapter.testConnection({
        id: "sqlite-1",
        type: "sqlite",
        name: "Local SQLite"
      })
    ).resolves.toEqual({
      success: false,
      message: "SQLite file path is required."
    });
  });

  it("returns a success result when file path is provided", async () => {
    const adapter = new SQLiteAdapter();

    await expect(
      adapter.testConnection({
        id: "sqlite-1",
        type: "sqlite",
        name: "Local SQLite",
        filePath: "demo.db"
      })
    ).resolves.toEqual({
      success: true,
      message: "Connection successful."
    });
  });
});
