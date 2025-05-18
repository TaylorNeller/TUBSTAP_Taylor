using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SimpleWars
{
    /// <summary>
    /// RHCP (Rolling Horizon Co-evolutionary Planning) agent that evolves both player and enemy actions.
    /// Based on the RHEA algorithm but co-evolves action sequences for both sides.
    /// </summary>
    class AI_RHCP : Player
    {
        private const int HORIZON = 2;               
        private const int POPULATION_SIZE = 30;       
        private const int NUM_GENERATIONS = 20;        
        private const double MUTATION_RATE = 0.4;     
        private const double CROSSOVER_RATE = 0.8;    
        private const int TOURNAMENT_SIZE = 3;         
        private const long LIMIT_TIME = 9700;          
        private const int ELITISM_COUNT = 3;          
        private const double ATTACK_BIAS = 0.8;     

        // A random number generator (reused throughout the agent)
        private Random rnd = new Random();
        
        // Stopwatch for time management
        private Stopwatch stopwatch = new Stopwatch();
        private static long timeLeft;
        
        // Previous best action sequence (for shift buffer)
        private List<Action>[] previousBestPlayerSequence = null;
        private List<Action>[] previousBestEnemySequence = null;
        
        // Class to represent an individual in the population
        private class Individual
        {
            // Array of action lists for both player and enemy
            public List<Action>[] playerActionSequence; 
            public List<Action>[] enemyActionSequence;
            public double fitness;
            
            public Individual(int horizon)
            {
                playerActionSequence = new List<Action>[horizon];
                enemyActionSequence = new List<Action>[horizon];
                
                for (int i = 0; i < horizon; i++)
                {
                    playerActionSequence[i] = new List<Action>();
                    enemyActionSequence[i] = new List<Action>();
                }
                fitness = 0.0;
            }
            
            public Individual Clone()
            {
                Individual clone = new Individual(playerActionSequence.Length);
                clone.fitness = this.fitness;
                
                for (int i = 0; i < playerActionSequence.Length; i++)
                {
                    clone.playerActionSequence[i] = new List<Action>();
                    foreach (Action action in playerActionSequence[i])
                    {
                        clone.playerActionSequence[i].Add(action.createDeepClone());
                    }
                    
                    clone.enemyActionSequence[i] = new List<Action>();
                    foreach (Action action in enemyActionSequence[i])
                    {
                        clone.enemyActionSequence[i].Add(action.createDeepClone());
                    }
                }
                
                return clone;
            }
        }

        public string getName()
        {
            return "RHCP";
        }

        public string showParameters()
        {
            return "HORIZON = " + HORIZON + 
                   ", POPULATION_SIZE = " + POPULATION_SIZE + 
                   ", NUM_GENERATIONS = " + NUM_GENERATIONS + 
                   ", MUTATION_RATE = " + MUTATION_RATE + 
                   ", CROSSOVER_RATE = " + CROSSOVER_RATE;
        }

        public Action makeAction(Map map, int teamColor, bool turnStart, bool gameStart)
        {
            stopwatch.Start();
            
            if (turnStart)
            {
                timeLeft = LIMIT_TIME;
                // Reset previous best sequences at the start of a new turn
                previousBestPlayerSequence = null;
                previousBestEnemySequence = null;
            }
            
            int movableUnitsCount = map.getUnitsList(teamColor, false, true, false).Count;
            
            // Initialize population
            Individual[] population = InitializePopulation(map, teamColor);
            
            // Evaluate initial population
            EvaluatePopulation(population, map, teamColor);
            
            // Evolve population for a number of generations or until time runs out
            for (int generation = 0; generation < NUM_GENERATIONS; generation++)
            {
                if (stopwatch.ElapsedMilliseconds > (timeLeft / movableUnitsCount))
                {
                    Logger.addLogMessage("RHCP: Time limit reached after " + generation + " generations\r\n", teamColor);
                    break;
                }
                
                // Create new population through selection, crossover, and mutation
                population = EvolvePopulation(population, map, teamColor);
                
                // Evaluate new population
                EvaluatePopulation(population, map, teamColor);
            }
            
            // Find the best individual
            Individual bestIndividual = GetBestIndividual(population);
            
            // Store the best sequences for the next call (shift buffer)
            previousBestPlayerSequence = bestIndividual.playerActionSequence;
            previousBestEnemySequence = bestIndividual.enemyActionSequence;
            
            // Get the best action from the best individual
            Action bestAction = GetBestAction(bestIndividual, map, teamColor);
            
            stopwatch.Stop();
            Logger.addLogMessage("RHCP: Time used: " + stopwatch.ElapsedMilliseconds + "ms\r\n", teamColor);
            timeLeft -= stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();
            
            return bestAction;
        }
        
        // Initialize the population with random individuals or using the shift buffer
        private Individual[] InitializePopulation(Map map, int teamColor)
        {
            Individual[] population = new Individual[POPULATION_SIZE];
            int enemyColor = (teamColor == 0) ? 1 : 0;
            
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                population[i] = new Individual(HORIZON);
                
                // If we have previous best sequences, use shift buffer for the first individual
                if (i == 0 && previousBestPlayerSequence != null && previousBestEnemySequence != null)
                {
                    // Shift the previous best sequences one step forward
                    for (int h = 0; h < HORIZON - 1; h++)
                    {
                        foreach (Action action in previousBestPlayerSequence[h + 1])
                        {
                            population[i].playerActionSequence[h].Add(action.createDeepClone());
                        }
                        
                        foreach (Action action in previousBestEnemySequence[h + 1])
                        {
                            population[i].enemyActionSequence[h].Add(action.createDeepClone());
                        }
                    }
                    
                    // Generate random actions for the last step
                    Map simMap = map.createDeepClone();
                    
                    // Apply actions from previous steps
                    for (int h = 0; h < HORIZON - 1; h++)
                    {
                        SimulateActions(simMap, population[i].playerActionSequence[h], teamColor);
                        SimulateActions(simMap, population[i].enemyActionSequence[h], enemyColor);
                        
                        // Enable units for next step
                        simMap.enableUnitsAction(teamColor);
                        simMap.enableUnitsAction(enemyColor);
                    }
                    
                    population[i].playerActionSequence[HORIZON - 1] = GenerateRandomActions(simMap, teamColor);
                    
                    // After player's actions, we need to update the map state again
                    SimulateActions(simMap, population[i].playerActionSequence[HORIZON - 1], teamColor);
                    
                    population[i].enemyActionSequence[HORIZON - 1] = GenerateRandomActions(simMap, enemyColor);
                }
                else
                {
                    // Generate random action sequences for each step in the horizon
                    Map simMap = map.createDeepClone();
                    
                    for (int h = 0; h < HORIZON; h++)
                    {
                        // Player's turn
                        population[i].playerActionSequence[h] = GenerateRandomActions(simMap, teamColor);
                        SimulateActions(simMap, population[i].playerActionSequence[h], teamColor);
                        
                        // Enemy's turn
                        population[i].enemyActionSequence[h] = GenerateRandomActions(simMap, enemyColor);
                        SimulateActions(simMap, population[i].enemyActionSequence[h], enemyColor);
                        
                        // Enable units for next step if not the last step
                        if (h < HORIZON - 1)
                        {
                            simMap.enableUnitsAction(teamColor);
                            simMap.enableUnitsAction(enemyColor);
                        }
                    }
                }
            }
            
            return population;
        }
        
        // Simulate a list of actions on the map for a specific team
        private void SimulateActions(Map simMap, List<Action> actions, int teamColor)
        {
            // Create a copy of the actions to avoid modifying the original during iteration
            List<Action> actionsToExecute = new List<Action>();
            foreach (Action action in actions)
            {
                actionsToExecute.Add(action.createDeepClone());
            }
            
            foreach (Action action in actionsToExecute)
            {
                // Make sure the unit exists and can act
                Unit unit = simMap.getUnit(action.operationUnitId);
                if (unit == null || unit.isActionFinished() || unit.getTeamColor() != teamColor)
                {
                    continue;
                }
                
                if (action.actionType == Action.ACTIONTYPE_MOVEANDATTACK)
                {
                    // Verify the target unit still exists
                    Unit targetUnit = simMap.getUnit(action.targetUnitId);
                    if (targetUnit == null)
                    {
                        continue;
                    }
                    
                    // Verify the destination is not occupied by another unit
                    Unit existingUnit = simMap.getUnit(action.destinationXpos, action.destinationYpos);
                    if (existingUnit != null && existingUnit.getID() != action.operationUnitId)
                    {
                        continue;
                    }
                    
                    // Verify that the attack is valid using the game's action checker
                    List<Action> validAttackActions = RangeController.getAttackActionList(unit, simMap);
                    bool isValidAttack = false;
                    
                    foreach (Action validAction in validAttackActions)
                    {
                        if (validAction.actionType == action.actionType &&
                            validAction.targetUnitId == action.targetUnitId &&
                            validAction.destinationXpos == action.destinationXpos &&
                            validAction.destinationYpos == action.destinationYpos)
                        {
                            isValidAttack = true;
                            break;
                        }
                    }
                    
                    if (!isValidAttack)
                    {
                        continue;
                    }
                }
                else if (action.actionType == Action.ACTIONTYPE_MOVEONLY)
                {
                    // Verify the destination is not occupied by another unit
                    Unit existingUnit = simMap.getUnit(action.destinationXpos, action.destinationYpos);
                    if (existingUnit != null && existingUnit.getID() != action.operationUnitId)
                    {
                        continue;
                    }
                    
                    // Verify that the move is valid
                    bool[,] reachable = RangeController.getReachableCellsMatrix(unit, simMap);
                    if (!reachable[action.destinationXpos, action.destinationYpos])
                    {
                        continue;
                    }
                }
                
                simMap.executeAction(action);
            }
        }
        
        // Generate random valid actions for one step
        private List<Action> GenerateRandomActions(Map map, int teamColor)
        {
            List<Action> actions = new List<Action>();
            Map simMap = map.createDeepClone();
            
            // Get all movable units
            List<Unit> movableUnits = new List<Unit>(simMap.getUnitsList(teamColor, false, true, false));
            
            // For each movable unit, generate a random action
            foreach (Unit unit in movableUnits)
            {
                // Skip if the unit no longer exists or has already acted
                if (unit == null || unit.isActionFinished())
                {
                    continue;
                }
                
                // Decide whether to prioritize attack or movement
                bool prioritizeAttack = rnd.NextDouble() < ATTACK_BIAS;
                
                if (prioritizeAttack)
                {
                    // Try to get attack actions first
                    List<Action> attackActions = RangeController.getAttackActionList(unit, simMap);
                    List<Action> validAttackActions = new List<Action>();
                    
                    foreach (Action attack in attackActions)
                    {
                        // Verify the target unit still exists
                        Unit targetUnit = simMap.getUnit(attack.targetUnitId);
                        if (targetUnit == null)
                        {
                            continue;
                        }
                        
                        // For attack actions that involve movement, verify the destination is not occupied
                        if (attack.actionType == Action.ACTIONTYPE_MOVEANDATTACK)
                        {
                            Unit existingUnit = simMap.getUnit(attack.destinationXpos, attack.destinationYpos);
                            if (existingUnit != null && existingUnit.getID() != attack.operationUnitId)
                            {
                                continue;
                            }
                        }
                        
                        validAttackActions.Add(attack);
                    }
                    
                    if (validAttackActions.Count > 0)
                    {
                        // Choose a random attack action
                        Action action = validAttackActions[rnd.Next(validAttackActions.Count)];
                        actions.Add(action);
                        simMap.executeAction(action);
                        continue;
                    }
                }
                
                // If no attack action or not prioritizing attack, try movement
                List<Action> moveActions = new List<Action>();
                bool[,] reachable = RangeController.getReachableCellsMatrix(unit, simMap);
                
                for (int x = 1; x < simMap.getXsize() - 1; x++)
                {
                    for (int y = 1; y < simMap.getYsize() - 1; y++)
                    {
                        // Make sure the position is reachable and not occupied by another unit
                        Unit existingUnit = simMap.getUnit(x, y);
                        if (reachable[x, y] && (existingUnit == null || existingUnit.getID() == unit.getID()))
                        {
                            moveActions.Add(Action.createMoveOnlyAction(unit, x, y));
                        }
                    }
                }
                
                if (moveActions.Count > 0)
                {
                    // Choose a random move action
                    Action action = moveActions[rnd.Next(moveActions.Count)];
                    
                    // Double-check that the destination is still not occupied
                    Unit existingUnit = simMap.getUnit(action.destinationXpos, action.destinationYpos);
                    if (existingUnit == null || existingUnit.getID() == action.operationUnitId)
                    {
                        actions.Add(action);
                        simMap.executeAction(action);
                    }
                }
            }
            
            return actions;
        }
        
        // Evaluate the fitness of each individual in the population
        private void EvaluatePopulation(Individual[] population, Map map, int teamColor)
        {
            for (int i = 0; i < population.Length; i++)
            {
                population[i].fitness = EvaluateIndividual(population[i], map, teamColor);
            }
        }
        
        // Evaluate the fitness of an individual by simulating its action sequences
        private double EvaluateIndividual(Individual individual, Map map, int teamColor)
        {
            Map simMap = map.createDeepClone();
            int enemyColor = (teamColor == 0) ? 1 : 0;
            
            // Simulate the action sequence
            for (int h = 0; h < HORIZON; h++)
            {
                // Player's turn: execute actions
                SimulateActions(simMap, individual.playerActionSequence[h], teamColor);
                
                // Enemy's turn: execute actions
                SimulateActions(simMap, individual.enemyActionSequence[h], enemyColor);
                
                // Enable units for the next step
                simMap.enableUnitsAction(teamColor);
                simMap.enableUnitsAction(enemyColor);
                
                // Increment turn count
                simMap.incTurnCount();
                
                // Check if the game is over
                if (simMap.getUnitsList(teamColor, true, true, false).Count == 0 ||
                    simMap.getUnitsList(enemyColor, true, true, false).Count == 0 ||
                    simMap.getTurnCount() >= simMap.getTurnLimit())
                {
                    break;
                }
            }
            
            // Evaluate the final state
            return EvaluateState(simMap, teamColor);
        }
        
        // Evaluate the state of the game
        private double EvaluateState(Map simMap, int teamColor)
        {
            int enemyColor = (teamColor == 0) ? 1 : 0;
            
            List<Unit> myUnits = simMap.getUnitsList(teamColor, true, true, false);
            List<Unit> enemyUnits = simMap.getUnitsList(enemyColor, true, true, false);
            
            // If we have no units left, return the worst possible score
            if (myUnits.Count == 0)
            {
                return 0.0;
            }
            
            // If the enemy has no units left, return the best possible score
            if (enemyUnits.Count == 0)
            {
                return 1.0;
            }
            
            // Dictionary for unit type values
            Dictionary<string, double> unitValues = new Dictionary<string, double>();
            unitValues.Add("infantry", 1.0);
            unitValues.Add("panzer", 4.0);
            unitValues.Add("cannon", 4.0);
            unitValues.Add("antiair", 2.5);
            unitValues.Add("fighter", 4);
            unitValues.Add("attacker", 6);
            // Calculate total value for both sides
            double myValue = 0.0;
            double enemyValue = 0.0;
            
            foreach (Unit unit in myUnits)
            {
                myValue += unitValues[unit.getName()] * unit.getHP();
            }
            
            foreach (Unit unit in enemyUnits)
            {
                enemyValue += unitValues[unit.getName()] * unit.getHP();
            }
            
            // If one side has a significant advantage, return win/loss
            if (myValue - enemyValue >= simMap.getDrawHPThreshold())
            {
                return 1.0;
            }
            else if (enemyValue - myValue >= simMap.getDrawHPThreshold())
            {
                return 0.0;
            }
            
            // Otherwise, return a score based on the value ratio
            double valueRatio = myValue / (myValue + enemyValue);
            
            // Add a small bonus for having more units
            double unitRatio = (double)myUnits.Count / (myUnits.Count + enemyUnits.Count);
            
            // Combine the two ratios with a weight
            return 0.7 * valueRatio + 0.3 * unitRatio;
        }
        
        // Evolve the population through selection, crossover, and mutation
        private Individual[] EvolvePopulation(Individual[] currentPopulation, Map map, int teamColor)
        {
            Individual[] newPopulation = new Individual[POPULATION_SIZE];
            int enemyColor = (teamColor == 0) ? 1 : 0;
            
            // Elitism: Keep the best individuals unchanged
            Array.Sort(currentPopulation, (a, b) => b.fitness.CompareTo(a.fitness));
            
            for (int i = 0; i < ELITISM_COUNT; i++)
            {
                newPopulation[i] = currentPopulation[i].Clone();
            }
            
            // Fill the rest of the population with offspring
            for (int i = ELITISM_COUNT; i < POPULATION_SIZE; i++)
            {
                // Select parents using tournament selection
                Individual parent1 = TournamentSelection(currentPopulation);
                Individual parent2 = TournamentSelection(currentPopulation);
                
                // Create offspring through crossover
                Individual offspring;
                
                if (rnd.NextDouble() < CROSSOVER_RATE)
                {
                    offspring = Crossover(parent1, parent2);
                }
                else
                {
                    offspring = parent1.Clone();
                }
                
                // Apply mutation
                Mutate(offspring, map, teamColor, enemyColor);
                
                // Add to new population
                newPopulation[i] = offspring;
            }
            
            return newPopulation;
        }
        
        // Tournament selection: Select the best individual from a random subset
        private Individual TournamentSelection(Individual[] population)
        {
            Individual best = null;
            
            for (int i = 0; i < TOURNAMENT_SIZE; i++)
            {
                int randomIndex = rnd.Next(population.Length);
                Individual individual = population[randomIndex];
                
                if (best == null || individual.fitness > best.fitness)
                {
                    best = individual;
                }
            }
            
            return best;
        }
        
        // Crossover: Combine two parents to create an offspring
        private Individual Crossover(Individual parent1, Individual parent2)
        {
            Individual offspring = new Individual(HORIZON);
            
            // Uniform crossover: For each step, randomly choose actions from either parent
            for (int h = 0; h < HORIZON; h++)
            {
                // Player actions
                if (rnd.NextDouble() < 0.5)
                {
                    foreach (Action action in parent1.playerActionSequence[h])
                    {
                        offspring.playerActionSequence[h].Add(action.createDeepClone());
                    }
                }
                else
                {
                    foreach (Action action in parent2.playerActionSequence[h])
                    {
                        offspring.playerActionSequence[h].Add(action.createDeepClone());
                    }
                }
                
                // Enemy actions
                if (rnd.NextDouble() < 0.5)
                {
                    foreach (Action action in parent1.enemyActionSequence[h])
                    {
                        offspring.enemyActionSequence[h].Add(action.createDeepClone());
                    }
                }
                else
                {
                    foreach (Action action in parent2.enemyActionSequence[h])
                    {
                        offspring.enemyActionSequence[h].Add(action.createDeepClone());
                    }
                }
            }
            
            return offspring;
        }
        
        // Mutation: Randomly modify an individual's action sequences
        private void Mutate(Individual individual, Map map, int teamColor, int enemyColor)
        {
            // Create a fresh map for each mutation to ensure proper state handling
            Map simMap = map.createDeepClone();
            
            for (int h = 0; h < HORIZON; h++)
            {
                // For each step, we need a consistent state
                if (h > 0)
                {
                    // If not the first step, apply all previous actions to get the correct state
                    simMap = map.createDeepClone();
                    for (int prev = 0; prev < h; prev++)
                    {
                        SimulateActions(simMap, individual.playerActionSequence[prev], teamColor);
                        SimulateActions(simMap, individual.enemyActionSequence[prev], enemyColor);
                        
                        // Enable units for the next step
                        simMap.enableUnitsAction(teamColor);
                        simMap.enableUnitsAction(enemyColor);
                    }
                }
                
                // Decide whether to mutate player actions for this step
                if (rnd.NextDouble() < MUTATION_RATE)
                {
                    individual.playerActionSequence[h] = GenerateRandomActions(simMap, teamColor);
                }
                
                // Apply player actions to update the simulation state for enemy actions
                Map enemyStateMap = simMap.createDeepClone();
                SimulateActions(enemyStateMap, individual.playerActionSequence[h], teamColor);
                
                // Decide whether to mutate enemy actions for this step
                if (rnd.NextDouble() < MUTATION_RATE)
                {
                    individual.enemyActionSequence[h] = GenerateRandomActions(enemyStateMap, enemyColor);
                }
            }
        }
        
        // Get the best individual from the population
        private Individual GetBestIndividual(Individual[] population)
        {
            Individual best = population[0];
            
            for (int i = 1; i < population.Length; i++)
            {
                if (population[i].fitness > best.fitness)
                {
                    best = population[i];
                }
            }
            
            return best;
        }
        
        // Get the best action from the best individual
        private Action GetBestAction(Individual bestIndividual, Map map, int teamColor)
        {
            // If the best individual has no player actions, return a random action
            if (bestIndividual.playerActionSequence[0].Count == 0)
            {
                List<Unit> movableUnits = map.getUnitsList(teamColor, false, true, false);
                
                if (movableUnits.Count > 0)
                {
                    Unit randomUnit = movableUnits[rnd.Next(movableUnits.Count)];
                    List<Action> actions = M_Tools.getUnitActions(randomUnit, map);
                    
                    if (actions.Count > 0)
                    {
                        return actions[rnd.Next(actions.Count)];
                    }
                }
                
                // If no actions are available, return a turn end action
                return Action.createTurnEndAction();
            }
            
            // Create a list of valid actions from the best individual's first step
            List<Action> validActions = new List<Action>();
            
            foreach (Action action in bestIndividual.playerActionSequence[0])
            {
                // Skip if the unit no longer exists or has already acted
                Unit unit = map.getUnit(action.operationUnitId);
                if (unit == null || unit.isActionFinished() || unit.getTeamColor() != teamColor)
                {
                    continue;
                }
                
                if (action.actionType == Action.ACTIONTYPE_MOVEANDATTACK)
                {
                    // For attack actions, verify the target unit still exists
                    Unit targetUnit = map.getUnit(action.targetUnitId);
                    if (targetUnit == null)
                    {
                        continue;
                    }
                    
                    // Also verify the destination is not occupied by another unit
                    Unit existingUnit = map.getUnit(action.destinationXpos, action.destinationYpos);
                    if (existingUnit != null && existingUnit.getID() != action.operationUnitId)
                    {
                        continue;
                    }
                    
                    // Verify that the attack is valid using the game's action checker
                    bool isValidAttack = false;
                    List<Action> validAttackActions = RangeController.getAttackActionList(unit, map);
                    
                    foreach (Action validAction in validAttackActions)
                    {
                        if (validAction.actionType == action.actionType &&
                            validAction.targetUnitId == action.targetUnitId &&
                            validAction.destinationXpos == action.destinationXpos &&
                            validAction.destinationYpos == action.destinationYpos)
                        {
                            isValidAttack = true;
                            break;
                        }
                    }
                    
                    if (!isValidAttack)
                    {
                        continue;
                    }
                    
                    validActions.Add(action);
                }
                else if (action.actionType == Action.ACTIONTYPE_MOVEONLY)
                {
                    // For move actions, verify the destination is not occupied by another unit
                    Unit existingUnit = map.getUnit(action.destinationXpos, action.destinationYpos);
                    if (existingUnit != null && existingUnit.getID() != action.operationUnitId)
                    {
                        continue;
                    }
                    
                    // Verify that the move is valid
                    bool[,] reachable = RangeController.getReachableCellsMatrix(unit, map);
                    if (!reachable[action.destinationXpos, action.destinationYpos])
                    {
                        continue;
                    }
                    
                    validActions.Add(action);
                }
                else
                {
                    // Other action types (like turn end) are always valid
                    validActions.Add(action);
                }
            }
            
            // If we have valid actions, return the first one (highest priority)
            if (validActions.Count > 0)
            {
                return validActions[0];
            }
            
            // If no valid actions are found, return a random action from a random unit
            List<Unit> movableUnits2 = map.getUnitsList(teamColor, false, true, false);
            
            if (movableUnits2.Count > 0)
            {
                Unit randomUnit = movableUnits2[rnd.Next(movableUnits2.Count)];
                List<Action> actions = M_Tools.getUnitActions(randomUnit, map);
                
                if (actions.Count > 0)
                {
                    return actions[rnd.Next(actions.Count)];
                }
            }
            
            // If no actions are available, return a turn end action
            return Action.createTurnEndAction();
        }
    }
}
