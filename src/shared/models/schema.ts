export interface TableColumn {
  name: string;
  dataType: string;
  nullable: boolean;
  defaultValue?: string | null;
  isPrimaryKey: boolean;
}

export interface TableSchema {
  tableName: string;
  databaseName?: string;
  schemaName?: string;
  columns: TableColumn[];
}
