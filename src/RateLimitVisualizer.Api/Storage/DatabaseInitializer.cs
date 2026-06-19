using Microsoft.Data.Sqlite;

namespace RateLimitVisualizer.Api.Storage;

public sealed class DatabaseInitializer(SqliteConnectionFactory connectionFactory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var commands = new[]
        {
            """
            CREATE TABLE IF NOT EXISTS observations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                provider TEXT NOT NULL,
                consumer TEXT NOT NULL,
                method TEXT NOT NULL,
                url TEXT NULL,
                endpoint_template TEXT NOT NULL,
                status_code INTEGER NOT NULL,
                latency_ms INTEGER NULL,
                limit_value INTEGER NULL,
                remaining_value INTEGER NULL,
                reset_at_utc TEXT NULL,
                retry_after_seconds INTEGER NULL,
                observed_at_utc TEXT NOT NULL,
                created_at_utc TEXT NOT NULL
            );
            """,
            "CREATE INDEX IF NOT EXISTS idx_observations_provider_consumer_time ON observations(provider, consumer, observed_at_utc);",
            "CREATE INDEX IF NOT EXISTS idx_observations_provider_consumer_reset ON observations(provider, consumer, reset_at_utc);",
            "CREATE INDEX IF NOT EXISTS idx_observations_endpoint ON observations(provider, consumer, method, endpoint_template);",
            "CREATE INDEX IF NOT EXISTS idx_observations_status ON observations(provider, consumer, status_code, observed_at_utc);"
        };

        foreach (var sql in commands)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
