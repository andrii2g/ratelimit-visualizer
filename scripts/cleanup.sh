#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

docker compose down --remove-orphans
rm -rf src/RateLimitVisualizer.Api/data
