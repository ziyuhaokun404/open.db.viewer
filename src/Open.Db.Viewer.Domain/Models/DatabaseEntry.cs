namespace Open.Db.Viewer.Domain.Models;

public sealed record DatabaseEntry(
    Guid Id,
    string Name,
    string FilePath,
    DateTimeOffset LastOpenedAt,
    bool IsPinned)
{
    public static DatabaseEntry CreatePinned(string name, string filePath) =>
        new(Guid.NewGuid(), name, filePath, DateTimeOffset.UtcNow, true);
}
