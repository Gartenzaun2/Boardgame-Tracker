#!/usr/bin/env bash
set -euo pipefail
if [[ $# -ne 2 ]]; then
  echo 'Usage: Build-iOS.sh <official-opencv-4.13.0-source> <build-directory>' >&2
  exit 2
fi
source_dir="$(cd "$(dirname "$0")" && pwd)"
opencv_dir="$(cd "$1" && pwd)"
if [[ "$(git -C "$opencv_dir" rev-parse HEAD)" != fe38fc608f6acb8b68953438a62305d8318f4fcd ]]; then
  echo 'OpenCV source must be the official 4.13.0 commit' >&2
  exit 2
fi
git -C "$opencv_dir" diff --exit-code HEAD
# Requires macOS + selected Xcode (xcode-select); never runs on Windows.
xcrun --sdk iphoneos --show-sdk-path
cmake -S "$source_dir" -B "$2" -G Xcode \
  -DOPENCV_SOURCE_DIR="$opencv_dir" -DCMAKE_SYSTEM_NAME=iOS \
  -DCMAKE_OSX_SYSROOT=iphoneos -DCMAKE_OSX_ARCHITECTURES=arm64 \
  -DCMAKE_OSX_DEPLOYMENT_TARGET=15.0 -DCMAKE_XCODE_ATTRIBUTE_CODE_SIGNING_ALLOWED=NO \
  -DCMAKE_BUILD_TYPE=Release -DANV_BUILD_RUNNER=OFF
cmake --build "$2" --config Release --target arknova_vision --parallel 8
# Build one archive for .NET's NativeReference. The individual OpenCV archives
# are dependencies, not contents of libarknova_vision.a until explicitly merged.
archives=()
while IFS= read -r -d '' archive; do archives+=("$archive"); done < <(
  find "$2" -type f -name '*.a' ! -name 'libarknova_vision_full.a' -print0
)
if [[ ${#archives[@]} -eq 0 ]]; then exit 1; fi
xcrun libtool -static -o "$2/libarknova_vision_full.a" "${archives[@]}"
xcrun lipo -info "$2/libarknova_vision_full.a"
xcrun nm -g "$2/libarknova_vision_full.a" | grep ' _anv_probe$'
