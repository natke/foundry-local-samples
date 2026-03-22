#!/bin/bash
set -e

# Default to net9.0 if not provided
NET_TARGET_FRAMEWORK="${1:-net9.0}"

# Save the starting directory
START_DIR=$(pwd)

echo ""
echo "=== Cleaning existing Core artifacts ==="
rm -rf neutron-server/artifacts

echo ""
echo "=== Rebuilding Core Native Library (Target: $NET_TARGET_FRAMEWORK) ==="
cd neutron-server/src/FoundryLocalCore/Core
dotnet publish -r osx-arm64 /p:Platform=arm64 /p:Configuration=Debug /p:NetTargetFramework=$NET_TARGET_FRAMEWORK /p:TargetFramework=$NET_TARGET_FRAMEWORK /p:PublishAot=true Core.csproj
cd "$START_DIR"

echo ""
echo "=== Building JS SDK (skipping post-install NuGet download, using local Core) ==="
cd Foundry-Local/sdk/js
npm install --ignore-scripts
npm run build
cd "$START_DIR"

echo ""
echo "=== Setting up E2E sample ==="
cd samples/E2E_JavaScript_HuggingFace
rm -rf node_modules
npm install --ignore-scripts
cd "$START_DIR"

echo ""
echo "=== Running JS E2E Test ==="
cd samples/E2E_JavaScript_HuggingFace
npx tsx test_huggingface_e2e.ts
cd "$START_DIR"

echo ""
echo "=== JS Build Complete ==="
