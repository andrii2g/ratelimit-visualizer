using Microsoft.Data.Sqlite;
using RateLimitVisualizer.Core.Models;

namespace RateLimitVisualizer.Api.Storage;

public sealed class ObservationRepository(SqliteConnectionFactory connectionFactory) : IObservationRepository
{
    public async Task InsertAsync(ObservedApiCall observation, DateTimeOffset createdAtUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO observations (
                provider, consumer, method, url, endpoint_template, status_code, latency_ms,
                limit_value, remaining_value, reset_at_utc, retry_after_seconds, observed_at_utc, created_at_utc
            )
            VALUES (
                $provider, $consumer, $method, $url, $endpoint_template, $status_code, $latency_ms,
                $limit_value, $remaining_value, $reset_at_utc, $retry_after_seconds, $observed_at_utc, $created_at_utc
            );
            """;

        Add(command, "$provider", observation.Provider);
        Add(command, "$consumer", observation.Consumer);
        Add(command, "$method", observation.Method);
        Add(command, "$url", observation.Url);
        Add(command, "$endpoint_template", observation.EndpointTemplate);
        Add(command, "$status_code", observation.StatusCode);
        Add(command, "$latency_ms", observation.LatencyMs);
        Add(command, "$limit_value", observation.Limit);
        Add(command, "$remaining_value", observation.Remaining);
        Add(command, "$reset_at_utc", ToStorage(observation.ResetAtUtc));
        Add(command, "$retry_after_seconds", observation.RetryAfterSeconds);
        Add(command, "$observed_at_utc", ToStorage(observation.ObservedAtUtc));
        Add(command, "$created_at_utc", ToStorage(createdAtUtc));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ObservedApiCall?> GetLatestAsync(string provider, string consumer, CancellationToken cancellationToken = default)
    {
        var observations = await QueryAsync(
            """
            SELECT * FROM observations
            WHERE provider = $provider AND consumer = $consumer
            ORDER BY observed_at_utc DESC, id DESC
            LIMIT 1;
            """,
            provider,
            consumer,
            cancellationToken);

        return observations.FirstOrDefault();
    }

    public Task<IReadOnlyList<ObservedApiCall>> GetObservationsSinceAsync(string provider, string consumer, DateTimeOffset sinceUtc, CancellationToken cancellationToken = default) =>
        QueryAsync(
            """
            SELECT * FROM observations
            WHERE provider = $provider AND consumer = $consumer AND observed_at_utc >= $since
            ORDER BY observed_at_utc ASC, id ASC;
            """,
            provider,
            consumer,
            cancellationToken,
            sinceUtc);

    public Task<IReadOnlyList<ObservedApiCall>> GetRecentAsync(string provider, string consumer, int minutes, DateTimeOffset nowUtc, CancellationToken cancellationToken = default) =>
        GetObservationsSinceAsync(provider, consumer, nowUtc.AddMinutes(-Math.Max(1, minutes)), cancellationToken);

    private async Task<IReadOnlyList<ObservedApiCall>> QueryAsync(
        string sql,
        string provider,
        string consumer,
        CancellationToken cancellationToken,
        DateTimeOffset? sinceUtc = null)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        Add(command, "$provider", provider);
        Add(command, "$consumer", consumer);
        if (sinceUtc is not null)
        {
            Add(command, "$since", ToStorage(sinceUtc));
        }

        var items = new List<ObservedApiCall>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadObservation(reader));
        }

        return items;
    }

    private static ObservedApiCall ReadObservation(SqliteDataReader reader) => new(
        reader.GetString(reader.GetOrdinal("provider")),
        reader.GetString(reader.GetOrdinal("consumer")),
        reader.GetString(reader.GetOrdinal("method")),
        GetNullableString(reader, "url"),
        reader.GetString(reader.GetOrdinal("endpoint_template")),
        reader.GetInt32(reader.GetOrdinal("status_code")),
        GetNullableInt(reader, "latency_ms"),
        GetNullableLong(reader, "limit_value"),
        GetNullableLong(reader, "remaining_value"),
        ParseDate(GetNullableString(reader, "reset_at_utc")),
        GetNullableInt(reader, "retry_after_seconds"),
        ParseDate(reader.GetString(reader.GetOrdinal("observed_at_utc")))!.Value);

    private static void Add(SqliteCommand command, string name, object? value) =>
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);

    private static string ToStorage(DateTimeOffset value) => value.UtcDateTime.ToString("O");

    private static string? ToStorage(DateTimeOffset? value) => value is null ? null : ToStorage(value.Value);

    private static string? GetNullableString(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? GetNullableInt(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static long? GetNullableLong(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static DateTimeOffset? ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed.ToUniversalTime() : null;
}
