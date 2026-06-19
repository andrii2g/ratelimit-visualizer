namespace RateLimitVisualizer.Core.Models;

public sealed record ObservedApiCall(
    string Provider,
    string Consumer,
    string Method,
    string? Url,
    string EndpointTemplate,
    int StatusCode,
    int? LatencyMs,
    long? Limit,
    long? Remaining,
    DateTimeOffset? ResetAtUtc,
    int? RetryAfterSeconds,
    DateTimeOffset ObservedAtUtc);

public sealed record ParsedRateLimit(
    long? Limit,
    long? Remaining,
    DateTimeOffset? ResetAtUtc,
    int? RetryAfterSeconds,
    bool HasAnyRateLimitData);

public sealed record DashboardSummary(
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
    int CurrentWindowRequestCount);

public sealed record EndpointUsage(
    string Method,
    string EndpointTemplate,
    int Requests,
    double ShareOfObservedRequestsPercent,
    double? AvgLatencyMs,
    int LastStatusCode);

public sealed record BurnRatePoint(
    DateTimeOffset TimestampUtc,
    long? Used,
    long? Remaining,
    long? Limit,
    double RequestsPerMinute);

public sealed record QuotaAlert(
    string Severity,
    string Code,
    string Message,
    string Recommendation,
    DateTimeOffset CreatedAtUtc);

public sealed record QuotaProjection(
    long? Used,
    double? UsagePercent,
    double? MinutesUntilReset,
    double? TimeToLimitMinutes,
    double? ProjectedUsageAtReset,
    double? ProjectedOverage);
