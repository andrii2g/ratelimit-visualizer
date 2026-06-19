using Microsoft.Extensions.Configuration;
using RateLimitVisualizer.Api.Storage;
using RateLimitVisualizer.Core.Models;

namespace RateLimitVisualizer.Tests.Storage;

public sealed class ObservationRepositoryTests
{
    [Fact]
    public async Task Repository_initializes_inserts_and_queries_observations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var dbPath = Path.Combine(Path.GetTempPath(), $"ratelimit-visualizer-{Guid.NewGuid():N}.db");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ConnectionString"] = $"Data Source={dbPath}"
            })
            .Build();
        var factory = new SqliteConnectionFactory(configuration);
        await new DatabaseInitializer(factory).InitializeAsync(cancellationToken);
        var repository = new ObservationRepository(factory);
        var now = DateTimeOffset.UtcNow;

        await repository.InsertAsync(new ObservedApiCall("demo-api", "local-demo-client", "GET", "/v1/items/1", "/v1/items/:id", 200, 11, 100, 90, now.AddHours(1), null, now), now, cancellationToken);

        var latest = await repository.GetLatestAsync("demo-api", "local-demo-client", cancellationToken);
        var recent = await repository.GetRecentAsync("demo-api", "local-demo-client", 5, now.AddMinutes(1), cancellationToken);

        Assert.NotNull(latest);
        Assert.Single(recent);
    }
}
