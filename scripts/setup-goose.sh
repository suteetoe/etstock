#!/usr/bin/env bash
# ETStock — ตั้งค่า goose ระดับ project ให้ทีม
# ใช้: ./scripts/setup-goose.sh
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# 1) หา config dir ระดับเครื่องตาม OS
case "$OSTYPE" in
  darwin*|linux-gnu*) CONFIG_DIR="$HOME/.config/goose" ;;
  msys*|win32*)       CONFIG_DIR="$APPDATA/Block/goose/config" ;;
  *) echo "Unsupported OS: $OSTYPE" >&2; exit 1 ;;
esac
mkdir -p "$CONFIG_DIR"

# 2) สำรอง config เดิม แล้ววาง team config (ถามก่อนทับ)
if [ -f "$CONFIG_DIR/config.yaml" ]; then
  cp "$CONFIG_DIR/config.yaml" "$CONFIG_DIR/config.yaml.bak.$(date +%s)"
  echo "สำรอง config.yaml เดิมไว้แล้ว"
fi
cp "$REPO_ROOT/.goose/config.yaml" "$CONFIG_DIR/config.yaml"
echo "วาง team config ไปที่ $CONFIG_DIR/config.yaml แล้ว"

# 3) ชี้ให้ goose เห็น recipe ในโปรเจกต์
echo ""
echo "ใส่บรรทัดนี้ใน ~/.bashrc / ~/.zshrc หรือใช้ direnv:"
echo "  export GOOSE_RECIPE_PATH=\"$REPO_ROOT/recipes\""
echo ""
echo "อย่าลืม: cp .env.example .env แล้วเติมคีย์จริง + 'gh auth login'"
