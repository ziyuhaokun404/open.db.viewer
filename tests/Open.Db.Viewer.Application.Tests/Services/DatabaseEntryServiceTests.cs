using FluentAssertions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Application.Tests.Support;
using Open.Db.Viewer.Domain.Models;

namespace Open.Db.Viewer.Application.Tests.Services;

public class DatabaseEntryServiceTests
{
    [Fact]
    public async Task OpenAsync_ShouldAddRecentEntry_WhenFileExists()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var service = new DatabaseEntryService(repository, _ => Task.FromResult(true));

        var result = await service.OpenAsync(@"C:\data\demo.db");

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("数据库已打开。");

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
        result.Message.Should().Be("数据库路径不能为空。");

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
        result.Message.Should().Be("未找到数据库文件。");

        (await repository.GetRecentAsync()).Should().BeEmpty();
    }
}
