#!/usr/bin/env bash
set -euo pipefail

requests=1000
delay_ms=10
skip_demo=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --requests)
      requests="${2:?missing value for --requests}"
      shift 2
      ;;
    --delay-ms)
      delay_ms="${2:?missing value for --delay-ms}"
      shift 2
      ;;
    --skip-demo)
      skip_demo=1
      shift
      ;;
    -h|--help)
      cat <<'HELP'
Usage: ./scripts/start.sh [--skip-demo] [--requests N] [--delay-ms N]

Starts the visualizer and mock API with Docker Compose. By default it also
runs the one-shot demo client to generate dashboard data.
HELP
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
done

cd "$(dirname "$0")/.."

docker compose up --build -d visualizer mock-api

ready=0
last_error=""
for _ in $(seq 1 45); do
  if curl -fsS --max-time 2 http://localhost:5050/health >/dev/null &&
     curl -fsS --max-time 2 http://localhost:5060/v1/account >/dev/null; then
    ready=1
    break
  fi
  last_error="$(curl -fsS --max-time 2 http://localhost:5050/health 2>&1 || true)"
  sleep 2
done

if [[ "$ready" -ne 1 ]]; then
  docker compose ps
  docker compose logs visualizer mock-api
  echo "Last health-check error: ${last_error:-unknown}" >&2
  echo "Services did not become ready within 90 seconds." >&2
  exit 1
fi

if [[ "$skip_demo" -ne 1 ]]; then
  DEMO_REQUESTS="$requests" DEMO_DELAY_MS="$delay_ms" docker compose --profile demo run --rm demo-client
fi

echo "Dashboard: http://localhost:5050"
echo "Health:    http://localhost:5050/health"

if command -v hostname >/dev/null 2>&1; then
  wsl_ip="$(hostname -I 2>/dev/null | awk '{print $1}')"
  if [[ -n "${wsl_ip:-}" && "$wsl_ip" != "127.0.0.1" ]]; then
    echo "WSL IP fallback: http://${wsl_ip}:5050"
  fi
fi
