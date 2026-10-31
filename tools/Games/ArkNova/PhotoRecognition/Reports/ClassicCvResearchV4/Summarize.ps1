param([string]$RepositoryRoot = (Get-Location).Path)

# Post-comparison arithmetic only: this script never reads photos, selects rules,
# refits models or changes the detector freeze.
$ErrorActionPreference = 'Stop'
$reportDirectory = Join-Path $RepositoryRoot 'tools/Games/ArkNova/PhotoRecognition/Reports/ClassicCvResearchV4'
$freeze = Get-Content (Join-Path $reportDirectory 'freeze.json') -Raw | ConvertFrom-Json
$mismatches = @($freeze.PSObject.Properties | Where-Object {
    (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $RepositoryRoot $_.Name)).Hash.ToLowerInvariant() -ne $_.Value
})
if ($mismatches.Count -ne 0) { throw 'Frozen V4 input/source changed.' }

$tables = [Collections.Generic.List[string]]::new()
$derived = [ordered]@{ frozenHashesVerified = @($freeze.PSObject.Properties).Count; partitions = [ordered]@{} }
foreach ($partition in @('calibration', 'old_holdout_reused', 'v3_reused')) {
    $report = Get-Content (Join-Path $reportDirectory "$partition.json") -Raw | ConvertFrom-Json
    $rows = @($report.images | ForEach-Object decisions)
    $accepted = @($rows | Where-Object { $null -ne $_.confidence.value -and $_.confidence.confidence -in @('High', 'Medium') })
    $acceptedCorrect = @($accepted | Where-Object { $_.confidence.value -eq $_.expected }).Count
    $gains = @($report.images | ForEach-Object {
        $image = $_
        $_.decisions | Where-Object { $null -ne $_.expected -and $_.a -ne $_.expected -and $_.new.C3.value -eq $_.expected } | ForEach-Object {
            [ordered]@{ image=$image.name; color=$_.color; track=$_.track; expected=$_.expected; a=$_.a; c3=$_.new.C3.value; aPipelineError=($null -ne $image.aError) }
        }
    })
    $ecc = @($report.images | ForEach-Object ecc)
    $withoutError = @($report.images | Where-Object { $null -eq $_.aError } | ForEach-Object decisions | Where-Object { $null -ne $_.expected })
    $c1Scores = @($report.images | ForEach-Object { $_.fields.C1 } | ForEach-Object score)
    $derived.partitions[$partition] = [ordered]@{
        aAccepted=$accepted.Count; aAcceptedCorrect=$acceptedCorrect
        aAcceptedAccuracy= $(if($accepted.Count){$acceptedCorrect/$accepted.Count}else{$null})
        k4OutputCount=($report.ensemble.total-$report.ensemble.missing+$report.ensemble.phantom)
        k4OutputAccuracy=($report.ensemble.correct/($report.ensemble.total-$report.ensemble.missing+$report.ensemble.phantom))
        c3Complementary=$gains
        nonErrorTotal=$withoutError.Count
        nonErrorC3Complementary=@($withoutError | Where-Object {$_.a -ne $_.expected -and $_.new.C3.value -eq $_.expected}).Count
        nonErrorOracle=@($withoutError | Where-Object {$_.a -eq $_.expected -or $_.new.C3.value -eq $_.expected}).Count
        eccAttempts=$ecc.Count; eccApplied=@($ecc|Where-Object applied).Count
        eccRejectedOutOfBounds=@($ecc|Where-Object failure -eq 'translation_out_of_bounds').Count
        eccMeanCorrelationGain=($ecc|ForEach-Object {$_.after-$_.before}|Measure-Object -Average).Average
        c1MaximumObservedScore=($c1Scores|Measure-Object -Maximum).Maximum
    }
    $tables.Add("## $partition")
    $tables.Add('')
    $tables.Add('| Method | TP | FP | FN | Precision | Recall | F1 | colored Top1 / Top2 / Top3 | null | A wrong / readout right | Oracle | ms |')
    $tables.Add('|---|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|')
    foreach($method in @('C1','C2a','C2b','C3')) {
        $summary=$report.methods.$method.summary
        $tables.Add(('| {0} | {1} | {2} | {3} | {4:P1} | {5:P1} | {6:F3} | {7} / {8} / {9} | {10} | {11} | {12} | {13:F1} |' -f
            $method,$summary.tp,$summary.fp,$summary.fn,$summary.precision,$summary.recall,$summary.f1,$summary.top1,$summary.top2,$summary.top3,
            $summary.abstention,$summary.aWrongReadoutRight,$summary.readoutOracle,$report.methods.$method.averageMilliseconds))
    }
    $tables.Add('')
    $tables.Add('| Track C3 | TP | FP | FN | Precision | Recall | colored Top1 | null | A wrong / readout right |')
    $tables.Add('|---|---:|---:|---:|---:|---:|---:|---:|---:|')
    foreach($track in @('attractiveness','conservation','reputation')) {
        $summary=$report.methods.C3.byTrack.$track
        $tables.Add(('| {0} | {1} | {2} | {3} | {4:P1} | {5:P1} | {6} | {7} | {8} |' -f
            $track,$summary.tp,$summary.fp,$summary.fn,$summary.precision,$summary.recall,$summary.top1,$summary.abstention,$summary.aWrongReadoutRight))
    }
    $tables.Add('')
    $tables.Add('| View C3 | TP | FP | FN | Precision | Recall | colored Top1 / total | null |')
    $tables.Add('|---|---:|---:|---:|---:|---:|---|---:|')
    foreach($view in $report.methods.C3.byView.PSObject.Properties) {
        $summary=$view.Value
        $tables.Add(('| {0} | {1} | {2} | {3} | {4:P1} | {5:P1} | {6}/{7} | {8} |' -f
            $view.Name,$summary.tp,$summary.fp,$summary.fn,$summary.precision,$summary.recall,$summary.top1,$summary.total,$summary.abstention))
    }
    $tables.Add('')
}
$derived | ConvertTo-Json -Depth 12 | Set-Content -Encoding utf8 (Join-Path $reportDirectory 'derived-diagnostics.json')
[IO.File]::WriteAllText((Join-Path $reportDirectory 'METRIC_TABLES.md'), (($tables -join "`n").TrimEnd() + "`n"), [Text.UTF8Encoding]::new($false))
Write-Output "Verified $(@($freeze.PSObject.Properties).Count) hashes; summary tables regenerated from stored reports only."
