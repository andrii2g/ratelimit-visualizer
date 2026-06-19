namespace RateLimitVisualizer.Core.HeaderParsing;

public static class RateLimitHeaderNames
{
    public static readonly string[] Limit = ["RateLimit-Limit", "X-RateLimit-Limit", "X-Rate-Limit-Limit"];
    public static readonly string[] Remaining = ["RateLimit-Remaining", "X-RateLimit-Remaining", "X-Rate-Limit-Remaining"];
    public static readonly string[] Reset = ["RateLimit-Reset", "X-RateLimit-Reset", "X-Rate-Limit-Reset"];
    public static readonly string[] RetryAfter = ["Retry-After"];
}
