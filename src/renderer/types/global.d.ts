import type { ConnectionProfile, TestConnectionResult } from "../../shared/models/connection";
import type { DatabaseObjectNode } from "../../shared/models/database-object";
import type { ExportCsvPayload, ExportCsvResult } from "../../main/services/export-service";
import type { QueryRequest, QueryResult } from "../../shared/models/query";
import type { TableSchema } from "../../shared/models/schema";
import type { TableDataPage } from "../../main/database/adapters/database-adapter";

declare global {
  interface Window {
    electron: {
      ipcRenderer: {
        invoke(channel: "connection:list"): Promise<ConnectionProfile[]>;
        invoke(
          channel: "connection:test",
          payload: ConnectionProfile
        ): Promise<TestConnectionResult>;
        invoke(channel: "connection:save", payload: ConnectionProfile): Promise<void>;
        invoke(channel: "connection:delete", payload: string): Promise<void>;
        invoke(channel: "explorer:load", payload: ConnectionProfile): Promise<DatabaseObjectNode[]>;
        invoke(
          channel: "schema:get",
          payload: { profile: ConnectionProfile; tableName: string }
        ): Promise<TableSchema>;
        invoke(
          channel: "table-data:get",
          payload: {
            profile: ConnectionProfile;
            tableName: string;
            page: number;
            pageSize: number;
          }
        ): Promise<TableDataPage>;
        invoke(
          channel: "query:execute",
          payload: {
            profile: ConnectionProfile;
            request: QueryRequest;
          }
        ): Promise<QueryResult>;
        invoke(channel: "export:csv", payload: ExportCsvPayload): Promise<ExportCsvResult>;
        invoke(channel: string, payload?: unknown): Promise<unknown>;
      };
    };
  }
}

export {};
