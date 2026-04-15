import { describe, expect, it } from "vitest";
import { resolveAdapter } from "../../../src/main/database/adapters/adapter-factory";

describe("adapter selection", () => {
  it("supports mysql and postgresql types", () => {
    expect(resolveAdapter("mysql").constructor.name).toBe("MySQLAdapter");
    expect(resolveAdapter("postgresql").constructor.name).toBe("PostgreSQLAdapter");
    expect(resolveAdapter("sqlite").constructor.name).toBe("SQLiteAdapter");
  });
});
