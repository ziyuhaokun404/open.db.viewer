using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Domain.Models;
using System.Text.Json;

namespace Open.Db.Viewer.Infrastructure.Sqlite.Storage;

public sealed class FileQueryHistoryStore : IQueryHistoryStore
{
    private const int MaxHistoryEntries = 100;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _storageFilePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileQueryHistoryStore(string storageDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
        Directory.CreateDirectory(storageDirectory);
        _storageFilePath = Path.Combine(storageDirectory, "query-history.json");
    }

    public async Task<IReadOnlyList<QueryHistoryEntry>> GetRecentAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Entries
            .OrderByDescending(entry => entry.ExecutedAt)
            .Take(Math.Max(1, take))
            .ToArray();
    }

    public async Task<IReadOnlyList<QueryHistoryEntry>> GetFavoritesAsync(CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Entries
            .Where(entry => entry.IsFavorite)
            .OrderByDescending(entry => entry.ExecutedAt)
            .ToArray();
    }

    public async Task AddAsync(string sql, string? databasePath = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await ReadUnlockedAsync(cancellationToken);
            var normalized = sql.Trim();
            document.Entries.RemoveAll(entry =>
                !entry.IsFavorite &&
                string.Equals(entry.Sql, normalized, StringComparison.Ordinal));

            document.Entries.Insert(0, new QueryHistoryEntry(
                Guid.NewGuid(),
                normalized,
                DateTimeOffset.UtcNow,
                IsFavorite: false,
                databasePath));

            // Keep favorites + newest non-favorites within cap.
            if (document.Entries.Count > MaxHistoryEntries)
            {
                var favorites = document.Entries.Where(e => e.IsFavorite).ToList();
                var recent = document.Entries.Where(e => !e.IsFavorite).Take(MaxHistoryEntries - favorites.Count).ToList();
                document.Entries = favorites.Concat(recent)
                    .OrderByDescending(e => e.ExecutedAt)
                    .ToList();
            }

            await WriteUnlockedAsync(document, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await ReadUnlockedAsync(cancellationToken);
            var index = document.Entries.FindIndex(entry => entry.Id == id);
            if (index < 0)
            {
                return;
            }

            document.Entries[index] = document.Entries[index] with { IsFavorite = isFavorite };
            await WriteUnlockedAsync(document, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var document = await ReadUnlockedAsync(cancellationToken);
            document.Entries.RemoveAll(entry => entry.Id == id);
            await WriteUnlockedAsync(document, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<HistoryDocument> ReadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ReadUnlockedAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<HistoryDocument> ReadUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storageFilePath))
        {
            return new HistoryDocument();
        }

        try
        {
            await using var stream = File.OpenRead(_storageFilePath);
            var document = await JsonSerializer.DeserializeAsync<HistoryDocument>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
            return document ?? new HistoryDocument();
        }
        catch (JsonException)
        {
            return new HistoryDocument();
        }
    }

    private async Task WriteUnlockedAsync(HistoryDocument document, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_storageFilePath);
        await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private sealed class HistoryDocument
    {
        public List<QueryHistoryEntry> Entries { get; set; } = new();
    }
}
