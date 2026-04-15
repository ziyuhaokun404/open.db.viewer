export interface QueryRequest {
  sql: string;
}

export interface QueryResult {
  columns: string[];
  rows: Array<Record<string, unknown>>;
  rowCount: number;
  durationMs: number;
  message?: string;
  error?: string;
}
