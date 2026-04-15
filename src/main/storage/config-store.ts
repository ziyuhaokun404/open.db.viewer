import { mkdir, readFile, writeFile } from "node:fs/promises";
import { dirname } from "node:path";

export class ConfigStore<T extends object> {
  constructor(
    private readonly filePath: string,
    private readonly defaultValue: T
  ) {}

  async read(): Promise<T> {
    try {
      const content = await readFile(this.filePath, "utf-8");
      return JSON.parse(content) as T;
    } catch {
      return this.defaultValue;
    }
  }

  async write(value: T): Promise<void> {
    await mkdir(dirname(this.filePath), { recursive: true });
    await writeFile(this.filePath, JSON.stringify(value, null, 2), "utf-8");
  }
}
