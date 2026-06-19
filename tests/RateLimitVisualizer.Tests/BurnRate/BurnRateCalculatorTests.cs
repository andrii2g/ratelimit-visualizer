using RateLimitVisualizer.Core.BurnRate;
using RateLimitVisualizer.Core.Models;

namespace RateLimitVisualizer.Tests.BurnRate;

public sealed class BurnRateCalculatorTests
{
    [Fact]
    public void CalculateCurrentRatePerMinute_counts_only_rolling_window()
    {
        var now = new DateTimeOffset(2026, 6, 20, 0, 5, 0, TimeSpan.Zero);
        var observations = Enumerable.Range(0, 100)
            .Select(index => Observation(now.AddMinutes(-4).AddSeconds(index)))
            .Concat([Observation(now.AddMinutes(-10))]);

        var rate = new BurnRateCalculator().CalculateCurrentRatePerMinute(observations, now, 5);

        Assert.Equal(20, rate);
    }

    private static ObservedApiCall Observation(DateTimeOffset observedAtUtc) => new(
        "demo-api",
        "local-demo-client",
        "GET",
        "/v1/items/1",
        "/v1/items/:id",
        200,
        10,
        100,
        90,
        observedAtUtc.AddHours(1),
        null,
        observedAtUtc);
}
