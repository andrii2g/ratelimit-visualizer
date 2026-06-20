#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

echo "Docker:"
docker --version || true
docker compose version || true

echo
echo "Compose services:"
docker compose ps || true

echo
echo "Published ports:"
docker compose port visualizer 8080 || true
docker compose port mock-api 8080 || true

echo
echo "Host checks:"
curl -v --max-time 5 http://localhost:5050/health || true
curl -v --max-time 5 http://127.0.0.1:5050/health || true

if command -v hostname >/dev/null 2>&1; then
  wsl_ip="$(hostname -I 2>/dev/null | awk '{print $1}')"
  if [[ -n "${wsl_ip:-}" ]]; then
    echo
    echo "WSL IP check: ${wsl_ip}"
    curl -v --max-time 5 "http://${wsl_ip}:5050/health" || true
  fi
fi

echo
echo "Logs:"
docker compose logs --tail=120 visualizer mock-api || true
