using Sqlite.Viewer.Application.Abstractions;
using Sqlite.Viewer.Domain.Models;
using System.Text.Json;

namespace Sqlite.Viewer.Infrastructure.Sqlite.Storage;

public sealed class FileAppSettingsStore : IAppSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _storageFilePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AppSettings _current = new();

    public FileAppSettingsStore(string storageDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
        Directory.CreateDirectory(storageDirectory);
        _storageFilePath = Path.Combine(storageDirectory, "settings.json");
    }

    public AppSettings Current => _current.Clone();

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_storageFilePath))
            {
                _current = new AppSettings();
                _current.Normalize();
                return _current.Clone();
            }

            await using var stream = File.OpenRead(_storageFilePath);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
            _current = loaded ?? new AppSettings();
            _current.Normalize();
            return _current.Clone();
        }
        catch (JsonException)
        {
            _current = new AppSettings();
            _current.Normalize();
            return _current.Clone();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Normalize();

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _current = settings.Clone();
            var directory = Path.GetDirectoryName(_storageFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(_storageFilePath);
            await JsonSerializer.SerializeAsync(stream, _current, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }
}
