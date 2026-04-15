import type { ConnectionProfile, TestConnectionResult } from "../../../shared/models/connection";
import type { DatabaseObjectNode } from "../../../shared/models/database-object";
import type { QueryRequest, QueryResult } from "../../../shared/models/query";
import type { TableSchema } from "../../../shared/models/schema";
import type { DatabaseAdapter, TableDataPage } from "./database-adapter";

export class MySQLAdapter implements DatabaseAdapter {
  async testConnection(config: ConnectionProfile): Promise<TestConnectionResult> {
    if (!config.host || !config.username) {
      return {
        success: false,
        message: "MySQL host and username are required."
      };
    }

    return {
      success: true,
      message: "MySQL connection configuration looks valid."
    };
  }

  async listObjects(config: ConnectionProfile): Promise<DatabaseObjectNode[]> {
    const databaseName = config.database ?? "mysql";

    return [
      {
        id: `database:${databaseName}`,
        kind: "database",
        name: databaseName,
        children: [
          {
            id: `table:${databaseName}.users`,
            kind: "table",
            name: "users",
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
          name: "id",
          dataType: "bigint",
          nullable: false,
          defaultValue: null,
          isPrimaryKey: true
        },
        {
          name: "email",
          dataType: "varchar",
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
      columns: ["id", "name"],
      rows: [
        {
          id: 1,
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
      columns: ["query"],
      rows: [{ query: request.sql }],
      rowCount: 1,
      durationMs: 10
    };
  }
}
