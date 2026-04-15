export class CsvWriter {
  toCsv(columns: string[], rows: Array<Record<string, unknown>>): string {
    const header = columns.join(",");
    const lines = rows.map((row) => columns.map((column) => String(row[column] ?? "")).join(","));

    return [header, ...lines].join("\n");
  }
}
