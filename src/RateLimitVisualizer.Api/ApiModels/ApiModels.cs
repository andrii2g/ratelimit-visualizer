using RateLimitVisualizer.Core.Models;

namespace RateLimitVisualizer.Api.ApiModels;

public sealed record ObservationRequest(
    string? Provider,
    string? Consumer,
    string? Method,
    string? Url,
    string? EndpointTemplate,
    int? StatusCode,
    int? LatencyMs,
    IReadOnlyDictionary<string, string>? Headers,
    DateTimeOffset? ObservedAtUtc);

public sealed record ObservationResponse(
    bool Accepted,
    string? Provider = null,
    string? Consumer = null,
    string? EndpointTemplate = null,
    long? Limit = null,
    long? Remaining = null,
    DateTimeOffset? ResetAtUtc = null,
    string? Reason = null);

public sealed record DashboardSummaryResponse(
    string Provider,
    string Consumer,
    long? Limit,
    long? Remaining,
    long? Used,
    double? UsagePercent,
    DateTimeOffset? ResetAtUtc,
    double? MinutesUntilReset,
    double CurrentRatePerMinute,
    double? TimeToLimitMinutes,
    double? ProjectedUsageAtReset,
    double? ProjectedOverage,
    string Status,
    DateTimeOffset? LatestObservationAtUtc,
    long? LatestRemaining,
    int? LatestStatusCode,
    int CurrentWindowRequestCount)
{
    public static DashboardSummaryResponse FromSummary(DashboardSummary summary) => new(
        summary.Provider,
        summary.Consumer,
        summary.Limit,
        summary.Remaining,
        summary.Used,
        summary.UsagePercent,
        summary.ResetAtUtc,
        summary.MinutesUntilReset,
        summary.CurrentRatePerMinute,
        summary.TimeToLimitMinutes,
        summary.ProjectedUsageAtReset,
        summary.ProjectedOverage,
        summary.Status,
        summary.LatestObservationAtUtc,
        summary.LatestRemaining,
        summary.LatestStatusCode,
        summary.CurrentWindowRequestCount);
}

public sealed record BurnRateResponse(string Provider, string Consumer, IReadOnlyList<BurnRatePoint> Points);

public sealed record EndpointUsageResponse(string Provider, string Consumer, IReadOnlyList<EndpointUsage> Items);

public sealed record AlertsResponse(string Provider, string Consumer, IReadOnlyList<QuotaAlert> Items);
