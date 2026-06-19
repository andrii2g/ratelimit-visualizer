using RateLimitVisualizer.Core.Models;

namespace RateLimitVisualizer.Core.BurnRate;

public sealed class QuotaProjectionCalculator
{
    public QuotaProjection Calculate(long? limit, long? remaining, double currentRatePerMinute, DateTimeOffset? resetAtUtc, DateTimeOffset nowUtc)
    {
        long? used = limit is not null && remaining is not null ? Math.Max(0, limit.Value - remaining.Value) : null;
        double? usagePercent = limit is > 0 && used is not null ? used.Value / (double)limit.Value * 100 : null;
        double? timeToLimitMinutes = remaining switch
        {
            <= 0 => 0,
            not null when currentRatePerMinute > 0 => remaining.Value / currentRatePerMinute,
            _ => null
        };
        double? minutesUntilReset = resetAtUtc is null ? null : Math.Max(0, (resetAtUtc.Value - nowUtc).TotalMinutes);
        double? projectedUsageAtReset = used is not null && minutesUntilReset is not null
            ? used.Value + currentRatePerMinute * minutesUntilReset.Value
            : null;
        double? projectedOverage = limit is not null && projectedUsageAtReset is not null
            ? Math.Max(0, projectedUsageAtReset.Value - limit.Value)
            : null;

        return new QuotaProjection(used, usagePercent, minutesUntilReset, timeToLimitMinutes, projectedUsageAtReset, projectedOverage);
    }
}
