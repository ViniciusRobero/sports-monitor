#!/usr/bin/env bash
set -euo pipefail

: "${VM_USER:?Set VM_USER before running deploy.sh}"
: "${VM_IP:?Set VM_IP before running deploy.sh}"

APP_DIR="${APP_DIR:-/opt/sportsmonitor}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

"$ROOT_DIR/publish-linux.sh"

echo "==> Preparing remote directory..."
ssh "$VM_USER@$VM_IP" "sudo mkdir -p '$APP_DIR' && sudo chown '$VM_USER:$VM_USER' '$APP_DIR'"

echo "==> Syncing publish-linux/ to $VM_IP:$APP_DIR..."
rsync -avz --delete "$ROOT_DIR/publish-linux/" "$VM_USER@$VM_IP:$APP_DIR/"

if [ -f "$ROOT_DIR/appsettings.Production.json" ]; then
  echo "==> Uploading local appsettings.Production.json..."
  scp "$ROOT_DIR/appsettings.Production.json" "$VM_USER@$VM_IP:$APP_DIR/"
else
  echo "==> No local appsettings.Production.json found; keeping remote production config."
fi

echo "==> Restarting sportsmonitor service..."
ssh "$VM_USER@$VM_IP" "sudo systemctl restart sportsmonitor"

echo "==> Deploy complete: http://$VM_IP/"
