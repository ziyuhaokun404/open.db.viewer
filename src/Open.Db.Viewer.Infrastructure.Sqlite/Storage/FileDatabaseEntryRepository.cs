using System.Text.Json;
using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Domain.Models;

namespace Open.Db.Viewer.Infrastructure.Sqlite.Storage;

public sealed class FileDatabaseEntryRepository : IDatabaseEntryRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _storageDirectory;
    private readonly string _storageFilePath;

    public FileDatabaseEntryRepository(string storageDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);

        _storageDirectory = storageDirectory;
        _storageFilePath = Path.Combine(storageDirectory, "database-entries.json");
    }

    public async Task<IReadOnlyList<DatabaseEntry>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.RecentEntries.ToArray();
    }

    public async Task<IReadOnlyList<DatabaseEntry>> GetPinnedAsync(CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.PinnedEntries.ToArray();
    }

    public async Task SaveRecentAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var document = await ReadAsync(cancellationToken);
        Upsert(document.RecentEntries, entry);
        await WriteAsync(document, cancellationToken);
    }

    public async Task SavePinnedAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var document = await ReadAsync(cancellationToken);
        Upsert(document.PinnedEntries, entry with { IsPinned = true });
        await WriteAsync(document, cancellationToken);
    }

    public async Task RemovePinnedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        document.PinnedEntries.RemoveAll(entry => entry.Id == id);
        await WriteAsync(document, cancellationToken);
    }

    private async Task<DatabaseEntriesDocument> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storageFilePath))
        {
            return new DatabaseEntriesDocument();
        }

        try
        {
            await using var stream = File.OpenRead(_storageFilePath);
            var document = await JsonSerializer.DeserializeAsync<DatabaseEntriesDocument>(
                stream,
                SerializerOptions,
                cancellationToken);

            return Normalize(document);
        }
        catch (JsonException)
        {
            return new DatabaseEntriesDocument();
        }
    }

    private async Task WriteAsync(DatabaseEntriesDocument document, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_storageDirectory);

        await using var stream = File.Create(_storageFilePath);
        await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken);
    }

    private static DatabaseEntriesDocument Normalize(DatabaseEntriesDocument? document)
    {
        if (document is null)
        {
            return new DatabaseEntriesDocument();
        }

        return new DatabaseEntriesDocument
        {
            RecentEntries = document.RecentEntries ?? [],
            PinnedEntries = document.PinnedEntries ?? []
        };
    }

    private static void Upsert(List<DatabaseEntry> entries, DatabaseEntry entry)
    {
        var index = entries.FindIndex(existing =>
            existing.FilePath.Equals(entry.FilePath, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            entries[index] = entry;
        }
        else
        {
            entries.Add(entry);
        }
    }

    private sealed class DatabaseEntriesDocument
    {
        public List<DatabaseEntry> RecentEntries { get; init; } = [];

        public List<DatabaseEntry> PinnedEntries { get; init; } = [];
    }
}
