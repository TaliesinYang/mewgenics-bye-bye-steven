#!/bin/bash
# Build speedhack.dll using MSYS2 UCRT64 GCC
# Requires: pacman -S mingw-w64-ucrt-x86_64-gcc
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "Building speedhack.dll..."
gcc -shared -O2 -o speedhack.dll speedhack.c -lpsapi
echo "Build complete: speedhack.dll"
