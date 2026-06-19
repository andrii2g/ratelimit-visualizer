using System.Globalization;
using RateLimitVisualizer.Core.Models;

namespace RateLimitVisualizer.Core.HeaderParsing;

public sealed class RateLimitHeaderParser
{
    private const long UnixTimestampThreshold = 1_000_000_000;

    public ParsedRateLimit Parse(IReadOnlyDictionary<string, string>? headers, DateTimeOffset nowUtc)
    {
        if (headers is null || headers.Count == 0)
        {
            return new ParsedRateLimit(null, null, null, null, false);
        }

        var lookup = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        var limit = TryParseLong(FindFirst(lookup, RateLimitHeaderNames.Limit));
        var remaining = TryParseLong(FindFirst(lookup, RateLimitHeaderNames.Remaining));
        var retryAfterSeconds = TryParseRetryAfterSeconds(FindFirst(lookup, RateLimitHeaderNames.RetryAfter));
        var resetAtUtc = TryParseReset(FindFirst(lookup, RateLimitHeaderNames.Reset), nowUtc)
            ?? (retryAfterSeconds is null ? null : nowUtc.AddSeconds(retryAfterSeconds.Value));

        var hasAny = limit is not null || remaining is not null || resetAtUtc is not null || retryAfterSeconds is not null;
        return new ParsedRateLimit(limit, remaining, resetAtUtc, retryAfterSeconds, hasAny);
    }

    private static string? FindFirst(IReadOnlyDictionary<string, string> headers, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (headers.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static long? TryParseLong(string? value)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
        {
            return parsed;
        }

        return null;
    }

    private static int? TryParseRetryAfterSeconds(string? value)
    {
        var parsed = TryParseLong(value);
        return parsed is >= 0 and <= int.MaxValue ? (int)parsed.Value : null;
    }

    private static DateTimeOffset? TryParseReset(string? value, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer) && integer >= 0)
        {
            return integer >= UnixTimestampThreshold
                ? DateTimeOffset.FromUnixTimeSeconds(integer)
                : nowUtc.AddSeconds(integer);
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
        {
            return timestamp.ToUniversalTime();
        }

        return null;
    }
}
