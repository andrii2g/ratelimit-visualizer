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

From the repository root, start the visualizer, mock API, and demo traffic with Docker Compose:

```bash
bash ./scripts/start.sh
```

Open:

```text
http://localhost:5050
```

Run tests in Docker:

```bash
bash ./scripts/test.sh
```

Stop services and remove local runtime data:

```bash
bash ./scripts/cleanup.sh
```

If the browser cannot reach `http://localhost:5050`, run:

```bash
bash ./scripts/doctor.sh
```

On some WSL + Docker setups, Windows localhost forwarding is not available. In that case, use the `WSL IP fallback` URL printed by `start.sh` or `doctor.sh`, for example `http://172.x.x.x:5050`.

If `curl http://localhost:5050` works inside WSL but the Windows browser cannot open `http://localhost:5050`, the app is running and the issue is Windows-to-WSL forwarding. Use one of these options:

1. Open the WSL IP fallback URL from Windows, for example `http://172.x.x.x:5050`.
2. If you need Windows `localhost:5050`, create a Windows portproxy from an Administrator PowerShell window:

```powershell
$wslIp = wsl hostname -I
$wslIp = $wslIp.Trim().Split()[0]
netsh interface portproxy delete v4tov4 listenaddress=127.0.0.1 listenport=5050
netsh interface portproxy add v4tov4 listenaddress=127.0.0.1 listenport=5050 connectaddress=$wslIp connectport=5050
```

Repeat the portproxy command after WSL restarts if the WSL IP changes.

### Quickstart options

Skip demo traffic when starting services:

```bash
bash ./scripts/start.sh --skip-demo
```

Generate a smaller or faster demo run:

```bash
bash ./scripts/start.sh --requests 250 --delay-ms 1
```

The dashboard is served from `http://localhost:5050`. The mock API is available on `http://localhost:5060`.

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

The scripts wrap these Compose services:

- `visualizer`: the dashboard and collector API on host port `5050`.
- `mock-api`: the fake external API on host port `5060`.
- `demo-client`: one-shot traffic generator, enabled by the `demo` profile.
- `tests`: one-shot `dotnet test src/RateLimitVisualizer.slnx`, enabled by the `test` profile.

Equivalent raw Compose commands:

```bash
docker compose up --build -d visualizer mock-api
docker compose --profile demo run --rm demo-client
docker compose --profile test run --rm tests
docker compose down --remove-orphans
```

## Tests

```bash
bash ./scripts/test.sh
```

## Limitations

This is a local single-user tool. It is not a reverse proxy, API gateway, authentication system, or distributed quota store.

## Future ideas

Prometheus export, OpenTelemetry metrics, notifications, historical reports, CSV export, and provider-specific header profiles.
