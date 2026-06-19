using RateLimitVisualizer.Core.EndpointNormalization;

namespace RateLimitVisualizer.Tests.EndpointNormalization;

public sealed class EndpointNormalizerTests
{
    private readonly EndpointNormalizer _normalizer = new();

    [Theory]
    [InlineData("http://localhost:5060/v1/items/123?include=details", "/v1/items/:id")]
    [InlineData("/v1/users/550e8400-e29b-41d4-a716-446655440000", "/v1/users/:uuid")]
    [InlineData("/v1/files/ab12cd34ef56ab12cd34", "/v1/files/:hash")]
    [InlineData("/v1/search?q=test", "/v1/search")]
    [InlineData("/", "/")]
    [InlineData("v1/items/123/", "/v1/items/:id")]
    public void Normalize_rewrites_variable_segments(string input, string expected)
    {
        Assert.Equal(expected, _normalizer.Normalize(input));
    }
}
