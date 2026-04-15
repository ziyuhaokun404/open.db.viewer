export type AppErrorCode =
  | "VALIDATION_ERROR"
  | "CONNECTION_ERROR"
  | "AUTH_ERROR"
  | "QUERY_ERROR"
  | "EXPORT_ERROR"
  | "UNKNOWN_ERROR";

export class AppError extends Error {
  constructor(
    public code: AppErrorCode,
    message: string
  ) {
    super(message);
    this.name = "AppError";
  }
}
