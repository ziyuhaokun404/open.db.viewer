using OpenDbViewer.Application.Abstractions;
using OpenDbViewer.Domain.Models;

namespace OpenDbViewer.Application.Services;

public sealed class DatabaseEntryService
{
    private readonly IDatabaseEntryRepository _repository;
    private readonly Func<string, Task<bool>> _fileExists;

    public DatabaseEntryService(IDatabaseEntryRepository repository, Func<string, Task<bool>> fileExists)
    {
        _repository = repository;
        _fileExists = fileExists;
    }

    public async Task<OperationResult> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return OperationResult.Failure("Database path is required.", "database_path_required");
        }

        if (!await _fileExists(filePath))
        {
            return OperationResult.Failure("Database file was not found.", "database_file_not_found");
        }

        var entry = new DatabaseEntry(
            Guid.NewGuid(),
            Path.GetFileNameWithoutExtension(filePath),
            filePath,
            DateTimeOffset.UtcNow,
            false);

        await _repository.SaveRecentAsync(entry, cancellationToken);

        return OperationResult.Success("Database opened.");
    }

    public Task<IReadOnlyList<DatabaseEntry>> GetRecentAsync(CancellationToken cancellationToken = default) =>
        _repository.GetRecentAsync(cancellationToken);

    public Task<IReadOnlyList<DatabaseEntry>> GetPinnedAsync(CancellationToken cancellationToken = default) =>
        _repository.GetPinnedAsync(cancellationToken);

    public Task PinAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return _repository.SavePinnedAsync(entry, cancellationToken);
    }

    public Task UnpinAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return _repository.RemovePinnedAsync(entry.Id, cancellationToken);
    }
}
