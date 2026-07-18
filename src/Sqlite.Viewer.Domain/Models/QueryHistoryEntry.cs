namespace Sqlite.Viewer.Domain.Models;

public sealed record QueryHistoryEntry(
    Guid Id,
    string Sql,
    DateTimeOffset ExecutedAt,
    bool IsFavorite = false,
    string? DatabasePath = null);
