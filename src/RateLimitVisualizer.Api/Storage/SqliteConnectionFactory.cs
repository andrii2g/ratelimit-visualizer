using Microsoft.Data.Sqlite;

namespace RateLimitVisualizer.Api.Storage;

public sealed class SqliteConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetValue<string>("Storage:ConnectionString")
        ?? "Data Source=./data/ratelimit-visualizer.db";

    public SqliteConnection CreateConnection()
    {
        var builder = new SqliteConnectionStringBuilder(_connectionString);

        if (!string.IsNullOrWhiteSpace(builder.DataSource) &&
            builder.DataSource != ":memory:" &&
            !builder.DataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        return new SqliteConnection(_connectionString);
    }
}
