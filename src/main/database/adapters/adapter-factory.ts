import type { DatabaseType } from "../../../shared/models/connection";
import type { DatabaseAdapter } from "./database-adapter";
import { MySQLAdapter } from "./mysql-adapter";
import { PostgreSQLAdapter } from "./postgresql-adapter";
import { SQLiteAdapter } from "./sqlite-adapter";

const mysqlAdapter = new MySQLAdapter();
const postgresqlAdapter = new PostgreSQLAdapter();
const sqliteAdapter = new SQLiteAdapter();

export function resolveAdapter(type: DatabaseType): DatabaseAdapter {
  if (type === "mysql") {
    return mysqlAdapter;
  }

  if (type === "postgresql") {
    return postgresqlAdapter;
  }

  return sqliteAdapter;
}
