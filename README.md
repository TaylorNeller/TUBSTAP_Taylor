# Towards Better Turn-Based Strategy Planning Agents: Turn-Based Evolutionary Tree Search

In this README, we detail how to reproduce the experiments from *"Towards Better Turn-Based Strategy Planning Agents: Turn-Based Evolutionary Tree Search."*

There are **two main sections** to this codebase:
1. The main **C# repository** originally developed by Fujiki et al. that we fork and extend.
2. Our **Python port** with neural network evaluations.

> **Note:** There are still several unpolished/unfinished parts in the code and debugging outputs. This explanation will allow you to run the required experiments to populate tables with win rates.

---

## Main Repository

This section covers **Turn-Based Evolutionary Tree Search (TBETS)** and the other agents compared in the round-robin tournament.

### Map Generation

1. To create a map set, run the standalone `MapGen.csx` file, changing desired map generation fields.
2. Adjust (if desired) the number of maps to create and the destination directory at the bottom of the script.
   - **Default:** 100 maps in the default `./bin/Release/autobattle` directory.
   - This directory must be **empty** before generating new maps.

Coincidentally, this `./bin/Release/autobattle` directory is the necessary directory for placing any maps when battling agents against each other.


### Running Battles

1. Place generated maps into `./bin/Release/autobattle`.
2. Run `run_games.ps1` in the same `./bin/Release` folder.
   - Change `$players` and `$dirname` to set tournament participants and destination folder.
3. This script produces one file per set of games played in each matchup on the autobattle maps:
   - Example: `Player1_Player2_3.csv` → 3rd set of games on the autobattle maps with Player1 as starting player and Player2 as second player.

### Calculating Win Rates

Run:
```powershell
./bin/Release/average-score-CI.ps1
```
- Change `$Folder` to the directory set as `$dirname`.
- Output includes:
  ```
  === PAIRWISE WINRATES (Z = 1.96) ===
  Player1_Player2      :      avg_winrate     center      error_margin
  ```
- These are the Wilson score intervals that can be directly inserted into a table for win rate comparison.


### Adding a New Player

- Code compiled with **Visual Studio 2019 Community Edition** (later versions may have issues).
- **GUI Version**: Run the project in Visual Studio to simulate individual games and visualize maps.
- Steps:
  1. Copy an existing player class.
  2. Change the class name and `getName()` function.
  3. Use namespace `SimpleWars` and inherit from `Player`.
  4. Add the new class name to `PlayerList.cs`.
  5. The `getName()` return value is used in `run_games.ps1`.

> **Note:** Time limits are not enforced automatically—agents must manage their own time budget.

---


## Python Code

All Python code is in the `./Taylor` folder.
- `./Taylor/port` → Mainly the Python port of C# TUBSTAP.
- `./Taylor` → Neural network code, data processing, and research scripts.


### Generating and Cleaning Data

1. Generate games with the C# code in `./bin/Release/autobattle`.
2. Run M-UCT vs M-UCT games:
   ```powershell
   .\SimpleWars.exe user1=M-UCT user2=M-UCT output=../gameresults/100kresults.csv games=2
   ```
3. By default, this outputs **WLD data** only. To include state data, modify `showAutoBattleResult()` in `Logger.cs`. Comment and uncomment lines so that it outputs in the correct ```map:unitlist:move:result``` format required by the parser in the Python code.


### Processing Data & Training Models

1. Set up the Python environment using `tubstap_python_env.yml`.
2. Run `./Taylor/job.py`, changing it to use the `process_raw_Cs_csv()` function:
   - Change to use `100kresults.csv`.
   - Adjust `mask` and `fast` parameters as needed for adjacency matrix formatting in GNN input.
3. Train the model:
   - Run `job.py`, changing it to call `train_model()`.
   - Adjust file paths, batch size, and epochs in `train_model()`.
   - Modify `./Taylor/NNModel.py` for loss/training hyperparameters if desired.

### Using a Trained Model

1. In `./Taylor/port/AI_Minimax.py`, set `self.model` to your trained model.
2. Run `job.py`, changing it to call `train_model()`.
    - Change number of games to play per side as desired

### Creating a New Model

1. Follow an existing example like `CNNModel.py`.
2. The `select_data()` function determines the inputs (5 graph + 5 image inputs).
3. Update `Trainer.py` to import and use your new model.


---

## Future Work

We believe the following would be fruitful future work directions:
- Testing agents under different time controls (`./AI/AI_Consts.cs`)
- Implementing better efficiencies for different agents/removing debugging inefficiencies, for example in TBETS hashing. 
- Updating the FH-EMCTS agent to use more efficient random move generation. Additionally, we tested 2 versions of FH-EMCTS, one that repaired the entire tail after each mutation and one that repaired only necessary actions. Somehow the one that repaired the entire tail each time seemed to do better.
- Implementing new agents with different strategies or hybrid models.
- Designing some sort of RL agent for self play and learning better neural network heuristics
- Designing some way to cluster related TUBSTAP states better for less search redundancy.