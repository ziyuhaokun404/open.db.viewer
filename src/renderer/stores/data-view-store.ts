import { create } from "zustand";
import type { ConnectionProfile } from "../../shared/models/connection";
import type { TableDataPage } from "../../main/database/adapters/database-adapter";
import { dataApi } from "../features/data-view/data-api";

interface DataViewState {
  currentTableData: TableDataPage | null;
  isLoading: boolean;
  error: string;
  loadTableData: (
    profile: ConnectionProfile,
    tableName: string,
    page?: number,
    pageSize?: number
  ) => Promise<void>;
  reset: () => void;
}

export const useDataViewStore = create<DataViewState>((set) => ({
  currentTableData: null,
  isLoading: false,
  error: "",
  async loadTableData(profile, tableName, page = 1, pageSize = 50) {
    set({ isLoading: true, error: "" });

    try {
      const currentTableData = await dataApi.getTableData(profile, tableName, page, pageSize);
      set({ currentTableData, isLoading: false });
    } catch {
      set({ currentTableData: null, isLoading: false, error: "表数据加载失败" });
    }
  },
  reset() {
    set({ currentTableData: null, isLoading: false, error: "" });
  }
}));
