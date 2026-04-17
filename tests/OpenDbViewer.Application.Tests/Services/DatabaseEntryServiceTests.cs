using FluentAssertions;
using OpenDbViewer.Application.Services;
using OpenDbViewer.Application.Tests.Support;
using OpenDbViewer.Domain.Models;

namespace OpenDbViewer.Application.Tests.Services;

public class DatabaseEntryServiceTests
{
    [Fact]
    public async Task OpenAsync_ShouldAddRecentEntry_WhenFileExists()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var service = new DatabaseEntryService(repository, _ => Task.FromResult(true));

        var result = await service.OpenAsync(@"C:\data\demo.db");

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Database opened.");

        var recentEntries = await repository.GetRecentAsync();
        recentEntries.Should().ContainSingle();

        var entry = recentEntries.Single();
        entry.Name.Should().Be("demo");
        entry.FilePath.Should().Be(@"C:\data\demo.db");
        entry.IsPinned.Should().BeFalse();
    }

    [Fact]
    public async Task OpenAsync_ShouldFail_WhenFilePathIsEmpty()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var service = new DatabaseEntryService(repository, _ => Task.FromResult(true));

        var result = await service.OpenAsync(string.Empty);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("database_path_required");
        result.Message.Should().Be("Database path is required.");

        (await repository.GetRecentAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task OpenAsync_ShouldFail_WhenFileDoesNotExist()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var service = new DatabaseEntryService(repository, _ => Task.FromResult(false));

        var result = await service.OpenAsync(@"C:\data\missing.db");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("database_file_not_found");
        result.Message.Should().Be("Database file was not found.");

        (await repository.GetRecentAsync()).Should().BeEmpty();
    }
}
