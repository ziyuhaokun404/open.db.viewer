using FluentAssertions;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Infrastructure.Sqlite.Storage;

namespace Open.Db.Viewer.Infrastructure.Sqlite.Tests.Storage;

public sealed class FileDatabaseEntryRepositoryTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        "OpenDbViewer.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GetRecentAsync_ShouldReturnEmpty_WhenStorageFileDoesNotExist()
    {
        var repository = new FileDatabaseEntryRepository(_rootPath);

        var entries = await repository.GetRecentAsync();

        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveRecentAsync_ShouldPersistEntriesAcrossRepositoryInstances()
    {
        var entry = new DatabaseEntry(
            Guid.NewGuid(),
            "demo",
            @"C:\data\demo.db",
            DateTimeOffset.UtcNow,
            false);

        await new FileDatabaseEntryRepository(_rootPath).SaveRecentAsync(entry);

        var entries = await new FileDatabaseEntryRepository(_rootPath).GetRecentAsync();

        entries.Should().ContainSingle();
        entries[0].FilePath.Should().Be(entry.FilePath);
    }

    [Fact]
    public async Task SavePinnedAsync_ShouldDeduplicateByFilePath()
    {
        var first = new DatabaseEntry(
            Guid.NewGuid(),
            "demo",
            @"C:\data\demo.db",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            false);
        var second = first with
        {
            Id = Guid.NewGuid(),
            LastOpenedAt = DateTimeOffset.UtcNow
        };

        var repository = new FileDatabaseEntryRepository(_rootPath);
        await repository.SavePinnedAsync(first);
        await repository.SavePinnedAsync(second);

        var entries = await repository.GetPinnedAsync();

        entries.Should().ContainSingle();
        entries[0].IsPinned.Should().BeTrue();
        entries[0].LastOpenedAt.Should().Be(second.LastOpenedAt);
    }

    [Fact]
    public async Task RemovePinnedAsync_ShouldDeleteMatchingEntry()
    {
        var entry = new DatabaseEntry(
            Guid.NewGuid(),
            "demo",
            @"C:\data\demo.db",
            DateTimeOffset.UtcNow,
            false);
        var repository = new FileDatabaseEntryRepository(_rootPath);
        await repository.SavePinnedAsync(entry);

        await repository.RemovePinnedAsync(entry.Id);

        (await repository.GetPinnedAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetPinnedAsync_ShouldReturnEmpty_WhenJsonIsInvalid()
    {
        Directory.CreateDirectory(_rootPath);
        await File.WriteAllTextAsync(
            Path.Combine(_rootPath, "database-entries.json"),
            "{ invalid json");

        var entries = await new FileDatabaseEntryRepository(_rootPath).GetPinnedAsync();

        entries.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
