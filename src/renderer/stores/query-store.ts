import { create } from "zustand";
import type { ConnectionProfile } from "../../shared/models/connection";
import type { QueryResult } from "../../shared/models/query";
import { queryApi } from "../features/query-view/query-api";

interface QueryState {
  sqlText: string;
  result: QueryResult | null;
  isExecuting: boolean;
  error: string;
  updateSqlText: (sqlText: string) => void;
  execute: (profile: ConnectionProfile) => Promise<void>;
  reset: () => void;
}

export const useQueryStore = create<QueryState>((set, get) => ({
  sqlText: "select * from sqlite_master;",
  result: null,
  isExecuting: false,
  error: "",
  updateSqlText(sqlText) {
    set({ sqlText });
  },
  async execute(profile) {
    set({ isExecuting: true, error: "" });

    try {
      const result = await queryApi.execute(profile, {
        sql: get().sqlText
      });
      set({ result, isExecuting: false });
    } catch {
      set({ result: null, isExecuting: false, error: "查询执行失败" });
    }
  },
  reset() {
    set({
      sqlText: "select * from sqlite_master;",
      result: null,
      isExecuting: false,
      error: ""
    });
  }
}));
