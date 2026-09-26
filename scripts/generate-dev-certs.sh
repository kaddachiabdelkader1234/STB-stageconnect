#!/usr/bin/env sh
# Generates a self-signed TLS certificate for local HTTPS (docker compose --profile tls).
#
# The certificate is a DEVELOPMENT artifact: browsers will show a warning for it, and it must
# never be used in production. Output goes to nginx/certs/, which is gitignored.
#
# Usage:
#   bash scripts/generate-dev-certs.sh
#
# Produces:
#   nginx/certs/tls.crt  — certificate (valid 825 days, localhost + 127.0.0.1 + stb.local)
#   nginx/certs/tls.key  — private key

set -eu

CERT_DIR="$(cd "$(dirname "$0")/.." && pwd)/nginx/certs"
mkdir -p "$CERT_DIR"

if [ -f "$CERT_DIR/tls.crt" ] && [ -f "$CERT_DIR/tls.key" ]; then
  echo "Certificates already exist in $CERT_DIR — delete them first to regenerate."
  exit 0
fi

if ! command -v openssl >/dev/null 2>&1; then
  echo "openssl introuvable. Installez-le (ou genereez le certificat manuellement) puis relancez." >&2
  exit 1
fi

echo "Generating self-signed certificate in $CERT_DIR ..."

openssl req -x509 -nodes -newkey rsa:2048 \
  -days 825 \
  -keyout "$CERT_DIR/tls.key" \
  -out "$CERT_DIR/tls.crt" \
  -subj "/C=TN/ST=Tunis/L=Tunis/O=STB/OU=Gestion des Stagiaires/CN=localhost" \
  -addext "subjectAltName=DNS:localhost,DNS:stb.local,IP:127.0.0.1"

chmod 600 "$CERT_DIR/tls.key" || true

echo "Done. Start the stack with:"
echo "  docker compose --profile tls up -d"
