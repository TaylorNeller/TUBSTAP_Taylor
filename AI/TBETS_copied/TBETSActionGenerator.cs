using System;
using System.Collections.Generic;

namespace SimpleWars
{
    /// <summary>
    /// Handles action generation and validation for the TBETS algorithm.
    /// </summary>
    class TBETSActionGenerator
    {
        private readonly Random rnd;
        private readonly double attackBias;
        
        /// <summary>
        /// Creates a new action generator with the given parameters.
        /// </summary>
        /// <param name="random">Random number generator</param>
        /// <param name="attackBias">Bias towards attack actions in random generation</param>
        public TBETSActionGenerator(Random random, double attackBias)
        {
            rnd = random ?? new Random();
            this.attackBias = attackBias;
        }
        
        /// <summary>
        /// Generate random actions for one turn.
        /// </summary>
        /// <param name="map">Current game map</param>
        /// <param name="teamColor">Team color to generate actions for</param>
        /// <returns>List of randomly generated actions</returns>
        public List<Action> GenerateRandomActions(Map map, int teamColor)
        {
            List<Action> actions = new List<Action>();
            Map simMap = map.createDeepClone();
            
            // Get all units for this team, including already acted units
            List<Unit> allUnits = new List<Unit>(simMap.getUnitsList(teamColor, false, true, false));
            
            // For each unit, generate a random action
            foreach (Unit unit in allUnits)
            {
                // Skip if the unit no longer exists
                if (unit == null)
                {
                    continue;
                }
                
                // If unit has already acted, we still need an action for it (move in place)
                if (unit.isActionFinished())
                {
                    // Create a move-in-place action
                    Action stayAction = Action.createMoveOnlyAction(unit, unit.getXpos(), unit.getYpos());
                    actions.Add(stayAction);
                    continue;
                }
                
                // Decide whether to prioritize attack or movement
                bool prioritizeAttack = rnd.NextDouble() < attackBias;
                bool actionAdded = false;
                
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
                            
                            // Double-check attack range after movement
                            int originalX = unit.getXpos();
                            int originalY = unit.getYpos();
                            unit.setPos(attack.destinationXpos, attack.destinationYpos);
                            bool[,] attackable = RangeController.getAttackableCellsMatrix(unit, simMap);
                            unit.setPos(originalX, originalY);
                            
                            if (!attackable[targetUnit.getXpos(), targetUnit.getYpos()])
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
                        Action actionClone = action.createDeepClone();
                        actions.Add(actionClone);
                        simMap.executeAction(action);
                        actionAdded = true;
                    }
                }
                
                // If no attack action added or not prioritizing attack, try movement
                if (!actionAdded)
                {
                    List<Action> moveActions = new List<Action>();
                    bool[,] reachable = RangeController.getReachableCellsMatrix(unit, simMap);
                    
                    // Always include the current position (move in place)
                    moveActions.Add(Action.createMoveOnlyAction(unit, unit.getXpos(), unit.getYpos()));
                    
                    for (int x = 1; x < simMap.getXsize() - 1; x++)
                    {
                        for (int y = 1; y < simMap.getYsize() - 1; y++)
                        {
                            // Skip the current position (already added)
                            if (x == unit.getXpos() && y == unit.getYpos())
                            {
                                continue;
                            }
                            
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
                            Action actionClone = action.createDeepClone();
                            actions.Add(actionClone);
                            simMap.executeAction(action);
                            actionAdded = true;
                        }
                    }
                }
                
                // Fallback: If no action was added for any reason, add a move-in-place action
                if (!actionAdded)
                {
                    Action stayAction = Action.createMoveOnlyAction(unit, unit.getXpos(), unit.getYpos());
                    actions.Add(stayAction);
                }
            }
            
            return actions;
        }
        
        
        /// <summary>
        /// Validates an attack action to ensure it's still legal.
        /// </summary>
        /// <param name="action">The attack action to validate</param>
        /// <param name="unit">The unit performing the action</param>
        /// <param name="map">The current game state</param>
        /// <returns>True if the action is valid, false otherwise</returns>
        private bool ValidateAttackAction(Action action, Unit unit, Map map)
        {
            // Verify the target unit still exists
            Unit targetUnit = map.getUnit(action.targetUnitId);
            if (targetUnit == null)
            {
                return false;
            }
            
            // Verify the destination is not occupied by another unit
            Unit existingUnit = map.getUnit(action.destinationXpos, action.destinationYpos);
            if (existingUnit != null && existingUnit.getID() != action.operationUnitId)
            {
                return false;
            }
            
            // Verify attack range - first move the unit to the destination
            int originalX = unit.getXpos();
            int originalY = unit.getYpos();
            unit.setPos(action.destinationXpos, action.destinationYpos);
            
            // Check if the target is within attack range from the new position
            bool[,] attackable = RangeController.getAttackableCellsMatrix(unit, map);
            
            // Reset the unit position
            unit.setPos(originalX, originalY);
            
            return attackable[targetUnit.getXpos(), targetUnit.getYpos()];
        }
        
        /// <summary>
        /// Validates a move-only action to ensure it's still legal.
        /// </summary>
        /// <param name="action">The move action to validate</param>
        /// <param name="unit">The unit performing the action</param>
        /// <param name="map">The current game state</param>
        /// <returns>True if the action is valid, false otherwise</returns>
        private bool ValidateMoveAction(Action action, Unit unit, Map map)
        {
            // Verify the destination is not occupied by another unit
            Unit existingUnit = map.getUnit(action.destinationXpos, action.destinationYpos);
            if (existingUnit != null && existingUnit.getID() != action.operationUnitId)
            {
                return false;
            }
            
            // Verify the destination is reachable
            bool[,] reachable = RangeController.getReachableCellsMatrix(unit, map);
            return reachable[action.destinationXpos, action.destinationYpos];
        }
        
        /// <summary>
        /// Try to find a valid move action for a unit.
        /// </summary>
        /// <param name="unit">The unit to find a move for</param>
        /// <param name="map">The current game state</param>
        /// <param name="action">Reference to the action to update</param>
        /// <returns>True if a valid move was found, false otherwise</returns>
        private bool TryFindMoveAction(Unit unit, Map map, ref Action action)
        {
            bool[,] reachable = RangeController.getReachableCellsMatrix(unit, map);
            List<Action> possibleMoves = new List<Action>();
            
            // Always include the current position (move in place)
            possibleMoves.Add(Action.createMoveOnlyAction(unit, unit.getXpos(), unit.getYpos()));
            
            for (int x = 1; x < map.getXsize() - 1; x++)
            {
                for (int y = 1; y < map.getYsize() - 1; y++)
                {
                    // Skip the current position (already added)
                    if (x == unit.getXpos() && y == unit.getYpos())
                    {
                        continue;
                    }
                    
                    Unit existingUnit = map.getUnit(x, y);
                    if (reachable[x, y] && (existingUnit == null || existingUnit.getID() == unit.getID()))
                    {
                        possibleMoves.Add(Action.createMoveOnlyAction(unit, x, y));
                    }
                }
            }
            
            // Choose a random move (we'll always have at least the move-in-place option)
            action = possibleMoves[rnd.Next(possibleMoves.Count)].createDeepClone();
            return true;
        }
        
        /// <summary>
        /// Generates a mutated action for a unit.
        /// </summary>
        /// <param name="unit">The unit to generate an action for</param>
        /// <param name="map">The current game state</param>
        /// <returns>A new random action for the unit</returns>
        public Action GenerateMutatedAction(Unit unit, Map map)
        {
            if (unit == null)
            {
                throw new Exception("TBETSActionGenerator: Unit should never be null in GenerateMutatedAction.");
            }
            
            // Get all possible actions for this unit
            List<Action> possibleActions = new List<Action>();
            
            // Add move-in-place action
            possibleActions.Add(Action.createMoveOnlyAction(unit, unit.getXpos(), unit.getYpos()));
            
            // Get attack actions
            List<Action> attackActions = RangeController.getAttackActionList(unit, map);
            foreach (Action attack in attackActions)
            {
                // Verify validity
                if (attack.actionType == Action.ACTIONTYPE_MOVEANDATTACK)
                {
                    Unit targetUnit = map.getUnit(attack.targetUnitId);
                    if (targetUnit == null)
                    {
                        continue;
                    }
                    
                    Unit existingUnit = map.getUnit(attack.destinationXpos, attack.destinationYpos);
                    if (existingUnit != null && existingUnit.getID() != attack.operationUnitId)
                    {
                        continue;
                    }
                }
                
                possibleActions.Add(attack);
            }
            
            // Get move actions
            bool[,] reachable = RangeController.getReachableCellsMatrix(unit, map);
            for (int x = 1; x < map.getXsize() - 1; x++)
            {
                for (int y = 1; y < map.getYsize() - 1; y++)
                {
                    // Skip current position (already added)
                    if (x == unit.getXpos() && y == unit.getYpos())
                    {
                        continue;
                    }
                    
                    Unit existingUnit = map.getUnit(x, y);
                    if (reachable[x, y] && (existingUnit == null || existingUnit.getID() == unit.getID()))
                    {
                        possibleActions.Add(Action.createMoveOnlyAction(unit, x, y));
                    }
                }
            }
            
            // Select a random action from the possibilities
            if (possibleActions.Count > 0)
            {
                return possibleActions[rnd.Next(possibleActions.Count)].createDeepClone();
            }
            
            // Fallback to move-in-place
            return Action.createMoveOnlyAction(unit, unit.getXpos(), unit.getYpos());
        }
        
        /// <summary>
        /// Generate a random legal action for a unit with a bias towards attack actions.
        /// </summary>
        /// <param name="unit">The unit to generate an action for</param>
        /// <param name="state">Current game state</param>
        /// <returns>A legal action for the unit</returns>
        public Action GenerateBiasedAction(Unit unit, Map state)
        {
            if (unit == null || state == null)
            {
                return null;
            }
            
            // First try to generate attack actions (with higher probability)
            if (rnd.NextDouble() < attackBias) // Higher bias towards attacks
            {
                List<Action> attackActions = RangeController.getAttackActionList(unit, state);
                List<Action> validAttacks = new List<Action>();
                
                foreach (Action attack in attackActions)
                {
                    if (ActionChecker.isTheActionLegalMove_Silent(attack, state))
                    {
                        validAttacks.Add(attack);
                    }
                }
                
                if (validAttacks.Count > 0)
                {
                    return validAttacks[rnd.Next(validAttacks.Count)].createDeepClone();
                }
            }
            
            // If no attack action is generated, try movement actions
            bool[,] reachable = RangeController.getReachableCellsMatrix(unit, state);
            List<Action> moveActions = new List<Action>();
            
            // Always include the current position (move in place)
            Action stayAction = Action.createMoveOnlyAction(unit, unit.getXpos(), unit.getYpos());
            if (ActionChecker.isTheActionLegalMove_Silent(stayAction, state))
            {
                moveActions.Add(stayAction);
            }
            
            // Add all possible move actions
            for (int x = 1; x < state.getXsize() - 1; x++)
            {
                for (int y = 1; y < state.getYsize() - 1; y++)
                {
                    // Skip current position (already added)
                    if (x == unit.getXpos() && y == unit.getYpos())
                    {
                        continue;
                    }
                    
                    if (reachable[x, y])
                    {
                        Action moveAction = Action.createMoveOnlyAction(unit, x, y);
                        if (ActionChecker.isTheActionLegalMove_Silent(moveAction, state))
                        {
                            moveActions.Add(moveAction);
                        }
                    }
                }
            }
            
            if (moveActions.Count > 0)
            {
                return moveActions[rnd.Next(moveActions.Count)].createDeepClone();
            }
            
            // If no valid action found, return a stay action
            return Action.createMoveOnlyAction(unit, unit.getXpos(), unit.getYpos());
        }
        
        /// <summary>
        /// Check if two actions match (considering action order for attack actions).
        /// </summary>
        /// <param name="a">First action</param>
        /// <param name="b">Second action</param>
        /// <returns>True if actions match</returns>
        public bool MatchesAction(Action a, Action b)
        {
            if (a.actionType != b.actionType ||
                a.operationUnitId != b.operationUnitId)
            {
                return false;
            }
            
            if (a.actionType == Action.ACTIONTYPE_MOVEANDATTACK)
            {
                return a.targetUnitId == b.targetUnitId &&
                       a.destinationXpos == b.destinationXpos &&
                       a.destinationYpos == b.destinationYpos;
            }
            else if (a.actionType == Action.ACTIONTYPE_MOVEONLY)
            {
                return a.destinationXpos == b.destinationXpos &&
                       a.destinationYpos == b.destinationYpos;
            }
            
            return true;
        }
    }
}
