#!/bin/bash

PROJECT_DIR="$HOME/mods/SOSInsurance/SOSInsurance"
RELEASE_DIR="$HOME/mods/SOSInsurance/release"
VERSION_FILE="$HOME/mods/SOSInsurance/version.txt"

# Read and bump version
if [ ! -f "$VERSION_FILE" ]; then
    echo "1.0.0" > "$VERSION_FILE"
fi

VERSION=$(cat "$VERSION_FILE")
IFS='.' read -r MAJOR MINOR PATCH <<< "$VERSION"
PATCH=$((PATCH + 1))
NEW_VERSION="$MAJOR.$MINOR.$PATCH"
echo "$NEW_VERSION" > "$VERSION_FILE"

echo "Building v$NEW_VERSION..."
cd "$PROJECT_DIR"
dotnet build -c Release
if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

cp bin/Release/netstandard2.1/SOSInsurance.dll "/mnt/storage/Steam Libary/steamapps/common/Delivery-Beyond/BepInEx/plugins/SOSInsurance.dll"

echo "Packaging..."
mkdir -p "$RELEASE_DIR"

# DLL
cp bin/Release/netstandard2.1/SOSInsurance.dll "$RELEASE_DIR/SOSInsurance-v$NEW_VERSION.dll"

# Source zip
cd "$PROJECT_DIR"
zip -r "$RELEASE_DIR/SOSInsurance-v$NEW_VERSION-source.zip" . \
    --exclude "*/bin/*" \
    --exclude "*/obj/*" \
    --exclude "*/decompiled/*" \
    --exclude "*/.git/*"

echo "Pushing to GitHub..."
cd "$PROJECT_DIR"
git add -A
git commit -m "Release v$NEW_VERSION"
git push

echo "Creating GitHub release..."
gh release create "v$NEW_VERSION" \
    "$RELEASE_DIR/SOSInsurance-v$NEW_VERSION.dll" \
    "$RELEASE_DIR/SOSInsurance-v$NEW_VERSION-source.zip" \
    --title "v$NEW_VERSION" \
    --notes "Release v$NEW_VERSION"

echo "Done! Released v$NEW_VERSION"
