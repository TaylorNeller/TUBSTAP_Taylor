<#
.SYNOPSIS
    Computes the Wilson score confidence interval for a proportion.

.PARAMETER WinRate
    Observed proportion of successes (e.g., 0.55 for 55 %).

.PARAMETER N
    Sample size.

.PARAMETER Z
    Z‑score that corresponds to the desired confidence level
    (e.g., 1.96 → 95 %, 1.64 → 90 %, 2.576 → 99 %).

.EXAMPLE
    .\WilsonCI.ps1 -WinRate 0.55 -N 200 -Z 1.96
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [double]$WinRate,

    [Parameter(Mandatory)]
    [int]$N,

    [Parameter(Mandatory)]
    [double]$Z
)

# Pre‑compute repeated terms
$z2          = [math]::Pow($Z, 2)
$denominator = 1 + ($z2 / $N)

# Wilson center (adjusted proportion)
$center = ($WinRate + ($z2 / (2 * $N))) / $denominator

# Wilson margin of error
$margin = ($Z * [math]::Sqrt(
            (($WinRate * (1 - $WinRate)) + ($z2 / (4 * $N))) / $N
          )) / $denominator

# Confidence interval
$lower = $center - $margin
$upper = $center + $margin

# Output nicely formatted results
"Wilson score interval (two‑sided):"
"  Center (adjusted proportion): {0:P4}" -f $center
"  Margin of error:              {0:P4}" -f $margin
"  Lower bound:                  {0:P4}" -f $lower
"  Upper bound:                  {0:P4}" -f $upper
