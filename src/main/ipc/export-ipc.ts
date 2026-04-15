import { ipcMain } from "electron";
import { ExportService, type ExportCsvPayload } from "../services/export-service";

const exportService = new ExportService();

export function registerExportIpc() {
  ipcMain.handle("export:csv", (_, payload: ExportCsvPayload) => {
    return exportService.exportCsv(payload);
  });
}
