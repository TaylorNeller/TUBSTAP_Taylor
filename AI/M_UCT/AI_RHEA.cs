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
         // private const int HORIZON = 2;               
        // private const int POPULATION_SIZE = 30;        
        // private const int NUM_GENERATIONS = 20;       
        // private const double MUTATION_RATE = 0.4;     
        // private const double CROSSOVER_RATE = 0.8;    
        // private const int TOURNAMENT_SIZE = 3;        
        // private const long LIMIT_TIME = 9700;        
        // private const int ELITISM_COUNT = 3;          
        // private const double ATTACK_BIAS = 0.8;       
        private const int HORIZON = 2;               
        private const int POPULATION_SIZE = 30;       
        private const int NUM_GENERATIONS = -1;        
        private const double MUTATION_RATE = 0.4;     // controls mutation rate per action within action sequences
        private const double CROSSOVER_RATE = 0.8;    
        private const int TOURNAMENT_SIZE = 3;         
        private const long LIMIT_TIME = AI_Consts.LIMIT_TIME;          
        private const int ELITISM_COUNT = 3;          
        private const double ATTACK_BIAS = 0.8;     

        // A random number generator (reused throughout the agent)
        private Random rnd = new Random();

        private TBETSActionGenerator actionGenerator;

        // Stopwatch for time management
        private Stopwatch stopwatch = new Stopwatch();
        private static long timeLeft;
        
        // Previous best action sequence (for shift buffer)
        private List<Action>[] previousBestSequence = null;
        
        // Class to represent an individual in the population
        public class Individual
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
            return "RHEA";
        }

        public string showParameters()
        {
            return "HORIZON = " + HORIZON + 
                   ", POPULATION_SIZE = " + POPULATION_SIZE + 
                   ", NUM_GENERATIONS = " + NUM_GENERATIONS + 
                   ", MUTATION_RATE = " + MUTATION_RATE + 
                   ", CROSSOVER_RATE = " + CROSSOVER_RATE;
        }
        
        public AI_RHEA()
        {
            actionGenerator = new TBETSActionGenerator(rnd, ATTACK_BIAS);
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

            Individual[] population = RunRHEA(map, teamColor, timeLeft / movableUnitsCount);

            // Find the best individual
            Individual bestIndividual = GetBestIndividual(population);

            // Store the best sequence for the next call (shift buffer)
            previousBestSequence = bestIndividual.actionSequence;

            // Get the best action from the best individual
            Action bestAction = GetBestAction(bestIndividual, map, teamColor);

            // Check if legal move (should never be illegal)
            if (!ActionChecker.isTheActionLegalMove_Silent(bestAction, map))
            {
                Console.WriteLine(map.toString());
                Console.WriteLine($"AI-RHEA: Action {bestAction.ToString()} is illegal in the current state.");
                // generate a valid move (move in place)
                List<Unit> movableUnits = new List<Unit>(map.getUnitsList(teamColor, false, true, false));
                bestAction = Action.createMoveOnlyAction(movableUnits[0], movableUnits[0].getXpos(), movableUnits[0].getYpos());
            }

            stopwatch.Stop();
            Logger.addLogMessage("RHEA: Time used: " + stopwatch.ElapsedMilliseconds + "ms\r\n", teamColor);
            timeLeft -= stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();

            // Console.WriteLine("RHEA: Best action: " + bestAction.toOneLineString());

            return bestAction;
        }

        public Individual[] RunRHEA(Map map, int teamColor, long timeLimit)
        {
            Stopwatch subloopWatch = new Stopwatch();
            subloopWatch.Start();

            // Initialize population
            Individual[] population = InitializePopulation(map, teamColor);

            // Evaluate initial population
            EvaluatePopulation(population, map, teamColor);

            int n_iters = NUM_GENERATIONS;
            if (NUM_GENERATIONS < 0)
            {
                n_iters = int.MaxValue;
            }

            // Evolve population for a number of generations or until time runs out
            for (int generation = 0; generation < n_iters; generation++)
            {
                if (subloopWatch.ElapsedMilliseconds > timeLimit)
                {
                    Logger.addLogMessage("RHEA: Time limit reached after " + generation + " generations\r\n", teamColor);
                    break;
                }

                // Create new population through selection, crossover, and mutation
                population = EvolvePopulation(population, map, teamColor);

                // Evaluate new population
                EvaluatePopulation(population, map, teamColor);
            }
            subloopWatch.Stop();
            return population;
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
            return AI_RHEA.EvaluateState(simMap, teamColor);
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
        public static double EvaluateState(Map simMap, int teamColor)
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
            unitValues.Add("cannon", 4.0);
            unitValues.Add("antiair", 2.5);
            unitValues.Add("fighter", 4);
            unitValues.Add("attacker", 6);
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
                    offspring = Crossover(parent1, parent2, teamColor, map);
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

        // private Individual Crossover(Individual parent1, Individual parent2)
        // {
        //     Individual offspring = new Individual(HORIZON);
            
        //     // Uniform crossover: For each step, randomly choose actions from either parent
        //     for (int h = 0; h < HORIZON; h++)
        //     {
        //         if (rnd.NextDouble() < 0.5)
        //         {
        //             foreach (Action action in parent1.actionSequence[h])
        //             {
        //                 offspring.actionSequence[h].Add(action.createDeepClone());
        //             }
        //         }
        //         else
        //         {
        //             foreach (Action action in parent2.actionSequence[h])
        //             {
        //                 offspring.actionSequence[h].Add(action.createDeepClone());
        //             }
        //         }
        //     }
            
        //     return offspring;
        // }

        // Crossover: Combine two parents to create an offspring
        private Individual Crossover(Individual parent1, Individual parent2, int teamColor, Map map)
        {
            // return Crossover(parent1, parent2);
            Individual offspring = new Individual(HORIZON);
            Map state = map.createDeepClone();

            // for each action sequence in the horizon, perform crossover operation.
            for (int h = 0; h < HORIZON; h++)
            {
                List<Action> offspringActions = new List<Action>();

                // Get all units for the team and create a map for quick lookup
                List<Unit> units = state.getUnitsList(teamColor, false, true, false);
                Dictionary<int, Unit> unitMap = new Dictionary<int, Unit>();
                foreach (Unit unit in units)
                {
                    unitMap[unit.getID()] = unit;
                }

                // Create maps of unit IDs to actions from both parents
                Dictionary<int, Action> parent1Actions = new Dictionary<int, Action>();
                Dictionary<int, Action> parent2Actions = new Dictionary<int, Action>();

                foreach (Action action in parent1.actionSequence[h])
                {
                    int unitId = action.operationUnitId;
                    if (unitMap.ContainsKey(unitId))
                    {
                        parent1Actions[unitId] = action.createDeepClone();
                    }
                }

                foreach (Action action in parent2.actionSequence[h])
                {
                    int unitId = action.operationUnitId;
                    if (unitMap.ContainsKey(unitId))
                    {
                        parent2Actions[unitId] = action.createDeepClone();
                    }
                }

                // Step 1: Determine which units' actions to keep from parent1 (approximately half)
                HashSet<int> keepFromParent1 = new HashSet<int>();
                List<int> unitsFromParent1 = new List<int>(parent1Actions.Keys);

                // Shuffle the list to randomize which units to keep from parent1
                for (int i = unitsFromParent1.Count - 1; i > 0; i--)
                {
                    int j = rnd.Next(i + 1);
                    int temp = unitsFromParent1[i];
                    unitsFromParent1[i] = unitsFromParent1[j];
                    unitsFromParent1[j] = temp;
                }

                // Select approximately half of the units from parent1
                int halfCount = (int)Math.Ceiling(unitsFromParent1.Count / 2.0);
                for (int i = 0; i < halfCount; i++)
                {
                    if (i < unitsFromParent1.Count)
                    {
                        keepFromParent1.Add(unitsFromParent1[i]);
                    }
                }

                // Step 2: Create a sequence of actions preserving ordering from both parents
                List<Action> combinedActions = new List<Action>();

                // Units to process from parent2 (in parent2's order)
                List<int> unitsToProcessFromParent2 = new List<int>();

                // Get the order of units from parent2
                List<int> parent2UnitOrder = new List<int>();
                foreach (Action action in parent2.actionSequence[h])
                {
                    int unitId = action.operationUnitId;
                    if (!parent2UnitOrder.Contains(unitId) && unitMap.ContainsKey(unitId) && !keepFromParent1.Contains(unitId))
                    {
                        parent2UnitOrder.Add(unitId);
                    }
                }

                // Get the order of units from parent1 that we're keeping
                List<int> parent1UnitOrder = new List<int>();
                foreach (Action action in parent1.actionSequence[h])
                {
                    int unitId = action.operationUnitId;
                    if (!parent1UnitOrder.Contains(unitId) && unitMap.ContainsKey(unitId) && keepFromParent1.Contains(unitId))
                    {
                        parent1UnitOrder.Add(unitId);
                    }
                }

                // Combine actions from both parents, preserving their ordering
                List<int> combinedUnitOrder = new List<int>();

                // First add all units from parent2 that we're not keeping from parent1
                foreach (int unitId in parent2UnitOrder)
                {
                    if (unitMap.ContainsKey(unitId) && !keepFromParent1.Contains(unitId))
                    {
                        combinedUnitOrder.Add(unitId);
                    }
                }

                // Now integrate the units we're keeping from parent1 at their relative positions
                foreach (int unitId in parent1UnitOrder)
                {
                    // Find the position of this unit in parent1's list
                    int position = -1;
                    for (int i = 0; i < parent1.actionSequence[h].Count; i++)
                    {
                        if (parent1.actionSequence[h][i].operationUnitId == unitId)
                        {
                            position = i;
                            break;
                        }
                    }

                    // Calculate relative position in the combined list
                    int insertPosition = (int)((position / (double)parent1.actionSequence[h].Count) * combinedUnitOrder.Count);
                    insertPosition = Math.Min(insertPosition, combinedUnitOrder.Count);

                    // Insert at the calculated position
                    combinedUnitOrder.Insert(insertPosition, unitId);

                }

                // Step 3: Execute actions in the combined order, checking legality
                foreach (int unitId in combinedUnitOrder)
                {
                    Unit unit = unitMap[unitId];
                    Action selectedAction = null;

                    // Get action from appropriate parent
                    if (keepFromParent1.Contains(unitId) && parent1Actions.ContainsKey(unitId))
                    {
                        selectedAction = parent1Actions[unitId].createDeepClone();
                    }
                    else if (parent2Actions.ContainsKey(unitId))
                    {
                        selectedAction = parent2Actions[unitId].createDeepClone();
                    }

                    // Check legality and apply action
                    if (ActionChecker.isTheActionLegalMove_Silent(selectedAction, state))
                    {
                        offspringActions.Add(selectedAction);
                        state.executeAction(selectedAction);
                    }
                    else
                    {
                        // Step 4: If illegal, try to modify it to a similar attack target if possible
                        Action modifiedAction = null;

                        if (selectedAction.actionType == Action.ACTIONTYPE_MOVEANDATTACK)
                        {
                            // Try to find another valid attack target at the same position
                            List<Action> attackActions = RangeController.getAttackActionList(unit, state);

                            foreach (Action attack in attackActions)
                            {
                                // If we can attack from the same position
                                if (attack.destinationXpos == selectedAction.destinationXpos &&
                                    attack.destinationYpos == selectedAction.destinationYpos)
                                {
                                    if (ActionChecker.isTheActionLegalMove_Silent(attack, state))
                                    {
                                        modifiedAction = attack.createDeepClone();
                                        break;
                                    }
                                }
                            }
                        }

                        // Step 5: If still no valid action, generate a random biased one
                        if (modifiedAction == null)
                        {
                            modifiedAction = actionGenerator.GenerateBiasedAction(unit, state);
                        }

                        offspringActions.Add(modifiedAction);
                        state.executeAction(modifiedAction);
                    }
                }

                offspring.actionSequence[h] = offspringActions;
                state.enableUnitsAction(teamColor);
                state.incTurnCount();
                SimulateEnemyTurn(state, teamColor == 0 ? 1 : 0);
                state.enableUnitsAction(teamColor == 0 ? 1 : 0);
                state.incTurnCount();
            }

            return offspring;
        }
        
        // Mutation: Randomly modify an individual's action sequence
        private void Mutate(Individual individual, Map map, int teamColor)
        {
            Map state = map.createDeepClone();
            // For each step in the horizon
            for (int h = 0; h < HORIZON; h++)
            {
                List<Action> turnActions = individual.actionSequence[h];
                List<Action> newActions = new List<Action>();

                foreach (Action action in turnActions)
                {
                    Unit unit = state.getUnit(action.operationUnitId);
                    if (unit == null)
                    {
                        continue;
                    }

                    // offspring.actionSequence[h].Add(action.createDeepClone());
                    Action originalAction = action.createDeepClone();
                    // With MUTATION_RATE probability, modify this action
                    if (rnd.NextDouble() < MUTATION_RATE)
                    {
                        // Generate a new action for this unit
                        Action newAction = actionGenerator.GenerateBiasedAction(unit, state);
                        newActions.Add(newAction);
                        state.executeAction(newAction);
                    }
                    else
                    {
                        // Check if the original action is still legal
                        if (ActionChecker.isTheActionLegalMove_Silent(originalAction, state))
                        {
                            newActions.Add(originalAction.createDeepClone());
                            state.executeAction(originalAction);
                        }
                        else
                        {
                            // If not legal, generate a new action
                            Action newAction = actionGenerator.GenerateBiasedAction(unit, state);
                            newActions.Add(newAction);
                            state.executeAction(newAction);
                        }
                    }
                }

                // take any other actions that need to be taken.
                List<Unit> movableUnits = state.getUnitsList(teamColor, false, true, false);
                foreach (Unit unit in movableUnits)
                {
                    // Generate a biased action for each movable unit
                    Action action = actionGenerator.GenerateBiasedAction(unit, state);
                    newActions.Add(action);
                    state.executeAction(action);
                }

                individual.actionSequence[h] = newActions;
                state.enableUnitsAction(teamColor);
                state.incTurnCount();
                SimulateEnemyTurn(state, teamColor == 0 ? 1 : 0);
                state.enableUnitsAction(teamColor == 0 ? 1 : 0);
                state.incTurnCount();
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
            foreach (Action action in bestIndividual.actionSequence[0])
            {
                if (ActionChecker.isTheActionLegalMove_Silent(action, map))
                {
                    return action;
                }
                else
                {
                    // Console.WriteLine("RHEA: Invalid action found in the best individual."); // only seems to happen when no valid action is found
                }
            }
            Console.WriteLine("RHEA: No valid action found in the best individual.");

            // create a move in place action.
            List<Unit> movableUnits = new List<Unit>(map.getUnitsList(teamColor, false, true, false));
            Action moveInPlace = Action.createMoveOnlyAction(movableUnits[0], movableUnits[0].getXpos(), movableUnits[0].getYpos());
            return moveInPlace;
        }
    }
}
