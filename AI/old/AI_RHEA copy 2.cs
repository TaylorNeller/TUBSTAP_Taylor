using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SimpleWars
{
    /// <summary>
    /// RHEA-based agent inspired by the Tribes RHEA paper.
    /// Implements a rolling horizon evolutionary algorithm with a shift buffer.
    /// </summary>
    class AI_RHEA : Player
    {
        // Parameters for RHEA
        private const int HORIZON = 5;                 // Length of the action sequence (rollout depth)
        private const int POPULATION_SIZE = 10;        // Number of individuals in the population
        private const int NUM_GENERATIONS = 8;         // Number of generations to evolve
        private const double MUTATION_RATE = 0.3;      // Chance to mutate each gene in the candidate
        private const double CROSSOVER_RATE = 0.7;     // Chance to perform crossover
        private const int TOURNAMENT_SIZE = 3;         // Number of individuals in tournament selection
        private const long LIMIT_TIME = 9700;          // Total allowed time (ms) per call, similar to M_UCT
        private const int ELITISM_COUNT = 2;           // Number of best individuals to keep unchanged
        private const double ATTACK_BIAS = 0.7;        // Bias towards attack actions (vs move actions)

        // A random number generator (reused throughout the agent)
        private Random rnd = new Random();
        
        // Stopwatch for time management
        private Stopwatch stopwatch = new Stopwatch();
        private static long timeLeft;
        
        // Previous best action sequence (for shift buffer)
        private List<Action>[] previousBestSequence = null;
        
        // Class to represent an individual in the population
        private class Individual
        {
            public List<Action>[] actionSequence; // Array of action lists, one per step in the horizon
            public double fitness;
            
            public Individual(int horizon)
            {
                actionSequence = new List<Action>[horizon];
                for (int i = 0; i < horizon; i++)
                {
                    actionSequence[i] = new List<Action>();
                }
                fitness = 0.0;
            }
            
            public Individual Clone()
            {
                Individual clone = new Individual(actionSequence.Length);
                clone.fitness = this.fitness;
                
                for (int i = 0; i < actionSequence.Length; i++)
                {
                    clone.actionSequence[i] = new List<Action>();
                    foreach (Action action in actionSequence[i])
                    {
                        clone.actionSequence[i].Add(action.createDeepClone());
                    }
                }
                
                return clone;
            }
        }

        public string getName()
        {
            return "RHEA2";
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
            // Console.WriteLine("RHEA: start");
            stopwatch.Start();
            
            if (turnStart)
            {
                timeLeft = LIMIT_TIME;
                // Reset previous best sequence at the start of a new turn
                previousBestSequence = null;
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
                    Logger.addLogMessage("RHEA: Time limit reached after " + generation + " generations\r\n", teamColor);
                    break;
                }
                
                // Create new population through selection, crossover, and mutation
                population = EvolvePopulation(population, map, teamColor);
                
                // Evaluate new population
                EvaluatePopulation(population, map, teamColor);
            }
            
            // Find the best individual
            Individual bestIndividual = GetBestIndividual(population);
            
            // Store the best sequence for the next call (shift buffer)
            previousBestSequence = bestIndividual.actionSequence;
            
            // Get the best action from the best individual
            Action bestAction = GetBestAction(bestIndividual, map, teamColor);
            
            stopwatch.Stop();
            Logger.addLogMessage("RHEA: Time used: " + stopwatch.ElapsedMilliseconds + "ms\r\n", teamColor);
            timeLeft -= stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();

            // Console.WriteLine("RHEA: Best action: " + bestAction.toOneLineString());
            
            return bestAction;
        }
        
        // Initialize the population with random individuals or using the shift buffer
        private Individual[] InitializePopulation(Map map, int teamColor)
        {
            Individual[] population = new Individual[POPULATION_SIZE];
            
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                population[i] = new Individual(HORIZON);
                
                // If we have a previous best sequence, use shift buffer for the first individual
                if (i == 0 && previousBestSequence != null)
                {
                    // Shift the previous best sequence one step forward
                    for (int h = 0; h < HORIZON - 1; h++)
                    {
                        foreach (Action action in previousBestSequence[h + 1])
                        {
                            population[i].actionSequence[h].Add(action.createDeepClone());
                        }
                    }
                    
                    // Generate random actions for the last step
                    population[i].actionSequence[HORIZON - 1] = GenerateRandomActions(map, teamColor);
                }
                else
                {
                    // Generate random action sequences for each step in the horizon
                    for (int h = 0; h < HORIZON; h++)
                    {
                        population[i].actionSequence[h] = GenerateRandomActions(map, teamColor);
                    }
                }
            }
            
            return population;
        }
        
        // Generate random actions for one step
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
        
        // Evaluate the fitness of an individual by simulating its action sequence
        private double EvaluateIndividual(Individual individual, Map map, int teamColor)
        {
            Map simMap = map.createDeepClone();
            int enemyColor = (teamColor == 0) ? 1 : 0;
            
            // Simulate the action sequence
            for (int h = 0; h < HORIZON; h++)
            {
                // Create a copy of the action sequence to avoid modifying the original during iteration
                List<Action> actionsToExecute = new List<Action>();
                foreach (Action action in individual.actionSequence[h])
                {
                    actionsToExecute.Add(action.createDeepClone());
                }
                
                // Execute actions for the current step one by one
                foreach (Action action in actionsToExecute)
                {
                    // Make sure the action is valid in the current state
                    Unit unit = simMap.getUnit(action.operationUnitId);
                    if (unit != null && !unit.isActionFinished() && unit.getTeamColor() == teamColor)
                    {
                        // Verify the action is valid
                        if (action.actionType == Action.ACTIONTYPE_MOVEANDATTACK)
                        {
                            // For attack actions, verify the target unit still exists
                            Unit targetUnit = simMap.getUnit(action.targetUnitId);
                            if (targetUnit == null)
                            {
                                // Skip this action if the target unit no longer exists
                                continue;
                            }
                            
                            // Also verify the destination is not occupied by another unit
                            Unit existingUnit = simMap.getUnit(action.destinationXpos, action.destinationYpos);
                            if (existingUnit != null && existingUnit.getID() != action.operationUnitId)
                            {
                                // Skip this action if the destination is occupied
                                continue;
                            }
                        }
                        else if (action.actionType == Action.ACTIONTYPE_MOVEONLY)
                        {
                            // For move actions, verify the destination is not occupied by another unit
                            Unit existingUnit = simMap.getUnit(action.destinationXpos, action.destinationYpos);
                            if (existingUnit != null && existingUnit.getID() != action.operationUnitId)
                            {
                                // Skip this action if the destination is occupied
                                continue;
                            }
                        }
                        
                        simMap.executeAction(action);
                    }
                }
                
                // Enable units for the next step
                simMap.enableUnitsAction(teamColor);
                
                // Simulate enemy turn with a simple heuristic
                SimulateEnemyTurn(simMap, enemyColor);
                
                // Enable units for the next step
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
        
        // Simulate the enemy's turn with a simple heuristic
        private void SimulateEnemyTurn(Map simMap, int enemyColor)
        {
            // Get a copy of the enemy units list to avoid modification during iteration
            List<Unit> enemyUnits = new List<Unit>(simMap.getUnitsList(enemyColor, false, true, false));
            
            while (enemyUnits.Count > 0)
            {
                int randomIndex = rnd.Next(enemyUnits.Count);
                Unit enemyUnit = enemyUnits[randomIndex];
                enemyUnits.RemoveAt(randomIndex);
                
                // Skip if the unit no longer exists or has already acted
                if (enemyUnit == null || enemyUnit.isActionFinished())
                {
                    continue;
                }
                
                // Try to attack first
                List<Action> attackActions = RangeController.getAttackActionList(enemyUnit, simMap);
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
                    // Find the attack action that deals the most damage
                    Action bestAttack = null;
                    int maxDamage = -1;
                    
                    foreach (Action attack in validAttackActions)
                    {
                        int[] damages = DamageCalculator.calculateDamages(simMap, attack);
                        if (damages[0] > maxDamage)
                        {
                            maxDamage = damages[0];
                            bestAttack = attack;
                        }
                    }
                    
                    if (bestAttack != null)
                    {
                        simMap.executeAction(bestAttack);
                        continue;
                    }
                }
                
                // If no attack is possible, move towards the nearest enemy
                List<Unit> myUnits = simMap.getUnitsList((enemyColor == 0) ? 1 : 0, true, true, false);
                
                if (myUnits.Count > 0)
                {
                    // Find the nearest enemy unit
                    Unit nearestEnemy = null;
                    int minDistance = int.MaxValue;
                    
                    foreach (Unit myUnit in myUnits)
                    {
                        int distance = Math.Abs(myUnit.getXpos() - enemyUnit.getXpos()) + 
                                      Math.Abs(myUnit.getYpos() - enemyUnit.getYpos());
                        
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestEnemy = myUnit;
                        }
                    }
                    
                    if (nearestEnemy != null)
                    {
                        // Move towards the nearest enemy
                        bool[,] reachable = RangeController.getReachableCellsMatrix(enemyUnit, simMap);
                        int bestX = enemyUnit.getXpos();
                        int bestY = enemyUnit.getYpos();
                        int bestDistance = minDistance;
                        
                        // Find the best position to move to (closest to the enemy)
                        for (int x = 1; x < simMap.getXsize() - 1; x++)
                        {
                            for (int y = 1; y < simMap.getYsize() - 1; y++)
                            {
                                // Make sure the position is reachable and not occupied by another unit
                                Unit existingUnit = simMap.getUnit(x, y);
                                if (reachable[x, y] && (existingUnit == null || existingUnit.getID() == enemyUnit.getID()))
                                {
                                    int distance = Math.Abs(nearestEnemy.getXpos() - x) + 
                                                  Math.Abs(nearestEnemy.getYpos() - y);
                                    
                                    if (distance < bestDistance)
                                    {
                                        bestDistance = distance;
                                        bestX = x;
                                        bestY = y;
                                    }
                                }
                            }
                        }
                        
                        if (bestX != enemyUnit.getXpos() || bestY != enemyUnit.getYpos())
                        {
                            // Double-check that the destination is still not occupied
                            Unit existingUnit = simMap.getUnit(bestX, bestY);
                            if (existingUnit == null || existingUnit.getID() == enemyUnit.getID())
                            {
                                Action moveAction = Action.createMoveOnlyAction(enemyUnit, bestX, bestY);
                                simMap.executeAction(moveAction);
                            }
                            else
                            {
                                // If the destination is now occupied, just mark the unit as finished
                                enemyUnit.setActionFinished(true);
                            }
                        }
                        else
                        {
                            // If no better position is found, just mark the unit as finished
                            enemyUnit.setActionFinished(true);
                        }
                    }
                    else
                    {
                        // If no enemy units are found, just mark the unit as finished
                        enemyUnit.setActionFinished(true);
                    }
                }
                else
                {
                    // If no enemy units are found, just mark the unit as finished
                    enemyUnit.setActionFinished(true);
                }
            }
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
            
            // Calculate total HP for both sides
            int myTotalHP = 0;
            int enemyTotalHP = 0;
            
            foreach (Unit unit in myUnits)
            {
                myTotalHP += unit.getHP();
            }
            
            foreach (Unit unit in enemyUnits)
            {
                enemyTotalHP += unit.getHP();
            }
            
            // If the HP difference exceeds the threshold, return win/loss
            if (myTotalHP - enemyTotalHP >= simMap.getDrawHPThreshold())
            {
                return 1.0;
            }
            else if (enemyTotalHP - myTotalHP >= simMap.getDrawHPThreshold())
            {
                return 0.0;
            }
            
            // Otherwise, return a score based on the HP ratio
            double hpRatio = (double)myTotalHP / (myTotalHP + enemyTotalHP);

            double myValue = 0.0;
            double enemyValue = 0.0;
            //dict for values
            Dictionary<string, double> unitValues = new Dictionary<string, double>();
            unitValues.Add("infantry", 1.0);
            unitValues.Add("panzer", 4.0);
            foreach (Unit unit in myUnits) {
                // Console.WriteLine(unit.getName());
                myValue += unitValues[unit.getName()] * unit.getHP();
            }
            foreach (Unit unit in enemyUnits) {
                // Console.WriteLine(unit.getName());
                enemyValue += unitValues[unit.getName()] * unit.getHP();
            }
            double valueRatio = myValue / (myValue + enemyValue);

            
            // Add a small bonus for having more units
            double unitRatio = (double)myUnits.Count / (myUnits.Count + enemyUnits.Count);
            
            // Combine the two ratios with a weight
            // return 0.7 * hpRatio + 0.3 * unitRatio;
            return 0.7 * valueRatio + 0.3 * unitRatio;
        }
        
        // Evolve the population through selection, crossover, and mutation
        private Individual[] EvolvePopulation(Individual[] currentPopulation, Map map, int teamColor)
        {
            Individual[] newPopulation = new Individual[POPULATION_SIZE];
            
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
                Mutate(offspring, map, teamColor);
                
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
                if (rnd.NextDouble() < 0.5)
                {
                    foreach (Action action in parent1.actionSequence[h])
                    {
                        offspring.actionSequence[h].Add(action.createDeepClone());
                    }
                }
                else
                {
                    foreach (Action action in parent2.actionSequence[h])
                    {
                        offspring.actionSequence[h].Add(action.createDeepClone());
                    }
                }
            }
            
            return offspring;
        }
        
        // Mutation: Randomly modify an individual's action sequence
        private void Mutate(Individual individual, Map map, int teamColor)
        {
            // For each step in the horizon
            for (int h = 0; h < HORIZON; h++)
            {
                // Decide whether to mutate this step
                if (rnd.NextDouble() < MUTATION_RATE)
                {
                    // Replace with random actions
                    individual.actionSequence[h] = GenerateRandomActions(map, teamColor);
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
            // If the best individual has no actions, return a random action
            if (bestIndividual.actionSequence[0].Count == 0)
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
            
            foreach (Action action in bestIndividual.actionSequence[0])
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
