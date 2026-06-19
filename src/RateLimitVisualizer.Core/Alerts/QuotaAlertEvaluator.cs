using RateLimitVisualizer.Core.Models;

namespace RateLimitVisualizer.Core.Alerts;

public sealed class QuotaAlertEvaluator
{
    public IReadOnlyList<QuotaAlert> Evaluate(DashboardSummary summary, bool has429InCurrentWindow, DateTimeOffset nowUtc)
    {
        var alerts = new List<QuotaAlert>();

        if (summary.UsagePercent is >= 95)
        {
            alerts.Add(Create("critical", "quota_usage_critical", "Quota usage is above 95%.", "Stop nonessential requests until the reset window.", nowUtc));
        }
        else if (summary.UsagePercent is >= 80)
        {
            alerts.Add(Create("warning", "quota_usage_warning", "Quota usage is above 80%.", "Reduce request volume or cache repeated calls until the reset window.", nowUtc));
        }
        else if (summary.UsagePercent is >= 60)
        {
            alerts.Add(Create("info", "quota_usage_info", "Quota usage is above 60%.", "Monitor consumption before increasing request volume.", nowUtc));
        }

        if (summary.TimeToLimitMinutes is not null &&
            summary.MinutesUntilReset is not null &&
            summary.TimeToLimitMinutes < summary.MinutesUntilReset &&
            summary.TimeToLimitMinutes <= 30)
        {
            alerts.Add(Create("warning", "quota_exhaustion_projected", "At the current burn rate, quota will be exhausted before the reset window.", "Reduce request volume or cache repeated calls until the reset window.", nowUtc));
        }

        if (summary.TimeToLimitMinutes is <= 10)
        {
            alerts.Add(Create("critical", "quota_exhaustion_critical", "Quota may be exhausted in less than 10 minutes.", "Stop nonessential calls immediately.", nowUtc));
        }

        if (has429InCurrentWindow)
        {
            alerts.Add(Create("critical", "rate_limited", "429 responses were observed in the current quota window.", "Back off request volume and respect Retry-After values.", nowUtc));
        }

        return alerts;
    }

    public static string HighestStatus(IEnumerable<QuotaAlert> alerts)
    {
        var severities = alerts.Select(alert => alert.Severity).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (severities.Contains("critical"))
        {
            return "critical";
        }

        if (severities.Contains("warning"))
        {
            return "warning";
        }

        return severities.Contains("info") ? "info" : "healthy";
    }

    private static QuotaAlert Create(string severity, string code, string message, string recommendation, DateTimeOffset nowUtc) =>
        new(severity, code, message, recommendation, nowUtc);
}
