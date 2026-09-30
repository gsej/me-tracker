#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PRIVATE_DIR="$SCRIPT_DIR/../me-tracker-private"
ENV_FILE="$PRIVATE_DIR/.env"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Error: .env file not found at $ENV_FILE" >&2
  exit 1
fi

api_keys_json=$(grep '^ApiKeys=' "$ENV_FILE" | cut -d'=' -f2-)
api_key=$(echo "$api_keys_json" | jq -r '.[] | select(.UserId == "gsej") | .Key')

if [[ -z "$api_key" ]]; then
  echo "Error: could not find API key for gsej in $ENV_FILE" >&2
  exit 1
fi

url="http://localhost:5200/api/backup"
output_file="$PRIVATE_DIR/backups/backup_$(date -u +%Y-%m-%dT%H%M%SZ).json"

echo "Requesting $url"
http_code=$(curl -s -o "$output_file" -w "%{http_code}" \
  -H "X-Api-Key: $api_key" \
  "$url") || curl_exit=$?

if [[ "${curl_exit:-0}" -ne 0 ]]; then
  echo "Error: curl failed (exit ${curl_exit:-0}) — is the API running?" >&2
  rm -f "$output_file"
  exit 1
fi

if [[ "$http_code" != "200" ]]; then
  echo "Error: backup request failed with HTTP $http_code" >&2
  echo "Response body:" >&2
  cat "$output_file" >&2
  rm -f "$output_file"
  exit 1
fi

jq . "$output_file" > "$output_file.tmp" && mv "$output_file.tmp" "$output_file"

echo "Backup written to $output_file"
