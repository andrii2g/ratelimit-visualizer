using RateLimitVisualizer.Api.ApiModels;
using RateLimitVisualizer.Api.Storage;
using RateLimitVisualizer.Core.EndpointNormalization;
using RateLimitVisualizer.Core.HeaderParsing;
using RateLimitVisualizer.Core.Models;
using RateLimitVisualizer.Core.Time;

namespace RateLimitVisualizer.Api.Services;

public sealed class ObservationIngestionService(
    RateLimitHeaderParser parser,
    EndpointNormalizer normalizer,
    IObservationRepository repository,
    IClock clock)
{
    public async Task<(int StatusCode, ObservationResponse Response)> IngestAsync(ObservationRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return (StatusCodes.Status400BadRequest, new ObservationResponse(false, Reason: validationError));
        }

        var nowUtc = clock.UtcNow;
        var observedAtUtc = (request.ObservedAtUtc ?? nowUtc).ToUniversalTime();
        var parsed = parser.Parse(request.Headers, observedAtUtc);

        if (parsed.Limit is null && parsed.Remaining is null)
        {
            return (StatusCodes.Status202Accepted, new ObservationResponse(false, Reason: "No supported rate-limit headers were found."));
        }

        var endpointTemplate = string.IsNullOrWhiteSpace(request.EndpointTemplate)
            ? normalizer.Normalize(request.Url)
            : request.EndpointTemplate.Trim();

        var observation = new ObservedApiCall(
            request.Provider!.Trim(),
            request.Consumer!.Trim(),
            request.Method!.Trim().ToUpperInvariant(),
            request.Url,
            endpointTemplate,
            request.StatusCode!.Value,
            request.LatencyMs,
            parsed.Limit,
            parsed.Remaining,
            parsed.ResetAtUtc,
            parsed.RetryAfterSeconds,
            observedAtUtc);

        await repository.InsertAsync(observation, nowUtc, cancellationToken);

        return (StatusCodes.Status200OK, new ObservationResponse(true, observation.Provider, observation.Consumer, observation.EndpointTemplate, observation.Limit, observation.Remaining, observation.ResetAtUtc));
    }

    private static string? Validate(ObservationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            return "provider is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Consumer))
        {
            return "consumer is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Method))
        {
            return "method is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Url) && string.IsNullOrWhiteSpace(request.EndpointTemplate))
        {
            return "url or endpointTemplate is required.";
        }

        if (request.StatusCode is null)
        {
            return "statusCode is required.";
        }

        if (request.Headers is null)
        {
            return "headers is required.";
        }

        return null;
    }
}
