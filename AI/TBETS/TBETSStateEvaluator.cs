using System;
using System.Collections.Generic;

namespace SimpleWars
{
    /// <summary>
    /// Handles state evaluation and fitness calculations for the TBETS algorithm.
    /// </summary>
    class TBETSStateEvaluator
    {
        // Unit value dictionary for improved evaluation
        private static readonly Dictionary<string, double> UnitValues = new Dictionary<string, double>
        {
            { "infantry", 1.0 },
            { "panzer", 4.0 },
            { "cannon", 4.0 },
            { "antiair", 2.5 },
            { "fighter", 4.0 },
            { "attacker", 6.0 }
        };
        
        private Random rnd;
        /// <summary>
        /// Creates a new state evaluator with the given random number generator.
        /// </summary>
        /// <param name="random">Random number generator</param>
        public TBETSStateEvaluator(Random random)
        {
            rnd = random ?? new Random();
        }
        
        /// <summary>
        /// Evaluates the fitness of a node.
        /// </summary>
        /// <param name="node">The node to evaluate</param>
        /// <param name="teamColor">The team color of the current player</param>
        /// <returns>A fitness value between 0.0 and 1.0</returns>
        public double EvaluateNodeFitness(TBETSNode node, int evalColor)
        {
            if (node.State == null)
            {
                return 0.0;
            }
            int teamColor = node.Color;
            int enemyColor = (node.Color == 0) ? 1 : 0;
            
            node.CheckLeaf();
            if (node.IsLeaf)
            {
                double nodeFitness = EvaluateState(node.State, evalColor);
                node.Fitness = nodeFitness;
                return nodeFitness;
            }

            // Handle phantom node creation for fitness evaluation
            // 1. Generate a phantom child if one doesn't exist (opposing player)
            // 2. Generate a phantom child of the phantom child (back to original player)
            // 3. Set fitness of original node to evaluation of grandchild
            
            // Check if node already has phantom children
            TBETSNode phantomChild = null;
            
            foreach (TBETSNode child in node.Children)
            {
                if (child.IsPhantom && child.Color != node.Color)
                {
                    phantomChild = child;
                    break;
                }
            }
            
            // If no phantom child exists, create one
            if (phantomChild == null)
            {
                // Create phantom node representing opponent's turn
                phantomChild = new TBETSNode(1 - node.Color, node.Depth + 1, node);
                phantomChild.IsPhantom = true; // Mark as phantom
                phantomChild.State = node.State.createDeepClone();
                
                SimulateTurn(phantomChild);
                
                // Add phantom child to parent node
                node.AddChild(phantomChild);
            }

            node.Successor = phantomChild;
            phantomChild.CheckLeaf();
            if (phantomChild.IsLeaf)
            {
                double childFitness = EvaluateState(phantomChild.State, evalColor);
                node.Fitness = childFitness;
                return childFitness;
            }
            
            // Check if phantom child has a phantom grandchild
            TBETSNode phantomGrandchild = null;
            
            foreach (TBETSNode grandchild in phantomChild.Children)
            {
                if (grandchild.IsPhantom && grandchild.Color != phantomChild.Color)
                {
                    phantomGrandchild = grandchild;
                    break;
                }
            }
            
            // If no phantom grandchild exists, create one
            if (phantomGrandchild == null)
            {
                // Create phantom grandchild (back to original player's turn)
                phantomGrandchild = new TBETSNode(1 - phantomChild.Color, phantomChild.Depth + 1, phantomChild);
                phantomGrandchild.IsPhantom = true; // Mark as phantom
                phantomGrandchild.State = phantomChild.State.createDeepClone();
                
                SimulateTurn(phantomGrandchild);
                
                // Add phantom grandchild to phantom child
                phantomChild.AddChild(phantomGrandchild);
            }

            // Evaluate the state of the phantom grandchild
            double fitness = EvaluateState(phantomGrandchild.State, evalColor);
            node.Fitness = fitness;
            return fitness;
        }
        
        /// <summary>
        /// Simulates a turn for a given node.
        /// </summary>
        /// <param name="node">The node to simulate a turn for. node contains no actions and the state prior to acting</param>
        private void SimulateTurn(TBETSNode node)
        {
            int myColor = node.Color;
            int enemyColor = (myColor == 0) ? 1 : 0;
            
            // Following the logic of AI_Sample_MaxActEval
            
            // Step 1: Calculate threats of enemy units
            List<Unit> enemies = node.State.getUnitsList(myColor, false, false, true);  // All enemy units
            
            foreach (Unit enemyUnit in enemies)
            {
                int damage = EstimateMaxAttackDamage(enemyUnit, node.State);
                
                // Initialize and store threat value in unit's custom int array
                enemyUnit.initX_ints(1);
                enemyUnit.setX_int(0, damage); // Using index 0 for threat value
            }
            
            // Step 2: Process all units that can act
            List<Unit> myUnits = node.State.getUnitsList(myColor, false, true, false);  // All unacted units
            
            while (myUnits.Count > 0)
            {
                // Step 2a: Try to find the best attack action
                List<Action> attackActions = AiTools.getAllAttackActions(myColor, node.State);
                
                int maxAttackValue = -20; // Minimum efficiency threshold for attacks
                Action bestAttack = null;
                
                foreach (Action attack in attackActions)
                {
                    int attackValue = EvaluateAttackAction(attack, node.State, myColor);
                    
                    if (attackValue > maxAttackValue)
                    {
                        maxAttackValue = attackValue;
                        bestAttack = attack;
                    }
                }
                
                if (bestAttack != null)
                {
                    // Execute the best attack action
                    node.State.executeAction(bestAttack);
                    node.AddAction(bestAttack.createDeepClone());
                }
                else
                {
                    // Step 2b: If no good attack found, move a random unit
                    if (myUnits.Count == 0) break; // Safety check
                    
                    // Select a random unit to move
                    Unit unitToMove = myUnits[rnd.Next(myUnits.Count)];
                    
                    // Get reachable cells
                    bool[,] movable = RangeController.getReachableCellsMatrix(unitToMove, node.State);
                    
                    // Find the best move based on terrain and proximity to enemies
                    int maxScore = int.MinValue;
                    int bestX = unitToMove.getXpos(); // Default to current position
                    int bestY = unitToMove.getYpos();
                    
                    for (int x = 1; x < node.State.getXsize() - 1; x++)
                    {
                        for (int y = 1; y < node.State.getYsize() - 1; y++)
                        {
                            if (!movable[x, y]) continue;
                            
                            // Skip if cell is occupied by another unit
                            Unit existingUnit = node.State.getUnit(x, y);
                            if (existingUnit != null && existingUnit.getID() != unitToMove.getID()) continue;
                            
                            // Calculate defensive bonus
                            int defense = node.State.getFieldDefensiveEffect(x, y);
                            if (unitToMove.getSpec().isAirUnit()) defense = 0;
                            
                            // Calculate score for this position
                            int localMaxScore = int.MinValue;
                            
                            foreach (Unit enemy in enemies)
                            {
                                int dist = Math.Abs(x - enemy.getXpos()) + Math.Abs(y - enemy.getYpos());
                                int effect = unitToMove.getSpec().getUnitAtkPower(enemy.getSpec().getUnitType());
                                
                                // Score formula from AI_Sample_MaxActEval
                                int score = defense * 5 + effect / (dist + 5);
                                
                                if (score > localMaxScore) localMaxScore = score;
                            }
                            
                            if (localMaxScore > maxScore)
                            {
                                maxScore = localMaxScore;
                                bestX = x;
                                bestY = y;
                            }
                        }
                    }
                    
                    // Create and execute the move action
                    Action moveAction = Action.createMoveOnlyAction(unitToMove, bestX, bestY);
                    node.State.executeAction(moveAction);
                    node.AddAction(moveAction.createDeepClone());
                }
                
                // Refresh the list of unacted units
                myUnits = node.State.getUnitsList(myColor, false, true, false);
            }
            
            // End turn
            node.EndTurn();
        }
        
        /// <summary>
        /// Estimates the maximum damage an attacking unit can deal to any opponent unit.
        /// </summary>
        /// <param name="attackingUnit">The unit to calculate threat for</param>
        /// <param name="map">The current game state</param>
        /// <returns>The maximum possible damage</returns>
        private int EstimateMaxAttackDamage(Unit attackingUnit, Map map)
        {
            int maxDamage = 0;
            List<Action> attackActions = RangeController.getAttackActionList(attackingUnit, map);
            
            foreach (Action action in attackActions)
            {
                Unit targetUnit = map.getUnit(action.targetUnitId);
                
                // Only consider units that have already acted
                if (targetUnit.isActionFinished() == false) continue;
                
                int[] damages = DamageCalculator.calculateDamages(attackingUnit, targetUnit, map);
                if (damages[0] > maxDamage) maxDamage = damages[0];
            }
            
            return maxDamage;
        }
        
        /// <summary>
        /// Evaluates an attack action to determine its value.
        /// </summary>
        /// <param name="action">The attack action to evaluate</param>
        /// <param name="map">The current game state</param>
        /// <param name="teamColor">The team color of the attacking unit</param>
        /// <returns>A value representing the desirability of the attack</returns>
        private int EvaluateAttackAction(Action action, Map map, int teamColor)
        {
            Unit attackingUnit = map.getUnit(action.operationUnitId);
            Unit targetUnit = map.getUnit(action.targetUnitId);
            
            if (attackingUnit == null || targetUnit == null) return int.MinValue;
            
            int[] damages = DamageCalculator.calculateDamages(map, action);
            
            // If no damage would be dealt, return minimum value
            if (damages[0] == 0) return int.MinValue;
            
            // Calculate unit values
            int attackerValue = 10 + attackingUnit.getHP(); // Base value + HP
            int targetValue = 10 + targetUnit.getX_int(0);  // Base value + threat
            
            // Calculate action value
            int actionValue = (damages[0] * targetValue) - (damages[1] * attackerValue);
            
            return actionValue;
        }

        /// <summary>
        /// Evaluates the state of the game.
        /// </summary>
        /// <param name="simMap">The map to evaluate</param>
        /// <param name="teamColor">The team color of the current player</param>
        /// <returns>A fitness value between 0.0 and 1.0</returns>
        public double EvaluateState(Map simMap, int teamColor)
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
            
            // Calculate total value for both sides
            double myValue = 0.0;
            double enemyValue = 0.0;
            
            foreach (Unit unit in myUnits)
            {
                string unitName = unit.getName().ToLower();
                if (UnitValues.TryGetValue(unitName, out double value))
                {
                    myValue += value * unit.getHP();
                }
                else
                {
                    myValue += unit.getHP(); // Default to HP if unit type not in dictionary
                }
            }
            
            foreach (Unit unit in enemyUnits)
            {
                string unitName = unit.getName().ToLower();
                if (UnitValues.TryGetValue(unitName, out double value))
                {
                    enemyValue += value * unit.getHP();
                }
                else
                {
                    enemyValue += unit.getHP(); // Default to HP if unit type not in dictionary
                }
            }
            
            // Calculate value ratio
            double valueRatio = myValue / (myValue + enemyValue);
            
            // Add a small bonus for having more units
            double unitRatio = (double)myUnits.Count / (myUnits.Count + enemyUnits.Count);
            
            // Combine the two ratios with weights
            return 0.7 * valueRatio + 0.3 * unitRatio;
        }
    }
}
