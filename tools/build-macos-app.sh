#!/usr/bin/env bash
#
# Packages the desktop app into a proper macOS .app bundle so the Dock shows
# the Knowledge Base lightbulb icon. Run from the repository root:
#
#   bash tools/build-macos-app.sh
#
# Produces: dist/KnowledgeBase.app
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP_NAME="KnowledgeBase"

# Auto-detect the CPU so the bundle matches the host; override with RID=osx-x64 / osx-arm64
if [[ "${RID:-}" == "" ]]; then
  case "$(uname -m)" in
    arm64|aarch64) RID="osx-arm64" ;;
    *)             RID="osx-x64" ;;
  esac
fi

BUILD="${BUILD:-Release}"
PUBLISH_DIR="$ROOT/dist/publish"
APP_BUNDLE="$ROOT/dist/$APP_NAME.app"

echo "Publishing desktop app ($RID, $BUILD)..."
dotnet publish "$ROOT/src/Desktop/KnowledgeBase.Desktop.csproj" \
    -c "$BUILD" \
    -r "$RID" \
    --self-contained true \
    -o "$PUBLISH_DIR"

echo "Assembling $APP_BUNDLE..."
EXE="$PUBLISH_DIR/KnowledgeBase.Desktop"

mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Executable -> bundled main binary
cp "$EXE" "$APP_BUNDLE/Contents/MacOS/$APP_NAME"

# Copy all runtime files (dlls, config) alongside the bundle binary
# (keep the standard layout; Avalonia resolves content files here)
cp -R "$PUBLISH_DIR/"* "$APP_BUNDLE/Contents/MacOS/"

# Dock icon
cp "$ROOT/src/Desktop/Assets/bulb.icns" "$APP_BUNDLE/Contents/Resources/$APP_NAME.icns"

cat > "$APP_BUNDLE/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>Knowledge Base</string>
    <key>CFBundleIdentifier</key>
    <string>com.knowledgebase.app</string>
    <key>CFBundleVersion</key>
    <string>1.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>CFBundleIconFile</key>
    <string>$APP_NAME</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
</dict>
</plist>
PLIST

# Sign ad-hoc so macOS permits launching the bundle
codesign --force --deep --sign - "$APP_BUNDLE" >/dev/null 2>&1 || true

chmod +x "$APP_BUNDLE/Contents/MacOS/$APP_NAME"

echo
echo "Done. Launch with:"
echo "  open $APP_BUNDLE"
echo "or in CLI:"
echo "  $APP_BUNDLE/Contents/MacOS/$APP_NAME"