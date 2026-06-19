using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RateLimitVisualizer.Tests.Api;

public sealed class ObservationEndpointsTests
{
    [Fact]
    public async Task Post_observations_accepts_valid_payload()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/observations", new
        {
            provider = "demo-api",
            consumer = "local-demo-client",
            method = "GET",
            url = "http://localhost:5060/v1/items/123",
            statusCode = 200,
            headers = new Dictionary<string, string>
            {
                ["X-RateLimit-Limit"] = "100",
                ["X-RateLimit-Remaining"] = "90"
            }
        }, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_observations_returns_accepted_false_for_missing_headers()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/observations", new
        {
            provider = "demo-api",
            consumer = "local-demo-client",
            method = "GET",
            url = "http://localhost:5060/v1/items/123",
            statusCode = 200,
            headers = new Dictionary<string, string>()
        }, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Contains("\"accepted\":false", body);
    }

    [Fact]
    public async Task Dashboard_summary_and_health_return_json()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var health = await client.GetAsync("/health", cancellationToken);
        var summary = await client.GetAsync("/api/dashboard/summary?provider=demo-api&consumer=local-demo-client", cancellationToken);
        var body = await summary.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        Assert.Contains("currentWindowRequestCount", body);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"ratelimit-visualizer-api-{Guid.NewGuid():N}.db");
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:ConnectionString"] = $"Data Source={dbPath}"
                });
            });
        });
    }
}
