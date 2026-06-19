using RateLimitVisualizer.Core.BurnRate;

namespace RateLimitVisualizer.Tests.BurnRate;

public sealed class QuotaProjectionCalculatorTests
{
    [Fact]
    public void Calculate_projects_usage_at_reset()
    {
        var now = new DateTimeOffset(2026, 6, 20, 0, 0, 0, TimeSpan.Zero);
        var projection = new QuotaProjectionCalculator().Calculate(100, 40, 10, now.AddMinutes(5), now);

        Assert.Equal(60, projection.Used);
        Assert.Equal(60, projection.UsagePercent);
        Assert.Equal(4, projection.TimeToLimitMinutes);
        Assert.Equal(110, projection.ProjectedUsageAtReset);
        Assert.Equal(10, projection.ProjectedOverage);
    }

    [Fact]
    public void Calculate_handles_zero_rate_and_missing_reset()
    {
        var projection = new QuotaProjectionCalculator().Calculate(100, 40, 0, null, DateTimeOffset.UtcNow);

        Assert.Null(projection.TimeToLimitMinutes);
        Assert.Null(projection.ProjectedUsageAtReset);
    }
}
