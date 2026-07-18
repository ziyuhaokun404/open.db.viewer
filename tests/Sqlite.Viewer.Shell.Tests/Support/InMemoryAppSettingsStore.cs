using Sqlite.Viewer.Application.Abstractions;
using Sqlite.Viewer.Domain.Models;

namespace Sqlite.Viewer.Shell.Tests.Support;

public sealed class InMemoryAppSettingsStore : IAppSettingsStore
{
    private AppSettings _current = new();

    public AppSettings Current => _current.Clone();

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        _current.Normalize();
        return Task.FromResult(_current.Clone());
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _current = settings.Clone();
        _current.Normalize();
        return Task.CompletedTask;
    }
}
