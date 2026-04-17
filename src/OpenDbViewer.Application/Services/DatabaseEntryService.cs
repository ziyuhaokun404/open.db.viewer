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
}
