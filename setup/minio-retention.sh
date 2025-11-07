#!/usr/bin/env bash
set -euo pipefail
# Requires 'mc' (MinIO client) installed on host
mc alias set local http://localhost:9000 hlvminio hlvminiopass
mc mb -p local/hearloveen || true
cat > lifecycle.json <<EOF
{
  "Rules": [{
    "ID": "expire-audio",
    "Status": "Enabled",
    "Expiration": {"Days": 60},
    "Filter": {"Prefix": "audio/"}
  }]
}
EOF
mc ilm import local/hearloveen < lifecycle.json
echo "Lifecycle set: 60-day expiration for audio/ prefix"