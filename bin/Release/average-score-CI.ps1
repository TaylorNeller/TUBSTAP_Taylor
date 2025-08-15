<# Average-WLD-WithCI.ps1 -------------------------------------------
Combines Average-WLD.ps1 with Wilson confidence interval calculations.

Usage:
  .\Average-WLD-WithCI.ps1                          # default folder, 95 % CI
  .\Average-WLD-WithCI.ps1 -Folder path -Z 2.576    # example 99 % CI
#>

param(
    # [string]$Folder = "../gameresults/matchup_stats/10u-100ms",
    [string]$Folder = "../gameresults/matchup_stats/4u-100ms",
    # [string]$Folder = "../gameresults/matchup_stats/4u-100ms-RHEA/3u/2",
    [double]$Z      = 1.96   # Z‑score (1.96 → 95 % CI)
)

function Get-WilsonCI {
    param(
        [double]$p,  # proportion (0‑1)
        [int]   $n,  # sample size
        [double]$z   # Z‑score
    )
    $z2      = [math]::Pow($z,2)
    $den     = 1 + ($z2 / $n)
    $center  = ($p + ($z2 / (2*$n))) / $den
    $margin  = ($z * [math]::Sqrt((($p*(1-$p)) + ($z2/(4*$n)))/$n)) / $den
    return [pscustomobject]@{ Center = $center; Margin = $margin }
}

$folderPath = Join-Path $PSScriptRoot $Folder
if (-not (Test-Path $folderPath)) { Write-Error "Folder '$folderPath' not found."; exit 1 }

Write-Host "Processing $(Get-ChildItem $folderPath -Filter '*.csv').Count CSV files..."

# -- aggregate per‑file W/L/D -------------------------------------------------
$stats = @{}   # key = "A_B" → [ordered] @{W,L,D,Count}
Get-ChildItem -Path $folderPath -Filter '*.csv' | ForEach-Object {
    $parts = $_.BaseName -split '_'      # e.g. A_B_7.csv
    if ($parts.Count -lt 3) { return }   # skip bad names
    $key   = "$($parts[0])_$($parts[1])"

    $last  = Get-Content $_.FullName -Tail 1
    if ($last -match 'WLD:(\d+),(\d+),(\d+):') {
        $w,$l,$d = [int]$matches[1],[int]$matches[2],[int]$matches[3]
        if (-not $stats.ContainsKey($key)) {
            $stats[$key] = [ordered]@{W=0;L=0;D=0;Count=0}
        }
        $stats[$key].W += $w;  $stats[$key].L += $l;  $stats[$key].D += $d
        $stats[$key].Count++
    }
}

# -- aggregated W/L/D summary -------------------------------------------------
'`n=== AGGREGATED SCORES ===`n'
$stats.GetEnumerator() |
    Sort-Object Name |
    ForEach-Object {
        $k = $_.Key;  $v = $_.Value
        $avgW = [math]::Round($v.W / $v.Count, 1)
        $avgL = [math]::Round($v.L / $v.Count, 1)
        $avgD = [math]::Round($v.D / $v.Count, 1)
        '{0}_{1}: {2}, {3}, {4}' -f $k, $v.Count, $avgW, $avgL, $avgD
    }

# -- pair‑wise win‑rates with Wilson CI --------------------------------------
'`n=== PAIRWISE WINRATES (Z = {0}) ===`n' -f $Z

# 1) only keys that have a mirror
$pairs = $stats.Keys | Where-Object {
    $a,$b = $_ -split '_'
    $stats.ContainsKey("$b`_$a")
}

$results = @()
$done    = @{}

foreach ($key in $pairs) {
    $a,$b   = $key -split '_'
    $pairId = (@($a,$b) | Sort-Object) -join '|'
    if ($done[$pairId]) { continue }

    $dataAB = $stats[$key]
    $dataBA = $stats["${b}_$a"]

    $aRate = ( ($dataAB.W / $dataAB.Count) + ($dataBA.L / $dataBA.Count) ) / 2
    $bRate = ( ($dataBA.W / $dataBA.Count) + ($dataAB.L / $dataAB.Count) ) / 2

    $N     = ($dataAB.Count + $dataBA.Count) * 100  # files × 100 games

    $ciA   = Get-WilsonCI -p ($aRate/100) -n $N -z $Z
    $ciB   = Get-WilsonCI -p ($bRate/100) -n $N -z $Z

    $results += [pscustomobject]@{
        Name   = "${a}_${b}"
        Rate   = [math]::Round($aRate,2)
        Center = [math]::Round($ciA.Center*100,2)
        Margin = [math]::Round($ciA.Margin*100,2)
    }
    $results += [pscustomobject]@{
        Name   = "${b}_${a}"
        Rate   = [math]::Round($bRate,2)
        Center = [math]::Round($ciB.Center*100,2)
        Margin = [math]::Round($ciB.Margin*100,2)
    }

    $done[$pairId] = $true
}

# 3) print neatly
$maxLen = ($results | ForEach-Object { $_.Name.Length } | Measure-Object -Maximum).Maximum
$results |
    Sort-Object Name |
    ForEach-Object {
        '{0} : {1,6:N2}     {2,6:N2}     ({3,5:N2})' -f $_.Name.PadRight($maxLen), $_.Rate, $_.Center, $_.Margin
    }

'`nProcessing complete!'
