using RateLimitVisualizer.Core.Alerts;
using RateLimitVisualizer.Core.Models;

namespace RateLimitVisualizer.Tests.Alerts;

public sealed class QuotaAlertEvaluatorTests
{
    [Fact]
    public void Evaluate_emits_only_highest_usage_threshold_alert()
    {
        var alerts = new QuotaAlertEvaluator().Evaluate(Summary(96, 40, 120), false, DateTimeOffset.UtcNow);

        Assert.Single(alerts, alert => alert.Code.StartsWith("quota_usage", StringComparison.Ordinal));
        Assert.Contains(alerts, alert => alert.Code == "quota_usage_critical");
    }

    [Fact]
    public void HighestStatus_returns_highest_active_severity()
    {
        var alerts = new[]
        {
            new QuotaAlert("info", "i", "i", "i", DateTimeOffset.UtcNow),
            new QuotaAlert("critical", "c", "c", "c", DateTimeOffset.UtcNow),
            new QuotaAlert("warning", "w", "w", "w", DateTimeOffset.UtcNow)
        };

        Assert.Equal("critical", QuotaAlertEvaluator.HighestStatus(alerts));
    }

    private static DashboardSummary Summary(double usagePercent, double? timeToLimit, double? minutesUntilReset) => new(
        "demo-api",
        "local-demo-client",
        100,
        4,
        96,
        usagePercent,
        DateTimeOffset.UtcNow.AddMinutes(minutesUntilReset ?? 0),
        minutesUntilReset,
        10,
        timeToLimit,
        150,
        50,
        "healthy",
        DateTimeOffset.UtcNow,
        4,
        200,
        50);
}
