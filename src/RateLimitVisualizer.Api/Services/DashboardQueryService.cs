using RateLimitVisualizer.Api.Storage;
using RateLimitVisualizer.Core.Alerts;
using RateLimitVisualizer.Core.BurnRate;
using RateLimitVisualizer.Core.Models;
using RateLimitVisualizer.Core.Time;

namespace RateLimitVisualizer.Api.Services;

public sealed class DashboardQueryService(
    IObservationRepository repository,
    BurnRateCalculator burnRateCalculator,
    QuotaProjectionCalculator projectionCalculator,
    QuotaAlertEvaluator alertEvaluator,
    IConfiguration configuration,
    IClock clock)
{
    public async Task<DashboardSummary> GetSummaryAsync(string provider, string consumer, CancellationToken cancellationToken = default)
    {
        var nowUtc = clock.UtcNow;
        var rollingWindowMinutes = GetRollingWindowMinutes();
        var latest = await repository.GetLatestAsync(provider, consumer, cancellationToken);
        var recent = await repository.GetRecentAsync(provider, consumer, rollingWindowMinutes, nowUtc, cancellationToken);
        var currentRate = burnRateCalculator.CalculateCurrentRatePerMinute(recent, nowUtc, rollingWindowMinutes);
        var projection = projectionCalculator.Calculate(latest?.Limit, latest?.Remaining, currentRate, latest?.ResetAtUtc, nowUtc);
        var has429 = recent.Any(observation => observation.StatusCode == StatusCodes.Status429TooManyRequests);

        var summaryWithoutStatus = new DashboardSummary(
            provider,
            consumer,
            latest?.Limit,
            latest?.Remaining,
            projection.Used,
            projection.UsagePercent,
            latest?.ResetAtUtc,
            projection.MinutesUntilReset,
            currentRate,
            projection.TimeToLimitMinutes,
            projection.ProjectedUsageAtReset,
            projection.ProjectedOverage,
            "healthy",
            latest?.ObservedAtUtc,
            latest?.Remaining,
            latest?.StatusCode,
            recent.Count);

        var alerts = alertEvaluator.Evaluate(summaryWithoutStatus, has429, nowUtc);
        return summaryWithoutStatus with { Status = QuotaAlertEvaluator.HighestStatus(alerts) };
    }

    public async Task<IReadOnlyList<BurnRatePoint>> GetBurnRateAsync(string provider, string consumer, int minutes, CancellationToken cancellationToken = default)
    {
        var nowUtc = clock.UtcNow;
        var observations = await repository.GetRecentAsync(provider, consumer, Math.Clamp(minutes, 1, 24 * 60), nowUtc, cancellationToken);
        return observations
            .GroupBy(observation => new DateTimeOffset(observation.ObservedAtUtc.UtcDateTime.AddSeconds(-observation.ObservedAtUtc.Second).AddMilliseconds(-observation.ObservedAtUtc.Millisecond)))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var latest = group.OrderBy(observation => observation.ObservedAtUtc).Last();
                return new BurnRatePoint(
                    group.Key,
                    latest.Limit is not null && latest.Remaining is not null ? Math.Max(0, latest.Limit.Value - latest.Remaining.Value) : null,
                    latest.Remaining,
                    latest.Limit,
                    group.Count());
            })
            .ToList();
    }

    public async Task<IReadOnlyList<EndpointUsage>> GetEndpointUsageAsync(string provider, string consumer, int top, CancellationToken cancellationToken = default)
    {
        var nowUtc = clock.UtcNow;
        var observations = await repository.GetRecentAsync(provider, consumer, 24 * 60, nowUtc, cancellationToken);
        var total = observations.Count;
        if (total == 0)
        {
            return [];
        }

        return observations
            .GroupBy(observation => new { observation.Method, observation.EndpointTemplate })
            .Select(group =>
            {
                var latest = group.OrderBy(observation => observation.ObservedAtUtc).Last();
                return new EndpointUsage(
                    group.Key.Method,
                    group.Key.EndpointTemplate,
                    group.Count(),
                    group.Count() / (double)total * 100,
                    group.Where(observation => observation.LatencyMs is not null).Select(observation => observation.LatencyMs!.Value).DefaultIfEmpty().Average(),
                    latest.StatusCode);
            })
            .OrderByDescending(item => item.Requests)
            .Take(Math.Clamp(top, 1, 100))
            .ToList();
    }

    public async Task<IReadOnlyList<QuotaAlert>> GetAlertsAsync(string provider, string consumer, CancellationToken cancellationToken = default)
    {
        var nowUtc = clock.UtcNow;
        var summary = await GetSummaryAsync(provider, consumer, cancellationToken);
        var recent = await repository.GetRecentAsync(provider, consumer, GetRollingWindowMinutes(), nowUtc, cancellationToken);
        return alertEvaluator.Evaluate(summary, recent.Any(observation => observation.StatusCode == StatusCodes.Status429TooManyRequests), nowUtc);
    }

    private int GetRollingWindowMinutes() => Math.Max(1, configuration.GetValue("BurnRate:RollingWindowMinutes", 5));
}
