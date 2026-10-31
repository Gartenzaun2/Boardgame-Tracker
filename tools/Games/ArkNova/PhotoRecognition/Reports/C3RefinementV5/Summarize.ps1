param([string]$RepositoryRoot = (Get-Location).Path)

# Post-comparison report arithmetic only; no inference, fitting or rule selection.
$ErrorActionPreference = 'Stop'
$reportDirectory = Join-Path $RepositoryRoot 'tools/Games/ArkNova/PhotoRecognition/Reports/C3RefinementV5'
$freeze = Get-Content (Join-Path $reportDirectory 'freeze.json') -Raw | ConvertFrom-Json
$mismatches = @($freeze.PSObject.Properties | Where-Object {
    (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $RepositoryRoot $_.Name)).Hash.ToLowerInvariant() -ne $_.Value
})
if ($mismatches.Count) { throw 'Frozen focused experiment changed.' }
$lines = [Collections.Generic.List[string]]::new()
$derived = [ordered]@{ hashesVerified=@($freeze.PSObject.Properties).Count; partitions=[ordered]@{} }
$methods = @('A_legacy','A_hardened','A_accepted','P0','P1','P2','F1','F2')
foreach ($partition in @('calibration','old_holdout_reused','v3_reused')) {
    $report = Get-Content (Join-Path $reportDirectory "$partition.json") -Raw | ConvertFrom-Json
    $lines.Add("## $partition")
    $lines.Add('')
    $lines.Add('| Method | TP / FP / FN | Precision / Recall / F1 | colored Top1 / Top2 / Top3 | null | phantom | output accuracy | coverage | A wrong / right | A right / wrong | Oracle |')
    $lines.Add('|---|---|---|---|---:|---:|---:|---:|---:|---:|---:|')
    foreach($method in $methods) {
        $s=$report.methods.$method.summary; $c=$s.colored
        $lines.Add(('| {0} | {1}/{2}/{3} | {4:P1}/{5:P1}/{6:F3} | {7}/{8}/{9} | {10} | {11} | {12:P1} | {13:P1} | {14} | {15} | {16} |' -f
            $method,$s.tp,$s.fp,$s.fn,$s.precision,$s.recall,$s.f1,$c.top1,$c.top2,$c.top3,$c.null,$c.phantoms,$c.outputAccuracy,$c.coverage,$c.aWrongRight,$c.aRightWrong,$c.oracle))
    }
    $lines.Add('')
    $lines.Add('| Method / track | TP / FP / FN | colored Top1 / total | null | output accuracy |')
    $lines.Add('|---|---|---|---:|---:|')
    foreach($method in $methods) {
        foreach($track in @('attractiveness','conservation','reputation')) {
            $s=$report.methods.$method.byTrack.$track; $c=$s.colored
            $lines.Add(('| {0}/{1} | {2}/{3}/{4} | {5}/{6} | {7} | {8:P1} |' -f $method,$track,$s.tp,$s.fp,$s.fn,$c.top1,$c.total,$c.null,$c.outputAccuracy))
        }
    }
    $lines.Add('')
    $lines.Add('| Method / color | Top1 / Top2 / Top3 / total | null | A wrong / right | Oracle |')
    $lines.Add('|---|---|---:|---:|---:|')
    foreach($method in $methods) {
        foreach($color in @('red','blue','yellow','black')) {
            $c=$report.methods.$method.byColor.$color
            $lines.Add(('| {0}/{1} | {2}/{3}/{4}/{5} | {6} | {7} | {8} |' -f $method,$color,$c.top1,$c.top2,$c.top3,$c.total,$c.null,$c.aWrongRight,$c.oracle))
        }
    }
    $lines.Add('')
    $lines.Add('| Method / view | TP / FP / FN | colored Top1 / total | null |')
    $lines.Add('|---|---|---|---:|')
    foreach($method in $methods) {
        foreach($view in $report.methods.$method.byView.PSObject.Properties) {
            $s=$view.Value; $c=$s.colored
            $lines.Add(('| {0}/{1} | {2}/{3}/{4} | {5}/{6} | {7} |' -f $method,$view.Name,$s.tp,$s.fp,$s.fn,$c.top1,$c.total,$c.null))
        }
    }
    $lines.Add('')
    $selected=$report.methods.($report.selection.selectedFusion).summary.colored
    $gains=@($report.images | ForEach-Object {
        $image=$_
        $_.rows | Where-Object {$null-ne$_.expected -and $_.a.value-ne$_.expected -and $_.predictions.P2.value-eq$_.expected} | ForEach-Object {
            [ordered]@{image=$image.name;color=$_.color;track=$_.track;expected=$_.expected;a=$_.a.value;local=$_.predictions.P2.value}
        }
    })
    $errors=@($report.images | ForEach-Object {
        $image=$_
        $_.rows | Where-Object {$null-ne$_.expected -and $null-ne$_.predictions.F2.value -and $_.predictions.F2.value-ne$_.expected} | ForEach-Object {
            [ordered]@{image=$image.name;color=$_.color;track=$_.track;expected=$_.expected;fusion=$_.predictions.F2.value;aConfidence=$_.confidence.confidence}
        }
    })
    $derived.partitions[$partition]=[ordered]@{
        selectedFusion=$report.selection.selectedFusion; qualityTargetMet=($selected.outputAccuracy-ge.98 -and $selected.coverage-ge.75)
        localComplementary=$gains; selectedFusionErrors=$errors; meanCombinedMilliseconds=$report.meanImageMilliseconds
    }
}
[IO.File]::WriteAllText((Join-Path $reportDirectory 'METRICS.md'), (($lines-join "`n").TrimEnd()+"`n"),[Text.UTF8Encoding]::new($false))
$derived|ConvertTo-Json -Depth 9|Set-Content -Encoding utf8 (Join-Path $reportDirectory 'derived-diagnostics.json')
Write-Output "Verified $(@($freeze.PSObject.Properties).Count) source/input hashes; tables regenerated without evaluating photos."
