param(
    [Parameter(Mandatory = $true)]
    [string]$Root
)

$ErrorActionPreference = 'Stop'

function Get-SafePath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $value = $RelativePath.Trim()
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw 'Empty path in operations file.'
    }
    if ([System.IO.Path]::IsPathRooted($value)) {
        throw "Absolute path rejected: $value"
    }

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $full = [System.IO.Path]::GetFullPath((Join-Path $Root $value))
    if (-not $full.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path outside repository rejected: $value"
    }
    return $full
}

$rootFull = [System.IO.Path]::GetFullPath($Root)
$updateFile = Join-Path $rootFull 'UPDATE_FILES.txt'
$deleteFile = Join-Path $rootFull 'DELETE_FILES.txt'

if (Test-Path -LiteralPath $updateFile) {
    $opsFile = $updateFile
} elseif (Test-Path -LiteralPath $deleteFile) {
    $opsFile = $deleteFile
} else {
    Write-Host 'No UPDATE_FILES.txt or DELETE_FILES.txt found. Nothing to do.'
    exit 0
}

Write-Host "Applying file operations from $opsFile ..."

foreach ($rawLine in [System.IO.File]::ReadAllLines($opsFile)) {
    $line = $rawLine.Trim()
    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }

    $parts = $line.Split('|')
    $knownOperation = $parts.Count -ge 2 -and $parts[0].Trim().ToUpperInvariant() -in @('DELETE', 'MOVE', 'RENAME')

    if ($knownOperation) {
        $operation = $parts[0].Trim().ToUpperInvariant()
        $sourceRelative = $parts[1].Trim()
        $destinationRelative = if ($parts.Count -ge 3) { $parts[2].Trim() } else { $null }
    } else {
        # Legacy DELETE_FILES.txt format: one relative path per line.
        $operation = 'DELETE'
        $sourceRelative = $line
        $destinationRelative = $null
    }

    $source = Get-SafePath $sourceRelative

    switch ($operation) {
        'DELETE' {
            if (Test-Path -LiteralPath $source) {
                Write-Host "DELETE: $sourceRelative"
                Remove-Item -LiteralPath $source -Force -Recurse
            } else {
                Write-Host "SKIP missing: $sourceRelative"
            }
        }
        { $_ -in @('MOVE', 'RENAME') } {
            if ([string]::IsNullOrWhiteSpace($destinationRelative)) {
                throw "$operation requires source and destination: $line"
            }
            $destination = Get-SafePath $destinationRelative
            if (-not (Test-Path -LiteralPath $source)) {
                throw "Source not found: $sourceRelative"
            }
            if (Test-Path -LiteralPath $destination) {
                throw "Destination already exists: $destinationRelative"
            }
            $parent = Split-Path -Parent $destination
            if (-not (Test-Path -LiteralPath $parent)) {
                New-Item -ItemType Directory -Path $parent -Force | Out-Null
            }
            Write-Host "$operation`: $sourceRelative -> $destinationRelative"
            Move-Item -LiteralPath $source -Destination $destination
        }
        default {
            throw "Unknown operation: $operation"
        }
    }
}
