namespace Open.Db.Viewer.Domain.Models;

public sealed record OperationResult(
    bool IsSuccess,
    string Message,
    string? ErrorCode = null)
{
    public static OperationResult Success(string message) => new(true, message);

    public static OperationResult Failure(string message, string? errorCode = null) =>
        new(false, message, errorCode);
}
