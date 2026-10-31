param([string]$RepositoryRoot = (Get-Location).Path)

# Report arithmetic only. Never opens images or invokes the analyzer.
$ErrorActionPreference = 'Stop'
$reports = Join-Path $RepositoryRoot 'tools/Games/ArkNova/PhotoRecognition/Reports'
$summary = @()
foreach ($batch in @('calibration', 'old_holdout_reused', 'v3_reused', 'stress')) {
    if ($batch -eq 'stress') {
        $manifest = Get-Content (Join-Path $reports 'FinalAcceptance/manifest.json') -Raw | ConvertFrom-Json
        $result = Get-Content (Join-Path $reports 'FinalAcceptance/result.json') -Raw | ConvertFrom-Json
        $rows = @(foreach ($color in $manifest.groundTruth.PSObject.Properties) {
            foreach ($track in $color.Value.PSObject.Properties) {
                $prediction = $result.colors.($color.Name).tracks.($track.Name)
                [pscustomobject]@{ expected=$track.Value; a=$null; confidence=$prediction }
            }
        })
    } else {
        $result = Get-Content (Join-Path $reports "C3RefinementV5/$batch.json") -Raw | ConvertFrom-Json
        $rows = @($result.images | ForEach-Object { $_.rows } | Where-Object { $null -ne $_.expected })
    }
    $display = @($rows | ForEach-Object { $_.confidence })
    $wrong = @($rows | Where-Object { $null -ne $_.confidence.value -and $_.confidence.value -ne $_.expected })
    $greenWrong = @($wrong | Where-Object { $_.confidence.confidence -eq 'High' }).Count
    $raw = if ($batch -eq 'stress') { $null } else {
        [ordered]@{
            correct=@($rows | Where-Object { $null -ne $_.a.value -and $_.a.value -eq $_.expected }).Count
            wrong=@($rows | Where-Object { $null -ne $_.a.value -and $_.a.value -ne $_.expected }).Count
            null=@($rows | Where-Object { $null -eq $_.a.value }).Count
        }
    }
    $summary += [ordered]@{
        batch=$batch; presentValues=$rows.Count; hardenedARaw=$raw
        proposals=[ordered]@{
            correct=@($rows | Where-Object { $null -ne $_.confidence.value -and $_.confidence.value -eq $_.expected }).Count
            wrong=$wrong.Count
            null=@($display | Where-Object { $null -eq $_.value }).Count
            green=@($display | Where-Object { $null -ne $_.value -and $_.confidence -eq 'High' }).Count
            yellow=@($display | Where-Object { $null -ne $_.value -and $_.confidence -eq 'Medium' }).Count
            red=@($display | Where-Object { $null -eq $_.value -or $_.confidence -notin @('High','Medium') }).Count
            greenErrors=$greenWrong
            errorsInYellowRed=($wrong.Count - $greenWrong)
            errorFractionInYellowRed=$(if ($wrong.Count -eq 0) { $null } else { ($wrong.Count - $greenWrong) / $wrong.Count })
        }
    }
}
[ordered]@{
    scope='Present GT values only; null is separate, not counted as wrong. Confidence E may abstain after raw A. Stress analyze output contains E only, so no second raw evaluation.'
    sources=@('C3RefinementV5/calibration.json','C3RefinementV5/old_holdout_reused.json','C3RefinementV5/v3_reused.json','FinalAcceptance/result.json')
    batches=$summary
} | ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8 (Join-Path $reports 'FinalAcceptance/metrics.json')
