#!/bin/bash
set -e

# Default to net9.0 if not provided
NET_TARGET_FRAMEWORK="${1:-net9.0}"

# Save the starting directory
START_DIR=$(pwd)

echo ""
echo "=== Cleaning existing artifacts ==="
rm -rf neutron-server/artifacts

echo ""
echo "=== Cleaning previous local packages ==="
rm -rf localpackages/*

echo ""
echo "=== Rebuilding Core NuGet Package (Target: $NET_TARGET_FRAMEWORK) ==="
cd neutron-server/src/FoundryLocalCore/Core
CORE_TIMESTAMP=$(date +%Y%m%d%H%M%S)
dotnet publish -r osx-arm64 /p:Platform=arm64 /p:Configuration=Debug /p:NetTargetFramework=$NET_TARGET_FRAMEWORK /p:TargetFramework=$NET_TARGET_FRAMEWORK /p:PublishAot=true Core.csproj
./create_local_nuget_macos.sh ../../../../localpackages $CORE_TIMESTAMP

cd "$START_DIR"

echo ""
echo "=== Updating SDK to reference Core version 0.9.0-0.local.$CORE_TIMESTAMP ==="
sed -i '' "s/Include=\"Microsoft.AI.Foundry.Local.Core\" Version=\"[^\"]*\"/Include=\"Microsoft.AI.Foundry.Local.Core\" Version=\"0.9.0-0.local.$CORE_TIMESTAMP\"/" Foundry-Local/sdk/cs/src/Microsoft.AI.Foundry.Local.csproj

echo ""
echo "=== Rebuilding SDK NuGet Package ==="
cd Foundry-Local/sdk/cs
SDK_TIMESTAMP=$(date +%Y%m%d%H%M%S)
rm -rf src/bin src/obj
dotnet clean src/Microsoft.AI.Foundry.Local.csproj -c Debug -p:NetTargetFramework=$NET_TARGET_FRAMEWORK
dotnet restore src/Microsoft.AI.Foundry.Local.csproj --source ../../../localpackages -p:NetTargetFramework=$NET_TARGET_FRAMEWORK
dotnet build src/Microsoft.AI.Foundry.Local.csproj -c Debug -p:NetTargetFramework=$NET_TARGET_FRAMEWORK
dotnet pack src/Microsoft.AI.Foundry.Local.csproj -c Debug --no-build -p:NetTargetFramework=$NET_TARGET_FRAMEWORK -p:PackageVersion=0.9.0-0.local.$SDK_TIMESTAMP -o src/bin/Debug
cp src/bin/Debug/Microsoft.AI.Foundry.Local.0.9.0-0.local.$SDK_TIMESTAMP.nupkg ../../../localpackages/
cd "$START_DIR"

echo ""
echo "=== Updating Sample to reference SDK version 0.9.0-0.local.$SDK_TIMESTAMP ==="
sed -i '' "s/Include=\"Microsoft.AI.Foundry.Local\" Version=\"[^\"]*\"/Include=\"Microsoft.AI.Foundry.Local\" Version=\"0.9.0-0.local.$SDK_TIMESTAMP\"/" samples/E2E_CSharp_HuggingFace/E2E_CSharp_HuggingFace.csproj

echo ""
echo "=== Rebuilding and Running Sample ==="
cd samples/E2E_CSharp_HuggingFace
rm -rf bin obj
dotnet clean E2E_CSharp_HuggingFace.csproj -c Debug -p:NetTargetFramework=$NET_TARGET_FRAMEWORK
dotnet restore -p:NetTargetFramework=$NET_TARGET_FRAMEWORK
dotnet build E2E_CSharp_HuggingFace.csproj -c Debug -p:NetTargetFramework=$NET_TARGET_FRAMEWORK
dotnet run -c Debug /p:RuntimeIdentifier="" -p:NetTargetFramework=$NET_TARGET_FRAMEWORK
cd "$START_DIR"

echo ""
echo "=== Build Complete ==="
