export class SQLiteClient {
  constructor(private readonly filePath: string) {}

  get path() {
    return this.filePath;
  }
}
