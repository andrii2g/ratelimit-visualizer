# ratelimit-visualizer

## What it does

`ratelimit-visualizer` is a local .NET tool that collects observed API responses, stores rate-limit data in SQLite, and shows quota usage, burn rate, projections, endpoint usage, and alerts in a static dashboard.

## Screenshot / dashboard preview

Run the visualizer and demo client, then open `http://localhost:5050`.

## Features

- Collector endpoint for observed API calls.
- Support for common `RateLimit-*` and `X-RateLimit-*` headers.
- SQLite persistence.
- Static dashboard with summary cards, burn-rate chart, endpoint usage, recent observations, and alerts.
- Mock API and demo client for local traffic generation.
- Optional Docker Compose demo.

## Quick start

```bash
dotnet run --project src/RateLimitVisualizer.Api
dotnet run --project src/RateLimitVisualizer.MockApi
dotnet run --project src/RateLimitVisualizer.DemoClient -- --requests 1000 --delay-ms 10
```

Open:

```text
http://localhost:5050
```

## Run the visualizer

```bash
dotnet run --project src/RateLimitVisualizer.Api
```

The dashboard is served from `http://localhost:5050`.

## Run the mock API

```bash
dotnet run --project src/RateLimitVisualizer.MockApi
```

The mock API listens on `http://localhost:5060`.

## Generate demo traffic

```bash
dotnet run --project src/RateLimitVisualizer.DemoClient -- --requests 1000 --delay-ms 10
```

## Observation API

```bash
curl -X POST http://localhost:5050/api/observations \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "demo-api",
    "consumer": "local-demo-client",
    "method": "GET",
    "url": "http://localhost:5060/v1/items/123",
    "statusCode": 200,
    "latencyMs": 84,
    "headers": {
      "X-RateLimit-Limit": "10000",
      "X-RateLimit-Remaining": "7420",
      "X-RateLimit-Reset": "1781881685"
    }
  }'
```

## Supported rate-limit headers

- `RateLimit-Limit`
- `RateLimit-Remaining`
- `RateLimit-Reset`
- `Retry-After`
- `X-RateLimit-Limit`
- `X-RateLimit-Remaining`
- `X-RateLimit-Reset`
- `X-Rate-Limit-Limit`
- `X-Rate-Limit-Remaining`
- `X-Rate-Limit-Reset`

## Dashboard API

- `GET /api/dashboard/summary?provider=demo-api&consumer=local-demo-client`
- `GET /api/dashboard/burn-rate?provider=demo-api&consumer=local-demo-client&minutes=360`
- `GET /api/dashboard/endpoints?provider=demo-api&consumer=local-demo-client&top=10`
- `GET /api/dashboard/alerts?provider=demo-api&consumer=local-demo-client`
- `GET /health`

## How endpoint normalization works

Absolute URLs are reduced to paths, query strings and fragments are removed, numeric path segments become `:id`, GUIDs become `:uuid`, and long hex values become `:hash`.

## How burn-rate prediction works

The current burn rate is the observed request count in the rolling window divided by the window length. Projections combine the latest quota headers with that observed rate.

## Configuration

```json
{
  "Storage": {
    "ConnectionString": "Data Source=./data/ratelimit-visualizer.db"
  },
  "Dashboard": {
    "DefaultProvider": "demo-api",
    "DefaultConsumer": "local-demo-client",
    "RefreshSeconds": 5
  },
  "BurnRate": {
    "RollingWindowMinutes": 5
  }
}
```

Environment variables use normal ASP.NET Core configuration, for example `Storage__ConnectionString`.

## Docker Compose

```bash
docker compose up --build
```

## Tests

```bash
dotnet build src/RateLimitVisualizer.slnx
dotnet test src/RateLimitVisualizer.slnx
```

## Limitations

This is a local single-user tool. It is not a reverse proxy, API gateway, authentication system, or distributed quota store.

## Future ideas

Prometheus export, OpenTelemetry metrics, notifications, historical reports, CSV export, and provider-specific header profiles.
