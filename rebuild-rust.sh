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

CORE_PUBLISH_DIR="$START_DIR/neutron-server/artifacts/publish/Core/debug_net9.0_osx-arm64"
LOCAL_CORE_DYLIB="$CORE_PUBLISH_DIR/Microsoft.AI.Foundry.Local.Core.dylib"

echo ""
echo "=== Building Rust SDK ==="
cd Foundry-Local/sdk/rust
cargo build
cd "$START_DIR"

echo ""
echo "=== Building Rust E2E Sample ==="
cd samples/E2E_Rust_HuggingFace
cargo build
cd "$START_DIR"

echo ""
echo "=== Replacing NuGet Core with locally-built Core ==="
# Find ALL OUT_DIR locations where build.rs placed NuGet-downloaded Core,
# and replace them with our locally-built Core so the default library
# resolution picks up our changes without needing DYLD_LIBRARY_PATH overrides.
find samples/E2E_Rust_HuggingFace/target/debug/build -name "Microsoft.AI.Foundry.Local.Core.dylib" -print0 | while IFS= read -r -d '' nug_core; do
    echo "  Replacing: $nug_core"
    cp "$LOCAL_CORE_DYLIB" "$nug_core"
done

echo ""
echo "=== Running Rust E2E Test ==="
cd samples/E2E_Rust_HuggingFace
cargo run
cd "$START_DIR"

echo ""
echo "=== Rust Build Complete ==="
