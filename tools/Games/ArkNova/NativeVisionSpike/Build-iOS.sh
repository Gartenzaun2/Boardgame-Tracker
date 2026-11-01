#!/usr/bin/env bash
set -euo pipefail
if [[ $# -ne 2 ]]; then
  echo 'Usage: Build-iOS.sh <official-opencv-4.13.0-source> <build-directory>' >&2
  exit 2
fi
source_dir="$(cd "$(dirname "$0")" && pwd)"
opencv_dir="$(cd "$1" && pwd)"
build_dir="$2"
stage_dir="$build_dir/opencv-install"
sdk="${ANV_IOS_SDK:-iphoneos}"
arch="${ANV_IOS_ARCH:-arm64}"
if [[ "$sdk" != iphoneos && "$sdk" != iphonesimulator ]]; then
  echo 'ANV_IOS_SDK must be iphoneos or iphonesimulator' >&2
  exit 2
fi
if [[ "$arch" != arm64 && "$arch" != x86_64 ]]; then exit 2; fi
if [[ "$sdk" == iphoneos && "$arch" != arm64 ]]; then exit 2; fi
if [[ "$(git -C "$opencv_dir" rev-parse HEAD)" != fe38fc608f6acb8b68953438a62305d8318f4fcd ]]; then
  echo 'OpenCV source must be the official 4.13.0 commit' >&2
  exit 2
fi
git -C "$opencv_dir" diff --exit-code HEAD
# Requires macOS + selected Xcode (xcode-select); never runs on Windows.
xcrun --sdk "$sdk" --show-sdk-path
cmake -S "$source_dir" -B "$build_dir" -G Xcode \
  -DOPENCV_SOURCE_DIR="$opencv_dir" -DCMAKE_SYSTEM_NAME=iOS \
  -DCMAKE_OSX_SYSROOT="$sdk" -DCMAKE_OSX_ARCHITECTURES="$arch" \
  -DCMAKE_OSX_DEPLOYMENT_TARGET=15.0 -DCMAKE_XCODE_ATTRIBUTE_CODE_SIGNING_ALLOWED=NO \
  -DCMAKE_BUILD_TYPE=Release -DANV_BUILD_RUNNER=OFF
cmake --build "$build_dir" --config Release --target arknova_vision --parallel 8

# Stage OpenCV's install tree before merging. The Xcode build tree contains
# multiple copies of some third-party codec archives; recursively merging every
# *.a from that tree duplicates object files (notably libjpeg-turbo) and causes
# duplicate-symbol failures in the final .NET iOS link.
rm -rf "$stage_dir"
cmake --install "$build_dir" --config Release --prefix "$stage_dir"

arknova_archive="$(find "$build_dir" -type f -name 'libarknova_vision.a' -print -quit)"
if [[ -z "$arknova_archive" ]]; then
  echo 'libarknova_vision.a was not produced by the iOS build' >&2
  exit 1
fi

archives=("$arknova_archive")
while IFS= read -r -d '' archive; do archives+=("$archive"); done < <(
  find "$stage_dir/lib" -type f -name '*.a' -print0
)
if [[ ${#archives[@]} -le 1 ]]; then
  echo 'No staged OpenCV static archives were found' >&2
  exit 1
fi

xcrun libtool -static -o "$build_dir/libarknova_vision_full.a" "${archives[@]}"
xcrun lipo -info "$build_dir/libarknova_vision_full.a"
xcrun nm -g "$build_dir/libarknova_vision_full.a" | grep ' _anv_probe$'
for symbol in anv_engine_create anv_engine_analyze anv_engine_destroy; do
  xcrun nm -g "$build_dir/libarknova_vision_full.a" | grep " _${symbol}$"
done
