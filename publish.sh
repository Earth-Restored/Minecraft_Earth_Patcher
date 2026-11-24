#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

CONFIG="Release"
FRAMEWORKS=("win-x64" "win-arm64" "linux-x64" "linux-arm64" "osx-x64" "osx-arm64")
ANDROID_ABIS=("android-arm64" "android-x64")
SKIP_DESKTOP=false
SKIP_ANDROID=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        -c|--config)
            CONFIG="$2"
            shift 2
            ;;
        --desktop-only|--desktop)
            SKIP_ANDROID=true
            shift
            ;;
        --android-only|--android)
            SKIP_DESKTOP=true
            shift
            ;;
        --skip-desktop)
            SKIP_DESKTOP=true
            shift
            ;;
        --skip-android)
            SKIP_ANDROID=true
            shift
            ;;
        -h|--help)
            echo "Usage: ./publish.sh [options]"
            echo "Options:"
            echo "  -c, --config <Release|Debug>  Build configuration (default: Release)"
            echo "  --desktop-only                Build only desktop targets"
            echo "  --android-only                Build only Android APK targets"
            echo "  --skip-desktop                Skip desktop build"
            echo "  --skip-android                Skip Android APK build"
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            exit 1
            ;;
    esac
done

VERSION="1.0.0"
PROPS_FILE="$SCRIPT_DIR/Directory.Build.props"
if [[ -f "$PROPS_FILE" ]]; then
    PARSED_VER=$(sed -n -e 's/.*<Version>\(.*\)<\/Version>.*/\1/p' -e 's/.*<VersionPrefix>\(.*\)<\/VersionPrefix>.*/\1/p' "$PROPS_FILE" | head -n 1 | tr -d '[:space:]')
    if [[ -n "$PARSED_VER" ]]; then
        VERSION="$PARSED_VER"
    fi
fi

OUT_DIR="$SCRIPT_DIR/build/$CONFIG"
mkdir -p "$OUT_DIR"

if [[ "$SKIP_DESKTOP" == false ]]; then
    DESKTOP_PROJ="$SCRIPT_DIR/src/UI.Desktop/MCEPatcher.UI.Desktop.csproj"
    DISPLAY_NAME="MCEPatcher_UI"

    for fx in "${FRAMEWORKS[@]}"; do
        echo -e "\033[36mPublishing $DISPLAY_NAME for $fx ($CONFIG)...\033[0m"
        ZIP_NAME="$DISPLAY_NAME-$fx-$VERSION.zip"
        ZIP_PATH="$OUT_DIR/$ZIP_NAME"
        TEMP_DIR=$(mktemp -d "$SCRIPT_DIR/temp_pub_XXXXXX")

        trap 'rm -rf "$TEMP_DIR"' EXIT

        dotnet publish "$DESKTOP_PROJ" \
            --configuration "$CONFIG" \
            --runtime "$fx" \
            --output "$TEMP_DIR" \
            --self-contained \
            -p:PublishSingleFile=true \
            -p:IncludeNativeLibrariesForSelfExtract=true \
            -p:PublishTrimmed=false \
            -p:DebugType=None \
            -p:DebugSymbols=false

        rm -f "$ZIP_PATH"
        (cd "$TEMP_DIR" && zip -q -9 -r "$ZIP_PATH" .)
        rm -rf "$TEMP_DIR"
        trap - EXIT

        echo -e "\033[32mSuccess: $ZIP_PATH\033[0m\n"
    done
fi

if [[ "$SKIP_ANDROID" == false ]]; then
    ANDROID_PROJ="$SCRIPT_DIR/src/UI.Android/MCEPatcher.UI.Android.csproj"

    for abi in "${ANDROID_ABIS[@]}"; do
        echo -e "\033[36mPublishing Android APK for $abi ($CONFIG)...\033[0m"

        dotnet publish "$ANDROID_PROJ" \
            --configuration "$CONFIG" \
            -r "$abi"

        ABI_SHORT="arm64"
        if [[ "$abi" == "android-x64" ]]; then
            ABI_SHORT="x64"
        fi

        ANDROID_OUT_DIR="$SCRIPT_DIR/src/UI.Android/bin/$CONFIG/net11.0-android/$abi"
        APK_PATH=$(find "$ANDROID_OUT_DIR/publish" -maxdepth 1 -name "*-Signed.apk" 2>/dev/null | head -n 1 || true)
        if [[ -z "$APK_PATH" ]]; then
            APK_PATH=$(find "$ANDROID_OUT_DIR/publish" -maxdepth 1 -name "*.apk" 2>/dev/null | head -n 1 || true)
        fi
        if [[ -z "$APK_PATH" ]]; then
            APK_PATH=$(find "$ANDROID_OUT_DIR" -name "*-Signed.apk" 2>/dev/null | head -n 1 || true)
        fi
        if [[ -z "$APK_PATH" ]]; then
            APK_PATH=$(find "$ANDROID_OUT_DIR" -name "*.apk" 2>/dev/null | head -n 1 || true)
        fi

        if [[ -n "$APK_PATH" && -f "$APK_PATH" ]]; then
            TARGET_APK="$OUT_DIR/MCEPatcher-$ABI_SHORT-$VERSION.apk"
            cp -f "$APK_PATH" "$TARGET_APK"
            echo -e "\033[32mSuccess: $TARGET_APK\033[0m\n"
        else
            echo -e "\033[33mWarning: Could not find built APK for $abi\033[0m" >&2
        fi
    done
fi

echo -e "\033[32mAll publish tasks completed successfully!\033[0m"
