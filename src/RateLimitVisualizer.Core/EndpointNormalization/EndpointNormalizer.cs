using System.Text.RegularExpressions;

namespace RateLimitVisualizer.Core.EndpointNormalization;

public sealed partial class EndpointNormalizer
{
    public string Normalize(string? urlOrPath)
    {
        if (string.IsNullOrWhiteSpace(urlOrPath))
        {
            return "/";
        }

        var value = urlOrPath.Trim();
        string path;

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute) && !string.IsNullOrWhiteSpace(absolute.Host))
        {
            path = absolute.AbsolutePath;
        }
        else
        {
            var queryIndex = value.IndexOfAny(['?', '#']);
            path = queryIndex >= 0 ? value[..queryIndex] : value;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        if (path.Length > 1)
        {
            path = path.TrimEnd('/');
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeSegment);

        return "/" + string.Join('/', segments);
    }

    private static string NormalizeSegment(string segment)
    {
        var unescaped = Uri.UnescapeDataString(segment);

        if (IntegerSegmentRegex().IsMatch(unescaped))
        {
            return ":id";
        }

        if (Guid.TryParse(unescaped, out _))
        {
            return ":uuid";
        }

        if (HashSegmentRegex().IsMatch(unescaped))
        {
            return ":hash";
        }

        return unescaped;
    }

    [GeneratedRegex("^[0-9]+$")]
    private static partial Regex IntegerSegmentRegex();

    [GeneratedRegex("^[a-fA-F0-9]{16,}$")]
    private static partial Regex HashSegmentRegex();
}
