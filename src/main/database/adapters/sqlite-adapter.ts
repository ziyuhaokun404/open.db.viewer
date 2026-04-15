import { basename } from "node:path";
import type { ConnectionProfile, TestConnectionResult } from "../../../shared/models/connection";
import type { DatabaseObjectNode } from "../../../shared/models/database-object";
import type { QueryRequest, QueryResult } from "../../../shared/models/query";
import type { TableSchema } from "../../../shared/models/schema";
import type { DatabaseAdapter, TableDataPage } from "./database-adapter";
import { SQLiteClient } from "../drivers/sqlite-client";

export class SQLiteAdapter implements DatabaseAdapter {
  async testConnection(config: ConnectionProfile): Promise<TestConnectionResult> {
    if (!config.filePath) {
      return {
        success: false,
        message: "SQLite file path is required."
      };
    }

    void new SQLiteClient(config.filePath);

    return {
      success: true,
      message: "Connection successful."
    };
  }

  async listObjects(config: ConnectionProfile): Promise<DatabaseObjectNode[]> {
    const databaseName = config.filePath ? basename(config.filePath) : "sqlite";

    return [
      {
        id: `database:${databaseName}`,
        kind: "database",
        name: databaseName,
        children: [
          {
            id: "table:sqlite_master",
            kind: "table",
            name: "sqlite_master",
            parentId: `database:${databaseName}`
          }
        ]
      }
    ];
  }

  async getTableSchema(_: ConnectionProfile, tableName: string): Promise<TableSchema> {
    return {
      tableName,
      columns: [
        {
          name: "type",
          dataType: "text",
          nullable: false,
          defaultValue: null,
          isPrimaryKey: false
        },
        {
          name: "name",
          dataType: "text",
          nullable: false,
          defaultValue: null,
          isPrimaryKey: false
        }
      ]
    };
  }

  async getTableData(
    _: ConnectionProfile,
    tableName: string,
    page: number,
    pageSize: number
  ): Promise<TableDataPage> {
    return {
      columns: ["type", "name"],
      rows: [
        {
          type: "table",
          name: tableName
        }
      ],
      page,
      pageSize,
      hasNextPage: false
    };
  }

  async executeQuery(_: ConnectionProfile, request: QueryRequest): Promise<QueryResult> {
    return {
      columns: ["type", "name"],
      rows: [
        {
          type: "table",
          name: request.sql.includes("sqlite_master") ? "sqlite_master" : "result"
        }
      ],
      rowCount: 1,
      durationMs: 12
    };
  }
}
