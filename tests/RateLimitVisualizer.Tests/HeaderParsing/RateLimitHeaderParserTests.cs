using RateLimitVisualizer.Core.HeaderParsing;

namespace RateLimitVisualizer.Tests.HeaderParsing;

public sealed class RateLimitHeaderParserTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 20, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Parse_supports_legacy_headers_case_insensitively()
    {
        var parser = new RateLimitHeaderParser();
        var parsed = parser.Parse(new Dictionary<string, string>
        {
            ["x-ratelimit-limit"] = "10000",
            ["X-RateLimit-Remaining"] = "7420",
            ["X-RateLimit-Reset"] = "1781881685"
        }, Now);

        Assert.True(parsed.HasAnyRateLimitData);
        Assert.Equal(10000, parsed.Limit);
        Assert.Equal(7420, parsed.Remaining);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1781881685), parsed.ResetAtUtc);
    }

    [Fact]
    public void Parse_treats_small_reset_integer_as_seconds_from_now()
    {
        var parsed = new RateLimitHeaderParser().Parse(new Dictionary<string, string>
        {
            ["RateLimit-Reset"] = "3600"
        }, Now);

        Assert.Equal(Now.AddHours(1), parsed.ResetAtUtc);
    }

    [Fact]
    public void Parse_uses_retry_after_when_reset_is_missing()
    {
        var parsed = new RateLimitHeaderParser().Parse(new Dictionary<string, string>
        {
            ["Retry-After"] = "30"
        }, Now);

        Assert.Equal(30, parsed.RetryAfterSeconds);
        Assert.Equal(Now.AddSeconds(30), parsed.ResetAtUtc);
    }

    [Fact]
    public void Parse_ignores_malformed_numeric_values()
    {
        var parsed = new RateLimitHeaderParser().Parse(new Dictionary<string, string>
        {
            ["RateLimit-Limit"] = "none",
            ["RateLimit-Remaining"] = "many"
        }, Now);

        Assert.False(parsed.HasAnyRateLimitData);
    }
}
