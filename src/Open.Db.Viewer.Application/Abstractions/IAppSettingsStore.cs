using Open.Db.Viewer.Domain.Models;

namespace Open.Db.Viewer.Application.Abstractions;

public interface IAppSettingsStore
{
    AppSettings Current { get; }

    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
