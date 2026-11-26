#!/usr/bin/env bash
# Diagnose the intermittent "UNREACHABLE" on the first deploy after an idle spell.
#
# Run it occasionally, ideally after the connection has been unused for 30+ minutes:
#   ./check-ssh.sh
# It tests the same SSH path the deploy uses (an Ansible ping, changing nothing
# on the box) and records enough context to tell a network timeout from an
# address-lookup (ARP) problem or sshd refusing. Output is shown and also
# appended to check-ssh.log (gitignored).
#
# The order matters: the context checks run *before* the real test so they
# don't "wake" the connection first; the ICMP pings come afterwards.
set -uo pipefail

cd "$(dirname "$0")"

HOST_IP=192.168.1.221
WIFI_IF=wlan0
LOG=check-ssh.log

run() {
  echo "===== $(date '+%Y-%m-%d %H:%M:%S') ====="

  echo "--- Wi-Fi link ($WIFI_IF)"
  iw dev "$WIFI_IF" link 2>&1 | grep -E "Connected|SSID|signal|freq|Not connected" || true

  echo "--- Address cache for $HOST_IP, before"
  ip neigh show "$HOST_IP" 2>&1 || true
  echo "(empty line above = no entry)"

  echo "--- Ansible ping (same SSH path as the deploy)"
  local start end status output
  start=${EPOCHREALTIME/./}
  output=$(cd ansible && ansible -i m900-inventory.ini metracker -m ping -vvv 2>&1)
  status=$?
  end=${EPOCHREALTIME/./}
  echo "$output"
  echo "Took $(( (end - start) / 1000 ))ms, exit code $status"

  echo "--- Address cache for $HOST_IP, after"
  ip neigh show "$HOST_IP" 2>&1 || true

  echo "--- ICMP ping"
  ping -c 3 -W 2 "$HOST_IP" 2>&1 | tail -3

  echo "--- Verdict"
  if [ "$status" -eq 0 ] && grep -q '"ping": "pong"' <<<"$output"; then
    echo "OK: connected"
  else
    echo "FAILED: see the lines below"
    grep -E '"msg"|UNREACHABLE|ssh: |Connection (timed out|refused|reset|closed)|No route to host|Host is down|kex_exchange|banner' <<<"$output" | head -20
  fi
  echo
}

run 2>&1 | tee -a "$LOG"
