import type { ExportCsvPayload, ExportCsvResult } from "../../../main/services/export-service";

export const exportApi = {
  exportCsv(payload: ExportCsvPayload): Promise<ExportCsvResult> {
    return window.electron.ipcRenderer.invoke("export:csv", payload);
  }
};
