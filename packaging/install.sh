#!/usr/bin/env bash
# Installs the SendIt CLI on Linux/macOS.
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/matthewratcliffe/sendit/main/packaging/install.sh | bash
set -euo pipefail

REPO="matthewratcliffe/sendit"
INSTALL_DIR="${SENDIT_INSTALL_DIR:-$HOME/.local/bin}"

os="$(uname -s)"
arch="$(uname -m)"

case "$os" in
  Linux)  platform="linux" ;;
  Darwin) platform="osx" ;;
  *) echo "Unsupported OS: $os" >&2; exit 1 ;;
esac

case "$arch" in
  x86_64|amd64) rid="${platform}-x64" ;;
  arm64|aarch64) rid="${platform}-arm64" ;;
  *) echo "Unsupported architecture: $arch" >&2; exit 1 ;;
esac

echo "Fetching latest SendIt release ($rid)..."
download_url=$(curl -fsSL "https://api.github.com/repos/$REPO/releases/latest" \
  | grep -o "\"browser_download_url\": *\"[^\"]*sendit-${rid}\\.tar\\.gz\"" \
  | sed -E 's/.*"(https[^"]+)"/\1/')

if [ -z "$download_url" ]; then
  echo "Could not find a release asset matching 'sendit-${rid}.tar.gz' for $REPO." >&2
  echo "See https://github.com/$REPO/releases" >&2
  exit 1
fi

mkdir -p "$INSTALL_DIR"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

echo "Downloading $download_url..."
curl -fsSL "$download_url" -o "$tmp/sendit.tar.gz"

echo "Installing to $INSTALL_DIR..."
tar -xzf "$tmp/sendit.tar.gz" -C "$tmp"
install -m 755 "$tmp/sendit" "$INSTALL_DIR/sendit"

if ! command -v sendit >/dev/null 2>&1; then
  echo ""
  echo "SendIt was installed to $INSTALL_DIR, which is not on your PATH."
  echo "Add this to your shell profile (~/.bashrc, ~/.zshrc, etc.):"
  echo "  export PATH=\"$INSTALL_DIR:\$PATH\""
fi

echo "SendIt installed. Run 'sendit --version' to verify."
