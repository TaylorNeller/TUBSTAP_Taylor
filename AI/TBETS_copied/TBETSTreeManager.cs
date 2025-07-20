using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleWars
{
    /// <summary>
    /// Manages the tree search operations for the TBETS algorithm.
    /// Handles node creation, evolution, and pruning.
    /// </summary>
    class TBETSTreeManager
    {
        private readonly Random rnd;
        private readonly TBETSStateEvaluator evaluator;
        private readonly TBETSActionGenerator actionGenerator;
        private readonly TBETSDuplicateManager duplicateManager;
        private readonly TBETSNodeSelector nodeSelector;
        
        // Probability of mutating an action during node mutation
        private const double MUTATION_RATE = 0.4;
        
        // List of all unexploited nodes across all depths and both players
        public List<TBETSNode> unexploitedNodes;
        public List<TBETSNode> exploredNodes;
        
        /// <summary>
        /// Creates a new tree manager with the given parameters.
        /// </summary>
        /// <param name="random">Random number generator</param>
        /// <param name="attackBias">Bias towards attack actions in random generation</param>
        public TBETSTreeManager(Random random, double attackBias)
        {
            rnd = random ?? new Random();
            evaluator = new TBETSStateEvaluator(rnd);
            actionGenerator = new TBETSActionGenerator(rnd, attackBias);
            duplicateManager = new TBETSDuplicateManager();
            nodeSelector = new TBETSNodeSelector(rnd);
            unexploitedNodes = new List<TBETSNode>();
            exploredNodes = new List<TBETSNode>();
        }
        
        /// <summary>
        /// Generate random actions for one turn using the action generator.
        /// </summary>
        /// <param name="map">Current game map</param>
        /// <param name="teamColor">Team color to generate actions for</param>
        /// <returns>List of randomly generated actions</returns>
        public List<Action> GenerateRandomActions(TBETSNode node)
        {
            return actionGenerator.GenerateRandomActions(node.State, node.Color);
        }
        
        /// <summary>
        /// Check if there are any explored nodes in the tree.
        /// </summary>
        /// <param name="root">Root node of the tree</param>
        /// <returns>True if there are any explored nodes, false otherwise</returns>
        public bool HasExploredNodes(TBETSNode root)
        {
            return nodeSelector.HasExploredNodes(root);
        }
        
        /// <summary>
        /// Check if there are any unexplored nodes in the tree.
        /// </summary>
        /// <returns>True if there are any unexplored nodes, false otherwise</returns>
        public bool HasUnexploredNodes()
        {
            return unexploitedNodes.Count > 0;
        }

        public void PrintUnexploited() {
            
            Console.WriteLine("Unexploited Nodes:");
            foreach (TBETSNode node in unexploitedNodes)
            {
                node.PrintSelf();
            }
            Console.WriteLine("Total Unexploited Nodes: " + unexploitedNodes.Count);
        }
        
        /// <summary>
        /// Adds a node to the list of unexploited nodes if appropriate.
        /// </summary>
        /// <param name="node">Node to add</param>
        public void AddUnexploitedNode(TBETSNode node)
        {
            if (node == null || node.Explored || !node.IsPrimary || node.IsLeaf)
            {
                throw new ArgumentException("Node must be non-null, unexplored, and a primary node.");
            }
            if (unexploitedNodes.Contains(node))
            {
                node.PrintSelf();
                throw new ArgumentException("Node is already in the unexploited nodes list.");
            }
            unexploitedNodes.Add(node);
        }
        public void RemoveExploitedNode(TBETSNode node)
        {
            unexploitedNodes.Remove(node);
            if (!node.Explored) {
                throw new ArgumentException("Node must be explored to remove from unexploited nodes.");
            }
        }

        public void AddUnexploitedRecursive(TBETSNode node) {
            if (node.IsPrimary && !node.Explored && !node.IsLeaf) {
                if (unexploitedNodes.Contains(node)) {
                    throw new ArgumentException("Node is already in the unexploited nodes list.");
                }
                unexploitedNodes.Add(node);
            }
            foreach (TBETSNode child in node.Children)
            {
                AddUnexploitedRecursive(child);
            }
        }

        public void RemoveUnexploitedRecursive(TBETSNode node) {
            if (unexploitedNodes.Contains(node)) {
                unexploitedNodes.Remove(node);
                node.Deleted = true;
            }
            foreach (TBETSNode child in node.Children)
            {
                RemoveUnexploitedRecursive(child);
            }
        }

        public bool CanBeExploited(TBETSNode node) {
            return unexploitedNodes.Contains(node);
        }
        
        /// <summary>
        /// Select an explored node for further evolution.
        /// </summary>
        /// <param name="root">Root node of the tree</param>
        /// <returns>Selected explored node</returns>
        public TBETSNode SelectExploredNode(TBETSNode root, int iter, int n_iters)
        {
            return nodeSelector.SelectExploredNode(root, iter, n_iters);
        }

        public TBETSNode SelectNode(TBETSNode root, int iter, int n_iters)
        {
            return nodeSelector.SelectNode(root, iter, n_iters);
        }
        
        /// <summary>
        /// Select an unexplored node from the unexploited nodes list,
        /// biasing towards high fitness for player nodes or low fitness for enemy nodes.
        /// </summary>
        /// <returns>Selected unexplored node, or null if none available</returns>
        public TBETSNode SelectUnexploredNode()
        {
            if (unexploitedNodes.Count == 0)
                return null;
            
            // Sort nodes by fitness, considering node type
            List<TBETSNode> sortedNodes = new List<TBETSNode>(unexploitedNodes);
            sortedNodes.Sort((a, b) => 
            {
                if (a.IsPlayerNode && b.IsPlayerNode)
                {
                    // For player nodes, higher fitness is better
                    return b.Fitness.CompareTo(a.Fitness);
                }
                else if (!a.IsPlayerNode && !b.IsPlayerNode)
                {
                    // For enemy nodes, lower fitness is better
                    return a.Fitness.CompareTo(b.Fitness);
                }
                // If comparing mixed types, prioritize by depth
                return a.Depth.CompareTo(b.Depth);
            });
            
            // Use tournament selection to bias towards better nodes while maintaining some randomness
            int tournamentSize = Math.Min(3, sortedNodes.Count);
            int selectedIndex = 0;
            
            // Pick from top performers with bias (tournament selection)
            double r = rnd.NextDouble();
            // Squared random gives higher bias towards top nodes
            selectedIndex = (int)(r * r * Math.Min(5, sortedNodes.Count));
            
            TBETSNode selectedNode = sortedNodes[selectedIndex];
            unexploitedNodes.Remove(selectedNode);
            
            return selectedNode;
        }
        
        /// <summary>
        /// Create a new node by mutating an existing one.
        /// Mutation goes through each action in the node and potentially changes it to another legal move.
        /// Starts with the state of the parent node and applies actions one by one.
        /// </summary>
        /// <param name="node">Node to mutate</param>
        /// <returns>New mutated node</returns>
        public TBETSNode MutateNode(TBETSNode node)
        {
            if (node == null || node.State == null || node.Actions.Count == 0 || node.Parent == null)
            {
                Console.WriteLine(node);
                Console.WriteLine("Node state: " + (node?.State?.toString() ?? "null"));
                Console.WriteLine("Node actions: " + (node?.Actions?.Count.ToString() ?? "null"));
                Console.WriteLine(node.Parent);
                throw new ArgumentNullException("Mutation: Node cannot be null and must have a valid state and parent.");
            }
            
            // Create a new node with same properties as the original
            TBETSNode mutated = new TBETSNode(node.Color, node.Depth, node.Parent);

            // Start with the state of the PARENT of the node being mutated
            mutated.State = node.Parent.State.createDeepClone();
            foreach (Action action in node.TakenActions)
            {
                if (action == null) {
                    throw new ArgumentNullException("Mutate: Action in node is null.");
                }
                mutated.AddTakenAction(action.createDeepClone());
            }
            mutated.ApplyActions(); // apply any TakenActions

            // Get all units for the team
            List<Unit> units = new List<Unit>(mutated.State.getUnitsList(mutated.Color, false, true, false));
            
            // Create a map of unit IDs to Units for quick lookup
            Dictionary<int, Unit> unitMap = new Dictionary<int, Unit>();
            foreach (Unit unit in units)
            {
                unitMap[unit.getID()] = unit;
            }
            
            // Process each action from the original node
            foreach (Action origAction in node.Actions)
            {
                Action action = origAction.createDeepClone();
                int unitId = action.operationUnitId;
                Unit unit = null;
                try {
                    unit = unitMap[unitId];
                }
                catch (KeyNotFoundException)
                {
                    // print unitMap 
                    
                    node.PrintRecursive();
                    Console.WriteLine("Unit ID not found in unitMap: " + unitId);
                    Console.WriteLine("IDs in unitMap: " + string.Join(", ", unitMap.Keys));
                    // print units too
                    Console.WriteLine("Units in unitMap: " + string.Join(", ", unitMap.Values.Select(u => u.getID())));
                    // print all units in the state
                    units = new List<Unit>(mutated.State.getUnitsList(mutated.Color, false, true, false));
                    Console.WriteLine("Units in state: " + string.Join(", ", units.Select(u => u.getID())));


                    // print state
                    Console.WriteLine(mutated.State.toString());
                    Console.WriteLine("Original action: " + action.toString());
                    //throw error
                    throw new KeyNotFoundException($"Unit with ID {unitId} not found in the unit map.");
                }
                
                // With MUTATION_RATE probability, modify this action
                if (rnd.NextDouble() < MUTATION_RATE)
                {
                    // Generate a new action for this unit
                    Action newAction = actionGenerator.GenerateBiasedAction(unit, mutated.State);
                    
                    if (newAction != null)
                    {
                        mutated.AddAction(newAction);
                        
                        // Apply the action to update the state
                        if (ActionChecker.isTheActionLegalMove_Silent(newAction, mutated.State))
                        {
                            mutated.State.executeAction(newAction);
                        }
                    }
                }
                else
                {
                    // Check if the original action is still legal
                    if (ActionChecker.isTheActionLegalMove_Silent(action, mutated.State))
                    {
                        mutated.AddAction(action);
                        mutated.State.executeAction(action);
                    }
                    else
                    {
                        // If not legal, generate a new action
                        Action newAction = actionGenerator.GenerateBiasedAction(unit, mutated.State);
                        
                        if (newAction != null)
                        {
                            mutated.AddAction(newAction);
                            
                            // Apply the action to update the state
                            if (ActionChecker.isTheActionLegalMove_Silent(newAction, mutated.State))
                            {
                                mutated.State.executeAction(newAction);
                            }
                        }
                    }
                }
            }
            // end turn
            mutated.EndTurn();
            
            // Calculate state hash for node comparisons
            mutated.CalculateStateHash();
            
            return mutated;
        }

        public TBETSNode FullCrossover(TBETSNode node) {
            // setup intial state
            TBETSNode offspring = new TBETSNode(1 - node.Color, node.Depth + 1, node);
            offspring.State = node.State.createDeepClone();

            // Sanity check - each of node's children must have the same TakenActions
            TBETSNode child1 = node.Children[0];
            foreach (TBETSNode child in node.Children)
            {
                if (!child.HasSameTakenActions(child1) && child != child1)
                {
                    throw new ArgumentException("Crossover2: Children do not have the same TakenActions.");  
                }
            }

            // apply takenactions
            foreach (Action action in child1.TakenActions)
            {
                if (action == null) {
                    throw new ArgumentNullException("Crossover2: Action in child1 is null.");
                }
                offspring.AddTakenAction(action.createDeepClone());
            }
            offspring.ApplyActions(); // apply any TakenActions

            // create dict of unit id : <dict of actions : <list of fitness, count>>
            Dictionary<int, Dictionary<Action, List<double>>> unitActionMap = new Dictionary<int, Dictionary<Action, List<double>>>();
            // dict of actions : best node
            Dictionary<Action, TBETSNode> actionBestNodeMap = new Dictionary<Action, TBETSNode>();
            // catalogue fitness of actions
            foreach (TBETSNode child in node.Children)
            {
                foreach (Action action in child.Actions)
                {
                    if (action == null) {
                        throw new ArgumentNullException("Crossover2: Action in child is null.");
                    }
                    int unitId = action.operationUnitId;
                    if (!unitActionMap.ContainsKey(unitId))
                    {
                        unitActionMap[unitId] = new Dictionary<Action, List<double>>();
                    }
                    Dictionary<Action, List<double>> actionMap = unitActionMap[unitId];
                    if (!actionMap.ContainsKey(action))
                    {
                        actionMap[action] = new List<double> { child.Fitness, 1 };
                        actionBestNodeMap[action] = child;
                    }
                    else {
                        if (actionBestNodeMap[action].Fitness < child.Fitness)
                        {
                            actionBestNodeMap[action] = child;
                        }
                        List<double> score_count = actionMap[action];
                        score_count[0] += child.Fitness;
                        score_count[1] += 1;
                    }
                }
            }

            Dictionary<int, List<Action>> sortedActionsByUnit = new Dictionary<int, List<Action>>();

            foreach (var unitEntry in unitActionMap)
            {
                int unitId = unitEntry.Key;
                Dictionary<Action, List<double>> actionScores = unitEntry.Value;

                List<Action> sortedActions = actionScores
                    .OrderByDescending(entry => entry.Value[0] / entry.Value[1])  // Sort by average score
                    .Select(entry => entry.Key)
                    .ToList();

                sortedActionsByUnit[unitId] = sortedActions;
            }
            
            // Generate a new action based on the best actions for each unit
            List<Unit> units = new List<Unit>(offspring.State.getUnitsList(offspring.Color, false, true, false));
            while (units.Count > 0) {
                // Get a random unit from the offspring's state
                int randomIndex = rnd.Next(units.Count);
                Unit unit = units[randomIndex];

                Action selectedAction = null;
                // Get the list of actions for this unit
                if (sortedActionsByUnit.ContainsKey(unit.getID()))
                {
                    List<Action> actions = sortedActionsByUnit[unit.getID()];
                    for (int i = 0; i < actions.Count; i++)
                    {
                        // Check if the action is legal
                        if (ActionChecker.isTheActionLegalMove_Silent(actions[i], offspring.State))
                        {
                            // If legal, add it to the offspring
                            selectedAction = actions[i].createDeepClone();
                            break;
                        }
                    }
                }
                if (selectedAction == null)
                {
                    // If no legal action found, generate a random one
                    // TODO: consider logging this
                    selectedAction = actionGenerator.GenerateBiasedAction(unit, offspring.State);
                }
                offspring.AddAction(selectedAction);
                offspring.State.executeAction(selectedAction);
                units.RemoveAt(randomIndex);
            }

            offspring.EndTurn();
            offspring.CalculateStateHash();
            return offspring;
        }
        
        /// <summary>
        /// Create a new node by crossing over two parent nodes.
        /// Combines approximately half the actions from each parent,
        /// maintaining the ordering from both parents.
        /// </summary>
        /// <param name="parent1">First parent node</param>
        /// <param name="parent2">Second parent node</param>
        /// <returns>New offspring node</returns>
        public TBETSNode CrossoverNodes(TBETSNode parent1, TBETSNode parent2)
        {
            if (parent1.Parent != parent2.Parent)
                throw new ArgumentNullException("parents aren't siblings.");
            
            TBETSNode commonParent = parent1.Parent;
            
            // setup intial state
            TBETSNode offspring = new TBETSNode(parent1.Color, parent1.Depth, commonParent);
            offspring.State = commonParent.State.createDeepClone();
            foreach (Action action in parent1.TakenActions)
            {
                if (action == null) {
                    throw new ArgumentNullException("Crossover: Action in parent1 is null.");
                }
                offspring.AddTakenAction(action.createDeepClone());
            }
            offspring.ApplyActions(); // apply any TakenActions

            // Get all units for the team and create a map for quick lookup
            List<Unit> units = offspring.State.getUnitsList(offspring.Color, false, true, false);
            Dictionary<int, Unit> unitMap = new Dictionary<int, Unit>();
            foreach (Unit unit in units)
            {
                unitMap[unit.getID()] = unit;
            }
            
            // Create maps of unit IDs to actions from both parents
            Dictionary<int, Action> parent1Actions = new Dictionary<int, Action>();
            Dictionary<int, Action> parent2Actions = new Dictionary<int, Action>();
            
            foreach (Action action in parent1.Actions)
            {
                int unitId = action.operationUnitId;
                if (unitMap.ContainsKey(unitId))
                {
                    parent1Actions[unitId] = action.createDeepClone();
                }
            }
            
            foreach (Action action in parent2.Actions)
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
            foreach (Action action in parent2.Actions)
            {
                int unitId = action.operationUnitId;
                if (!parent2UnitOrder.Contains(unitId) && unitMap.ContainsKey(unitId) && !keepFromParent1.Contains(unitId))
                {
                    parent2UnitOrder.Add(unitId);
                }
            }
            
            // Get the order of units from parent1 that we're keeping
            List<int> parent1UnitOrder = new List<int>();
            foreach (Action action in parent1.Actions)
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
                for (int i = 0; i < parent1.Actions.Count; i++)
                {
                    if (parent1.Actions[i].operationUnitId == unitId)
                    {
                        position = i;
                        break;
                    }
                }
                
                // Calculate relative position in the combined list
                int insertPosition = (int)((position / (double)parent1.Actions.Count) * combinedUnitOrder.Count);
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
                if (ActionChecker.isTheActionLegalMove_Silent(selectedAction, offspring.State))
                {
                    offspring.AddAction(selectedAction);
                    offspring.State.executeAction(selectedAction);
                }
                else
                {
                    // Step 4: If illegal, try to modify it to a similar attack target if possible
                    Action modifiedAction = null;
                    
                    if (selectedAction.actionType == Action.ACTIONTYPE_MOVEANDATTACK)
                    {
                        // Try to find another valid attack target at the same position
                        List<Action> attackActions = RangeController.getAttackActionList(unit, offspring.State);
                        
                        foreach (Action attack in attackActions)
                        {
                            // If we can attack from the same position
                            if (attack.destinationXpos == selectedAction.destinationXpos && 
                                attack.destinationYpos == selectedAction.destinationYpos)
                            {
                                if (ActionChecker.isTheActionLegalMove_Silent(attack, offspring.State))
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
                        modifiedAction = actionGenerator.GenerateBiasedAction(unit, offspring.State);
                    }
                    
                    offspring.AddAction(modifiedAction);
                    offspring.State.executeAction(modifiedAction);
                }
            }
            
            // end turn
            offspring.EndTurn();
            // Calculate state hash for node comparisons
            offspring.CalculateStateHash();
            
            return offspring;
        }
        
        /// <summary>
        /// Checks if a node is a duplicate of any node in a list.
        /// </summary>
        /// <param name="existingNodes">List of existing nodes</param>
        /// <param name="newNode">New node to check</param>
        /// <returns>True if the node is a duplicate, false otherwise</returns>
        public bool CheckForDuplicates(List<TBETSNode> existingNodes, TBETSNode newNode)
        {
            if (existingNodes == null || newNode == null)
            {
                return false;
            }
            
            // First check for identical action sequences
            foreach (TBETSNode existingNode in existingNodes)
            {
                if (existingNode.HasSameActions(newNode))
                {
                    return true;
                }
            }
            
            // Then check for state equivalence
            return duplicateManager.HandleDuplicateNode(existingNodes, newNode);
        }
        
        /// <summary>
        /// Select a high fitness node from a list.
        /// </summary>
        /// <param name="nodes">List of nodes to select from</param>
        /// <param name="isPlayerNode">Whether these are player nodes</param>
        /// <param name="exclude">Optional node to exclude</param>
        /// <returns>Selected high fitness node</returns>
        public TBETSNode SelectHighFitnessChild(TBETSNode parent, int PlayerColor, int iter, int n_iters, TBETSNode exclude = null)
        {
            return nodeSelector.SelectHighFitnessChild(parent, PlayerColor, iter, n_iters, exclude);
        }
        
        /// <summary>
        /// Get the lowest fitness among a list of nodes.
        /// </summary>
        /// <param name="nodes">List of nodes</param>
        /// <returns>Lowest fitness value</returns>
        public double GetLowestFitness(List<TBETSNode> nodes)
        {
            return nodeSelector.GetLowestFitness(nodes);
        }
        
        /// <summary>
        /// Get the best player node from a list.
        /// </summary>
        /// <param name="nodes">List of nodes</param>
        /// <returns>Node with highest fitness</returns>
        public TBETSNode GetBestPlayerNode(TBETSNode root)
        {
            return nodeSelector.GetBestPlayerNode(root);
        }

        /// <summary>
        /// Updates the fitness of a node and propagates the change to all its duplicates.
        /// </summary>
        /// <param name="node">The node whose fitness is being updated</param>
        /// <param name="newFitness">The new fitness value</param>
        public void PropagateFitness(TBETSNode node, int PlayerColor)
        {
            duplicateManager.UpdateNodeFitness(node, PlayerColor);
        }
        
        /// <summary>
        /// Prune branches of the tree that don't start with the specified action.
        /// Updated to keep children that have the action later, if it's not an attack on a unit
        /// being attacked earlier by another unit.
        /// </summary>
        /// <param name="root">Root node of the tree</param>
        /// <param name="action">Action to keep</param>
        /// <returns>New root node after pruning, which might be empty if the last action was taken</returns>
        public TBETSNode PruneTreeByFirstAction(TBETSNode root, TBETSNode chosenChild, Action action)
        {
            if (root == null) {
                throw new ArgumentNullException("Pruning: Root node cannot be null.");
            }
            if (action == null) {
                throw new ArgumentNullException("Pruning: Action cannot be null.");
            }
            // Console.WriteLine("child at start: ");
            // string starts = "";
            // foreach (TBETSNode child in root.Children)
            // {
            //     starts += child.StateHash + ", ";
            // }
            // Console.WriteLine(starts);

            // Console.WriteLine("Pruning: action: " + action.toString());
            // Console.WriteLine("Before pruning:");
            // root.PrintChildren();

            // list of nodes to remove from children
            List<TBETSNode> nodesToRemove = new List<TBETSNode>();

            // dict of children (kept) and the action to remove
            Dictionary<TBETSNode, Action> updates = new Dictionary<TBETSNode, Action>();
            
            foreach (TBETSNode child in root.Children)
            {                
                // Check if first action matches
                if (actionGenerator.MatchesAction(child.Actions[0], action))
                {
                    // add child and action to dict
                    updates[child] = child.Actions[0];
                }
                // Check if any other action matches, considering attack conflicts
                else
                {
                    int target = action.targetUnitId;
                    bool keepNode = false;
                    foreach (Action childAction in child.Actions)
                    {
                        
                        // If it's the same action, we can keep this node
                        if (actionGenerator.MatchesAction(childAction, action))
                        {
                            keepNode = true;
                            // add child and action to dict
                            updates[child] = childAction;
                        }
                        if (childAction.targetUnitId == target && target != -1) { // different attack action targeted same unit first
                            break;
                        }
                    }
                    if (!keepNode) {
                        // If no matching action found, mark this child for removal
                        nodesToRemove.Add(child);
                    }
                }
            }

            // Remove all marked nodes from the children list
            root.Children.RemoveAll(child => nodesToRemove.Contains(child));

            // Remove actions from kept nodes
            // for each node in dict, remove action
            foreach (KeyValuePair<TBETSNode, Action> entry in updates)
            {
                TBETSNode child = entry.Key;
                Action actionToRemove = entry.Value;
                if (actionToRemove == null) {
                    throw new ArgumentNullException("Pruning: Action to remove cannot be null.");
                }
                child.Actions.Remove(actionToRemove);
                child.AddTakenAction(actionToRemove);
            }

            // check if any duplicates nodes are now identical to parents, in which case they should be removed
            // gather primary nodes in list
            List<TBETSNode> duplicatesToRemove = new List<TBETSNode>();
            foreach (TBETSNode child in root.Children)
            {
                if (child.IsPrimary)
                {
                    foreach (TBETSNode duplicate in child.Duplicates)
                    {
                        if (duplicate.HasSameActions(child))
                        {
                            duplicatesToRemove.Add(duplicate);
                            if (duplicate.Children.Count > 1) {
                                throw new Exception("Pruning: Found a duplicate node with multiple children, which should not happen.");
                            }
                        }
                    }
                }
            }

            root.Children.RemoveAll(child => duplicatesToRemove.Contains(child));

            
            // Console.WriteLine("Pruning: After pruning:");
            // root.PrintChildren();
            // root.PrintRecursive();


            List<TBETSNode> primariesReadded = new List<TBETSNode>();
            // check if any untethered duplicates exist
            for (int i = 0; i < root.Children.Count; i++)
            {
                TBETSNode child = root.Children[i];
                if (!child.IsPrimary && !root.Children.Contains(child.PrimaryNode))
                {
                    TBETSNode primary = child.PrimaryNode;
                    if (!child.IsSameState(primary))
                    {
                        throw new Exception("Pruning: Found a duplicate node with different state than its primary node.");
                    }

                    // readd primary node with duplicate's actions
                    primary.Actions = child.Actions;
                    primary.TakenActions = child.TakenActions;
                    primary.RemoveDuplicate(child);

                    root.Children[i] = primary;
                    primariesReadded.Add(primary);
                }
            }
            
            // Console.WriteLine("removed children: ");
            string removed_str = ""; 
            // prune unexploited
            foreach (TBETSNode child in nodesToRemove)
            {
                removed_str += ", " + child.StateHash;
                if (!primariesReadded.Contains(child)) {

                    if (root.Children.Contains(child)) {
                        throw new Exception("Pruning: Child node should not be in the root's children after pruning.");
                    }
                    if (child.IsPrimary) {
                        removed_str += " DEL ";
                        RemoveUnexploitedRecursive(child);
                    }
                }
            }

            // Console.WriteLine(removed_str);
            // Console.WriteLine("child at end: ");
            // string ends = "";
            // foreach (TBETSNode child in root.Children)
            // {
            //     ends += child.StateHash + ", ";
            // }
            // Console.WriteLine(ends);

            // last action was taken. there should be one remaining child
            if (root.Children[0].Actions.Count == 0)
            {
                Logger.addLogMessage("Pruning: Last action was taken, no more actions left.\r\n", 1-root.Color);
                // Console.WriteLine("Pruning: Last action was taken, no more actions left.");
                if (root.Children.Count > 1) {
                    root.PrintChildren();
                    Console.WriteLine(root.Children[0].State.toString());
                    Console.WriteLine(root.Children[1].State.toString());
                    Console.WriteLine(action.toString());
                    // Console.WriteLine(root.Children[0].Actions[0].toString());
                    // Console.WriteLine(root.Children[1].Actions[0].toString());
                    throw new Exception("Pruning: More than one child left after last action was taken.");
                }
                return root.Children[0];
            }



            return root;
        }

        public double EvaluateFitness(TBETSNode node, int teamColor) {
            return evaluator.EvaluateNodeFitness(node, teamColor);
        }
        
        /// <summary>
        /// Clear all unexploited nodes.
        /// </summary>
        public void ClearUnexploitedNodes()
        {
            unexploitedNodes.Clear();
        }



    }
}
