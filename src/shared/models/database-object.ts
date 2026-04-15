export type DatabaseObjectKind = "connection" | "database" | "schema" | "table" | "view";

export interface DatabaseObjectNode {
  id: string;
  kind: DatabaseObjectKind;
  name: string;
  parentId?: string;
  children?: DatabaseObjectNode[];
}
