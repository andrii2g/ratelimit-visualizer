using RateLimitVisualizer.Api.ApiModels;
using RateLimitVisualizer.Api.Services;

namespace RateLimitVisualizer.Api.Endpoints;

public static class ObservationEndpoints
{
    public static IEndpointRouteBuilder MapObservationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/observations", async (ObservationRequest request, ObservationIngestionService service, CancellationToken cancellationToken) =>
        {
            var result = await service.IngestAsync(request, cancellationToken);
            return Results.Json(result.Response, statusCode: result.StatusCode);
        });

        return app;
    }
}
