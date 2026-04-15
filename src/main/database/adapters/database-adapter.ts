import type { ConnectionProfile, TestConnectionResult } from "../../../shared/models/connection";
import type { DatabaseObjectNode } from "../../../shared/models/database-object";
import type { QueryRequest, QueryResult } from "../../../shared/models/query";
import type { TableSchema } from "../../../shared/models/schema";

export interface TableDataPage {
  columns: string[];
  rows: Array<Record<string, unknown>>;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
}

export interface DatabaseAdapter {
  testConnection(config: ConnectionProfile): Promise<TestConnectionResult>;
  listObjects(config: ConnectionProfile): Promise<DatabaseObjectNode[]>;
  getTableSchema(config: ConnectionProfile, tableName: string): Promise<TableSchema>;
  getTableData(
    config: ConnectionProfile,
    tableName: string,
    page: number,
    pageSize: number
  ): Promise<TableDataPage>;
  executeQuery(config: ConnectionProfile, request: QueryRequest): Promise<QueryResult>;
}
