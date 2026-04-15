export type DatabaseType = "mysql" | "postgresql" | "sqlite";

export interface ConnectionProfile {
  id: string;
  type: DatabaseType;
  name: string;
  host?: string;
  port?: number;
  username?: string;
  password?: string;
  database?: string;
  filePath?: string;
}

export interface TestConnectionResult {
  success: boolean;
  message: string;
}
