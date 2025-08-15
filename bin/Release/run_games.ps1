# List of player names
# $players = @("TBETS", "EMCTS", "EMCTS-FH");
# $players = @("TBETS", "RHEA", "M-UCT")
$players = @("TBETS", "RHEA", "M-UCT", "M-UCT-PW-RR", "EMCTS-FH", "Sample-MaxActionEvalFunc") # all 6
# $players = @("TBETS", "RHEA", "M-UCT", "M-UCT-PW-RR") # best 4
# $players = @("M-UCT", "M-UCT-RR")
# $players = @("M-UCT", "M-UCT-HR")
# $players = @("M-UCT", "M-UCT-TT")
# $players = @("M-UCT", "M-UCT-PW")
# $players = @("M-UCT", "M-UCT-PW-RR")
# $players = @("EMCTS-FH", "M-UCT-PW-RR")
# $players = @("M-UCT-PW-RR", "EMCTS", "EMCTS-FH")
# $players = @("M-UCT-PW-RR", "RHEA")
# $players = @("TBETS", "RHEA")

$n = 3      # Number of repetitions

# Path to executable and output folder
$exe = ".\SimpleWars.exe"
$dirname = "4u-100ms"
# $dirname = "6u-100ms"
# $dirname = "10u-100ms"
# $dirname = "4u-100ms-EMCTS"
# $dirname = "4u-100ms-RHEA"
# $dirname = "4u-100ms-MCTS"

$outputDir = "..\gameresults\matchup_stats\$dirname\"

# Make sure output directory exists
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# Loop through each unique pair of players
# Loop through each unique pair of players
for ($i = 0; $i -lt $players.Count; $i++) {
    for ($j = 0; $j -lt $players.Count; $j++) {
        if ($i -ne $j) {
            $player1 = $players[$i]
            $player2 = $players[$j]

            for ($k = 1; $k -le $n; $k++) {
                $output1 = "$outputDir\$player1`_$player2`_$k.csv"
                $output2 = "$outputDir\$player2`_$player1`_$k.csv"

                if (-not (Test-Path $output1)) {
                    Write-Host "Running $player1 vs $player2 - game $k"
                    & $exe "user1=$player1" "user2=$player2" "output=$output1" "games=2"
                } else {
                    Write-Host "Skipping $output1 (already exists)"
                }

                if (-not (Test-Path $output2)) {
                    Write-Host "Running $player2 vs $player1 - game $k"
                    & $exe "user1=$player2" "user2=$player1" "output=$output2" "games=2"
                } else {
                    Write-Host "Skipping $output2 (already exists)"
                }
            }
        }
    }
}