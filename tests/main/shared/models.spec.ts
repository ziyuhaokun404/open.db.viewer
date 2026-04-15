import { describe, expect, it } from "vitest";
import { AppError } from "../../../src/shared/errors/app-error";
import type { ConnectionProfile, DatabaseType, TestConnectionResult } from "../../../src/shared/models/connection";
import type { DatabaseObjectNode } from "../../../src/shared/models/database-object";
import type { QueryResult } from "../../../src/shared/models/query";
import type { TableSchema } from "../../../src/shared/models/schema";

describe("shared models", () => {
  it("supports supported database types in connection profiles", () => {
    const type: DatabaseType = "mysql";
    const profile: ConnectionProfile = {
      id: "conn-1",
      type,
      name: "Local MySQL",
      host: "127.0.0.1",
      port: 3306,
      username: "root",
      database: "app"
    };
    const result: TestConnectionResult = {
      success: true,
      message: "ok"
    };

    expect(profile.type).toBe("mysql");
    expect(result.success).toBe(true);
  });

  it("models database objects, schema data, and query results in a consistent shape", () => {
    const node: DatabaseObjectNode = {
      id: "table-users",
      kind: "table",
      name: "users"
    };
    const schema: TableSchema = {
      tableName: "users",
      columns: [
        {
          name: "id",
          dataType: "integer",
          nullable: false,
          defaultValue: null,
          isPrimaryKey: true
        }
      ]
    };
    const result: QueryResult = {
      columns: ["id"],
      rows: [{ id: 1 }],
      rowCount: 1,
      durationMs: 5
    };

    expect(node.kind).toBe("table");
    expect(schema.columns[0]?.isPrimaryKey).toBe(true);
    expect(result.rows[0]).toEqual({ id: 1 });
  });

  it("preserves structured application errors", () => {
    const error = new AppError("QUERY_ERROR", "bad sql");

    expect(error.name).toBe("AppError");
    expect(error.code).toBe("QUERY_ERROR");
    expect(error.message).toBe("bad sql");
  });
});
