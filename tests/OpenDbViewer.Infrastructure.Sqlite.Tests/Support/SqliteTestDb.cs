using Microsoft.Data.Sqlite;

namespace OpenDbViewer.Infrastructure.Sqlite.Tests.Support;

public sealed class SqliteTestDb : IAsyncDisposable
{
    private SqliteTestDb(string directoryPath, string filePath)
    {
        DirectoryPath = directoryPath;
        FilePath = filePath;
    }

    public string DirectoryPath { get; }

    public string FilePath { get; }

    public static async Task<SqliteTestDb> CreateAsync()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), $"open-db-viewer-sqlite-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);

        var filePath = Path.Combine(directoryPath, "sample.db");

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = filePath,
            Pooling = false
        }.ToString();

        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();

            var commandText = """
                CREATE TABLE users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT
                );

                CREATE TABLE orders (
                    id INTEGER PRIMARY KEY,
                    user_id INTEGER NOT NULL,
                    total REAL NOT NULL
                );

                INSERT INTO users (name, email) VALUES
                    ('Alice', 'alice@example.com'),
                    ('Bob', 'bob@example.com'),
                    ('Charlie', NULL);

                INSERT INTO orders (id, user_id, total) VALUES
                    (10, 1, 12.5),
                    (11, 2, 18.75);
                """;

            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync();
        }

        return new SqliteTestDb(directoryPath, filePath);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Yield();

        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
