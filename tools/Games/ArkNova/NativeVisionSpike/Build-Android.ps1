param(
    [Parameter(Mandatory)][string]$NdkDirectory,
    [Parameter(Mandatory)][string]$OpenCvSource,
    [string]$CMake = 'cmake',
    [string]$Ninja = 'ninja',
    [ValidateSet('arm64-v8a','x86_64')][string]$Abi = 'arm64-v8a',
    [string]$BuildDirectory = 'artifacts/native-spike/android-arm64'
)
$ErrorActionPreference = 'Stop'
$source = $PSScriptRoot
$ndk = (Resolve-Path -LiteralPath $NdkDirectory).Path
$opencv = (Resolve-Path -LiteralPath $OpenCvSource).Path
$revision = git -C $opencv rev-parse HEAD
if ($LASTEXITCODE -ne 0 -or $revision -ne 'fe38fc608f6acb8b68953438a62305d8318f4fcd') {
    throw 'OpenCV source must be the official 4.13.0 commit'
}
git -C $opencv diff --quiet HEAD
if ($LASTEXITCODE -ne 0) { throw 'OpenCV source has local tracked changes' }
& $CMake -S $source -B $BuildDirectory -G Ninja `
    "-DCMAKE_MAKE_PROGRAM=$Ninja" "-DOPENCV_SOURCE_DIR=$opencv" `
    "-DCMAKE_TOOLCHAIN_FILE=$ndk/build/cmake/android.toolchain.cmake" `
    "-DANDROID_ABI=$Abi" -DANDROID_PLATFORM=android-30 -DANDROID_STL=c++_static `
    -DCMAKE_BUILD_TYPE=Release -DANV_BUILD_RUNNER=ON
if ($LASTEXITCODE -ne 0) { throw 'Android configure failed' }
& $CMake --build $BuildDirectory --target arknova_vision arknova_probe --parallel 8
if ($LASTEXITCODE -ne 0) { throw 'Android build failed' }
$strip = Join-Path $ndk 'toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-strip.exe'
& $strip --strip-unneeded (Join-Path $BuildDirectory 'libarknova_vision.so')
if ($LASTEXITCODE -ne 0) { throw 'Android release strip failed' }
