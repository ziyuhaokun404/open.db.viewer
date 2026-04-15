import { create } from "zustand";
import type { ConnectionProfile } from "../../shared/models/connection";
import type { TableSchema } from "../../shared/models/schema";
import { schemaApi } from "../features/schema-view/schema-api";

interface SchemaState {
  currentSchema: TableSchema | null;
  isLoading: boolean;
  error: string;
  loadSchema: (profile: ConnectionProfile, tableName: string) => Promise<void>;
  reset: () => void;
}

export const useSchemaStore = create<SchemaState>((set) => ({
  currentSchema: null,
  isLoading: false,
  error: "",
  async loadSchema(profile, tableName) {
    set({ isLoading: true, error: "" });

    try {
      const currentSchema = await schemaApi.getSchema(profile, tableName);
      set({ currentSchema, isLoading: false });
    } catch {
      set({ currentSchema: null, isLoading: false, error: "表结构加载失败" });
    }
  },
  reset() {
    set({ currentSchema: null, isLoading: false, error: "" });
  }
}));
