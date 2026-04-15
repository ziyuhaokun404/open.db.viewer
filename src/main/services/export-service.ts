import { writeFile } from "node:fs/promises";
import { join } from "node:path";
import { CsvWriter } from "../exports/csv-writer";

export interface ExportCsvPayload {
  fileName: string;
  columns: string[];
  rows: Array<Record<string, unknown>>;
}

export interface ExportCsvResult {
  success: boolean;
  message: string;
  filePath?: string;
}

export class ExportService {
  constructor(private readonly csvWriter = new CsvWriter()) {}

  async exportCsv(payload: ExportCsvPayload): Promise<ExportCsvResult> {
    const filePath = join(process.cwd(), payload.fileName);
    const csv = this.csvWriter.toCsv(payload.columns, payload.rows);

    await writeFile(filePath, csv, "utf-8");

    return {
      success: true,
      message: "CSV 导出成功",
      filePath
    };
  }
}
