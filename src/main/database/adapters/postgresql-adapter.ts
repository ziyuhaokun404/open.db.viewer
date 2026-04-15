import type { ConnectionProfile, TestConnectionResult } from "../../../shared/models/connection";
import type { DatabaseObjectNode } from "../../../shared/models/database-object";
import type { QueryRequest, QueryResult } from "../../../shared/models/query";
import type { TableSchema } from "../../../shared/models/schema";
import type { DatabaseAdapter, TableDataPage } from "./database-adapter";

export class PostgreSQLAdapter implements DatabaseAdapter {
  async testConnection(config: ConnectionProfile): Promise<TestConnectionResult> {
    if (!config.host || !config.username) {
      return {
        success: false,
        message: "PostgreSQL host and username are required."
      };
    }

    return {
      success: true,
      message: "PostgreSQL connection configuration looks valid."
    };
  }

  async listObjects(config: ConnectionProfile): Promise<DatabaseObjectNode[]> {
    const databaseName = config.database ?? "postgres";
    const schemaId = `schema:${databaseName}.public`;

    return [
      {
        id: `database:${databaseName}`,
        kind: "database",
        name: databaseName,
        children: [
          {
            id: schemaId,
            kind: "schema",
            name: "public",
            parentId: `database:${databaseName}`,
            children: [
              {
                id: `table:${databaseName}.public.users`,
                kind: "table",
                name: "users",
                parentId: schemaId
              }
            ]
          }
        ]
      }
    ];
  }

  async getTableSchema(_: ConnectionProfile, tableName: string): Promise<TableSchema> {
    return {
      tableName,
      schemaName: "public",
      columns: [
        {
          name: "id",
          dataType: "uuid",
          nullable: false,
          defaultValue: null,
          isPrimaryKey: true
        },
        {
          name: "created_at",
          dataType: "timestamp",
          nullable: false,
          defaultValue: "now()",
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
      columns: ["id", "table"],
      rows: [
        {
          id: "00000000-0000-0000-0000-000000000000",
          table: tableName
        }
      ],
      page,
      pageSize,
      hasNextPage: false
    };
  }

  async executeQuery(_: ConnectionProfile, request: QueryRequest): Promise<QueryResult> {
    return {
      columns: ["query"],
      rows: [{ query: request.sql }],
      rowCount: 1,
      durationMs: 11
    };
  }
}
