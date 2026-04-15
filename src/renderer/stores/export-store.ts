import { create } from "zustand";
import { exportApi } from "../features/export/export-api";

interface ExportState {
  message: string;
  exportQueryResult: (columns: string[], rows: Array<Record<string, unknown>>) => Promise<void>;
  exportTableData: (columns: string[], rows: Array<Record<string, unknown>>) => Promise<void>;
  reset: () => void;
}

export const useExportStore = create<ExportState>((set) => ({
  message: "",
  async exportQueryResult(columns, rows) {
    const result = await exportApi.exportCsv({
      fileName: "query-result.csv",
      columns,
      rows
    });
    set({ message: result.message });
  },
  async exportTableData(columns, rows) {
    const result = await exportApi.exportCsv({
      fileName: "table-data.csv",
      columns,
      rows
    });
    set({ message: result.message });
  },
  reset() {
    set({ message: "" });
  }
}));
