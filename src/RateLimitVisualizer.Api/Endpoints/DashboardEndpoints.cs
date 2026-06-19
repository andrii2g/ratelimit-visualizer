using RateLimitVisualizer.Api.ApiModels;
using RateLimitVisualizer.Api.Services;

namespace RateLimitVisualizer.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard");

        group.MapGet("/summary", async (string provider, string consumer, DashboardQueryService service, CancellationToken cancellationToken) =>
            DashboardSummaryResponse.FromSummary(await service.GetSummaryAsync(provider, consumer, cancellationToken)));

        group.MapGet("/burn-rate", async (string provider, string consumer, int? minutes, DashboardQueryService service, CancellationToken cancellationToken) =>
            new BurnRateResponse(provider, consumer, await service.GetBurnRateAsync(provider, consumer, minutes ?? 360, cancellationToken)));

        group.MapGet("/endpoints", async (string provider, string consumer, int? top, DashboardQueryService service, CancellationToken cancellationToken) =>
            new EndpointUsageResponse(provider, consumer, await service.GetEndpointUsageAsync(provider, consumer, top ?? 10, cancellationToken)));

        group.MapGet("/alerts", async (string provider, string consumer, DashboardQueryService service, CancellationToken cancellationToken) =>
            new AlertsResponse(provider, consumer, await service.GetAlertsAsync(provider, consumer, cancellationToken)));

        return app;
    }
}
