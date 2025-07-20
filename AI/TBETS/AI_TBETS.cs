using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SimpleWars
{
    /// <summary>
    /// Turn Based Evolutionary Tree-Search (TBETS) Agent
    /// This algorithm progressively generates a game tree of nodes
    /// Each node holds the actions for a full turn. There are player nodes and enemy nodes.
    /// </summary>
    class AI_TBETS : Player
    {
        // Static property to store the current player's color for node checks
        public static int PlayerColor { get; private set; }
        
        // Algorithm parameters
        private const int NUM_INIT = 20;              // Number of initial randomized player nodes
        private const int RHEA_INDIVIDUALS = 0;       // Number of RHEA individuals to seed the root with
        private const long RHEA_BUDGET_FRACTION = 3; // Fraction of action budget to spend on RHEA seeding, e.g. 3 means 1/3 of budget
        private const int N_ITERS = -1;              // Number of exploration iterations
        private const double MUTATION_RATIO = 0.3;     // Chance to mutate instead of crossover
        private const double PERCENT_FULL_CROSSOVER = 0.0;     // Chance to mutate instead of crossover
        private const int STARTING_POP = 5;           // Number of children for unexplored nodes
        private const double ATK_BIAS = .8;          // Bias towards attack actions in random generation
        private const long LIMIT_TIME = AI_Consts.LIMIT_TIME;         // Time limit for decision making in milliseconds

        // Stopwatch for time management
        private readonly Stopwatch stopwatch = new Stopwatch();
        private static long timeLeft = LIMIT_TIME;
        
        // Random number generator
        private readonly Random rnd = new Random();
        
        // Tree management components
        private readonly TBETSTreeManager treeManager;
         
        // Saved tree from previous function calls
        private TBETSNode savedTree = null;
        
        /// <summary>
        /// Constructor for the TBETS AI agent.
        /// </summary>
        public AI_TBETS()
        {
            treeManager = new TBETSTreeManager(rnd, ATK_BIAS);
        }

        /// <summary>
        /// Gets the name of the AI.
        /// </summary>
        /// <returns>The name of the AI</returns>
        public string getName()
        {
            return "TBETS";
        }

        /// <summary>
        /// Shows the parameter settings of the AI.
        /// </summary>
        /// <returns>A string describing the parameter settings</returns>
        public string showParameters()
        {
            return "NUM_INIT = " + NUM_INIT + 
                   ", N_ITERS = " + N_ITERS + 
                   ", STARTING_POP = " + STARTING_POP +
                   ", ATK_BIAS = " + ATK_BIAS;
        }

        /// <summary>
        /// Main function to make an action decision. Current function in GameManager doesn't ask for action when none can be taken (all units moved)
        /// </summary>
        /// <param name="map">Current game map</param>
        /// <param name="teamColor">Team color of the current player</param>
        /// <param name="turnStart">Whether this is the start of a turn</param>
        /// <param name="gameStart">Whether this is the start of a game</param>
        /// <returns>The chosen action to perform</returns>
        public Action makeAction(Map map, int teamColor, bool turnStart, bool gameStart)
        {
            if (gameStart)
            {
                WarmUp(map.createDeepClone(), teamColor);
                savedTree = null;
            }
            stopwatch.Start();

            // Console.WriteLine(map.toString());

            
            if (turnStart)
            {
                timeLeft = LIMIT_TIME;
            }
            
            int movableUnitsCount = map.getUnitsList(teamColor, false, true, false).Count;
            if (movableUnitsCount == 0)
            {
                return Action.createTurnEndAction();
            }
            long actionBudget = timeLeft / movableUnitsCount;
            
            // Set the static PlayerColor for node checks
            PlayerColor = teamColor;
            
            // Get enemy color
            int enemyColor = (teamColor == 0) ? 1 : 0;
            
            // Begin with an empty root enemy node
            TBETSNode root = null;


            bool newRoot = true;
            // If a tree exists
            if (savedTree != null)
            {
                root = savedTree;
                newRoot = false;

                if (root.State.getTurnCount() > map.getTurnCount())
                {
                    // new game, need new root
                    newRoot = true;
                    Logger.addLogMessage("TBETS: New game detected; map turn is less than saved root\r\n", teamColor);
                    // Console.WriteLine("TBETS: New game detected; map turn is less than saved root\r\n");
                }
                else {
                    // If the root is empty (no actions left) then assume we are seeing ENEMY ACTIONS that we predicted that happened

                    // If root is not an enemy node, the turn must have ended and the enemy moved
                    if (root.Color == teamColor)
                    {
                        // search for a matching child node (enemy) with the current map state
                        newRoot = true;
                        // Console.WriteLine("New turn detected; matching child node.");
                        Logger.addLogMessage("TBETS: New turn detected; matching child node.\r\n", teamColor);
                        // root.PrintRecursive();
                        foreach (TBETSNode child in root.Children)
                        {
                            if (MapsAreEquivalent(map, child.State))
                            {
                                // Console.WriteLine("Cache hit - found matching child node.");
                                Logger.addLogMessage("TBETS: Cache hit - found matching child node.\r\n", teamColor);

                                // prune rest of tree from unexploitedNodes
                                for (int i = 0; i < root.Children.Count; i++)
                                {
                                    TBETSNode otherChild = root.Children[i];
                                    if (otherChild != child && otherChild.IsPrimary)
                                    {
                                        treeManager.RemoveUnexploitedRecursive(otherChild);
                                    }
                                }

                                // Set matching child as new root
                                root = child;
                                savedTree = root;
                                newRoot = false;

                                if (root.Children.Count == 1) { // unexploited
                                    if (root.Explored) {
                                        // root.PrintSelf();
                                        // root.PrintRecursive();
                                        // throw new Exception("AI_TBETS: Expected root to be unexplored if it has only 1 child.");
                                    }
                                    else {
                                        SeedRoot(root, actionBudget);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            string string_root = "null";
            if (newRoot)
            {
                treeManager.ClearUnexploitedNodes();

                // Console.WriteLine("Cache miss - creating new root node.");
                Logger.addLogMessage("TBETS: Cache miss - creating new root node.\r\n", teamColor);
                root = new TBETSNode(enemyColor, 0, null);
                root.State = map.createDeepClone();

                FitnessUpdate(root);                
                SeedRoot(root, actionBudget);
            }
            else {
                string_root = root.StringRecursive();
                if (treeManager.unexploitedNodes.Count == 0) {
                    // root.PrintRecursive();
                    // Console.WriteLine(root.State.toString());
                    // int[,] map2 = root.State.getFieldTypeArray();
                    // // print map
                    // for (int y = 0; y < map2.GetLength(1); y++)
                    // {
                    //     for (int x = 0; x < map2.GetLength(0); x++)
                    //     {
                    //         Console.Write(map2[x, y] + " ");
                    //     }
                    //     Console.WriteLine();
                    // }
                    // throw new ArgumentException("AI_TBETS: No unexploited nodes found in the tree. This indicates an issue with the tree management.");
                    Logger.addLogMessage("TBETS: Loaded root with no available nodes to exploit.\r\n", teamColor);
                }
            }
            string debug = "";
            // Main exploration loop

            int n_iters = N_ITERS;
            if (N_ITERS < 0) {
                n_iters = int.MaxValue;
            }

            int completedIters = 0;
            for (int i = 0; i < n_iters; i++)
            {
                completedIters = i;
                // Check for time limit
                if (stopwatch.ElapsedMilliseconds > (timeLeft / movableUnitsCount))
                {
                    Logger.addLogMessage("TBETS: Time limit reached after " + i + " iters\r\n", teamColor);
                    break;
                    // throw new TimeoutException("TBETS: Time limit reached after " + i + " iters");
                }

                // SELECTION
                TBETSNode selectedNode = treeManager.SelectNode(root, i, N_ITERS);

                // EXPANSION
                if (selectedNode.IsLeaf)
                {
                    debug += "TBETS: Selected node is leaf.\n";
                    Logger.addLogMessage("TBETS: Selected node is leaf.\r\n", teamColor);
                    continue;
                }
                if (!selectedNode.Explored)
                {
                    // EXPLOIT
                    debug += "TBETS: Selected node is EXPLOITABLE. depth=" + selectedNode.Depth + "\n";
                    Exploit(selectedNode);
                }
                else
                {
                    // CROSSOVER/MUTATION
                    debug += "TBETS: Selected node is being EXPLORED. depth=" + selectedNode.Depth + "\n";
                    if (rnd.NextDouble() < MUTATION_RATIO || selectedNode.Children.Count == 1)
                    {
                        // MUTATION: Pick 1 well performing node and mutate it
                        TBETSNode highFitnessNode = treeManager.SelectHighFitnessChild(selectedNode, PlayerColor, i, N_ITERS);
                        if (highFitnessNode != null)
                        {
                            TBETSNode mutatedNode = treeManager.MutateNode(highFitnessNode);

                            // Add the mutated node to the tree
                            AddNode(selectedNode, mutatedNode);
                        }
                    }
                    else
                    {
                        if (rnd.NextDouble() < PERCENT_FULL_CROSSOVER)
                        {
                            // FULL CROSSOVER: perform a crossover across all nodes
                            TBETSNode offspring = treeManager.FullCrossover(selectedNode);
                        }
                        else
                        {
                            // CROSSOVER: Pick 2 high fitness nodes and create a new node with genetic crossover
                            TBETSNode parent1 = treeManager.SelectHighFitnessChild(selectedNode, PlayerColor, i, N_ITERS);
                            TBETSNode parent2 = treeManager.SelectHighFitnessChild(selectedNode, PlayerColor, i, N_ITERS, parent1);
                            TBETSNode offspring = treeManager.CrossoverNodes(parent1, parent2);
                            AddNode(selectedNode, offspring);
                        }
                    }
                }
            }
            // Console.WriteLine("TBETS: Iterations completed: " + N_ITERS);
            // Find the best action sequence from the root's children
            TBETSNode bestNode = treeManager.GetBestPlayerNode(root);
            bool oneMoveFlag = false;
            if (bestNode == null) {
                if (root.Children.Count == 1) { // it's possible there is only one legal move (e.g. trapped artillary), in which case all iterations will be spent "exploring" the root node
                    bestNode = root.Children[0];
                    oneMoveFlag = true;
                    Console.WriteLine("TBETS: Root has only 1 child. (only 1 legal move?).");
                }
                else {  //something has gone wrong
                    // root.PrintChildren();
                    // root.PrintRecursive();
                    foreach (TBETSNode node in root.Children) {
                        if (treeManager.CanBeExploited(node)) {
                            Console.WriteLine($"Node {node} can be exploited.");
                        }
                    }
                    Console.WriteLine("Original root:");
                    // Console.WriteLine(string_root);
                    Console.WriteLine(root.State.toString());
                    Console.WriteLine("Completed iters: " + completedIters);
                    Console.WriteLine(root.nDescendents + " descendents");
                    throw new ArgumentException($"AI_TBETS: No explored nodes found in the list to select from.\n" +
                        "[" + debug + "]" + root.PrintChildren() + "\n" + root.StringRecursive() + "\n" + string_root + "\n" + root.State.toString());
                }
            }

            // debugging print
            // root.PrintRecursive();
            // throw new Exception("AI_TBETS: Debugging print - root tree printed.");

            if (!bestNode.Explored && !oneMoveFlag)
            {
                Console.WriteLine("TBETS: Best node is unexplored. This should not happen.");
            }

            // verify fitness integrity
            root.FitnessUpdate(PlayerColor);
            root.FitnessIntegrityCheck(PlayerColor);
            
            // Return the first action from the best node
            Action firstAction = null;
            
            // Get the first action from the best node with highest fitness
            firstAction = bestNode.Actions[0].createDeepClone();
            
            // Prune branches that don't start with this action
            savedTree = treeManager.PruneTreeByFirstAction(root, bestNode, firstAction);

            // Check if legal move (should never be illegal)
            if (!ActionChecker.isTheActionLegalMove_Silent(firstAction, map))
            {
                Console.WriteLine(map.toString());
                Console.WriteLine($"AI-TBETS: Action {firstAction.ToString()} is illegal in the current state.");
                // generate a valid move (move in place)
                List<Unit> movableUnits = new List<Unit>(map.getUnitsList(teamColor, false, true, false));
                firstAction = Action.createMoveOnlyAction(movableUnits[0], movableUnits[0].getXpos(), movableUnits[0].getYpos());
            }
            stopwatch.Stop();
            Logger.addLogMessage("TBETS: Time used: " + stopwatch.ElapsedMilliseconds + "ms\r\n", teamColor);
            timeLeft -= stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();

            // Console.WriteLine("TBETS: Chosen action: " + (firstAction != null ? firstAction.ToString() : "TURN END"));

            return firstAction;
        }

        private void WarmUp(Map map, int teamColor)
        {
            // This method is designed to be called once at the start of a game.
            // Its purpose is to execute a lightweight version of the main logic
            // to ensure that critical methods are JIT-compiled before the
            // first time-sensitive call to makeAction.

            Stopwatch warmupWatch = new Stopwatch();
            warmupWatch.Start();

            PlayerColor = teamColor;
            int enemyColor = (teamColor == 0) ? 1 : 0;

            TBETSNode root = new TBETSNode(enemyColor, 0, null);
            root.State = map;
            
            treeManager.ClearUnexploitedNodes();
            FitnessUpdate(root);
            Exploit(root, STARTING_POP);

            // Run a few iterations to warm up the selection/expansion logic
            for (int i = 0; i < 2; i++)
            {
                if (treeManager.unexploitedNodes.Count == 0) break;
                TBETSNode selectedNode = treeManager.SelectNode(root, i, 10);
                if (selectedNode == null || selectedNode.IsLeaf) continue;
                if (!selectedNode.Explored) {
                    Exploit(selectedNode);
                }
            }
            
            // Clean up after warmup
            treeManager.ClearUnexploitedNodes();
            savedTree = null;
            
            warmupWatch.Stop();
            Logger.addLogMessage("TBETS: Warm-up finished in " + warmupWatch.ElapsedMilliseconds + "ms.\r\n", teamColor);
        }

        private void SeedRoot(TBETSNode root, long actionBudget)
        {

            Exploit(root, NUM_INIT);

            if (RHEA_INDIVIDUALS < 1)
            {
                return;
            }

            // run RHEA for a few iters.
            AI_RHEA rhea_agent = new AI_RHEA();
            // spend a third of the budget running RHEA to seed the root
            AI_RHEA.Individual[] RHEA_pop = rhea_agent.RunRHEA(root.State.createDeepClone(), PlayerColor, actionBudget / RHEA_BUDGET_FRACTION);
            Array.Sort(RHEA_pop, (a, b) => b.fitness.CompareTo(a.fitness));

            for (int i = 0; i < Math.Min(RHEA_INDIVIDUALS, RHEA_pop.Length); i++)
            {
                AI_RHEA.Individual individual = RHEA_pop[i];
                List<Action> actions = individual.actionSequence[0];


                int childColor = root.Color == 0 ? 1 : 0;
                TBETSNode childNode = new TBETSNode(childColor, root.Depth + 1, root);
                childNode.State = root.State.createDeepClone();

                List<Action> clonedActions = new List<Action>();
                foreach (Action act in actions)
                {
                    clonedActions.Add(act.createDeepClone());
                }
                // Generate random actions with attack bias
                childNode.setActions(clonedActions);

                // Apply actions to the state
                childNode.ApplyActions();
                childNode.EndTurn();

                // Add the child node to the tree
                AddNode(root, childNode);

            }
            root.Explored = true;
            treeManager.RemoveExploitedNode(root);
        }

        private void Exploit(TBETSNode selectedNode, int numChildren = STARTING_POP)
        {
            // First, ensure the node has a phantom child if it doesn't already have one
            TBETSNode phantomChild = null;

            // Look for existing phantom child
            foreach (TBETSNode child in selectedNode.Children)
            {
                if (child.IsPhantom && child.IsPlayerNode != selectedNode.IsPlayerNode)
                {
                    phantomChild = child;
                    break;
                }
            }

            if (!selectedNode.IsPrimary)
            {
                throw new Exception("AI_TBETS: Expected selectedNode to be a primary node.");
            }

            if (selectedNode.Explored)
            {
                selectedNode.PrintSelf();
                selectedNode.GetRoot().PrintRecursive();
                throw new Exception("AI_TBETS: Expected selectedNode to be unexplored.");
            }

            if (phantomChild == null)
            {
                throw new Exception("AI_TBETS: Expected phantom child to exist but none was found.");
            }
            phantomChild.IsPhantom = false;
            phantomChild.CalculateStateHash();
            FitnessUpdate(phantomChild);

            // List<TBETSNode> childrenToAdd = new List<TBETSNode>();

            // Generate the rest of the randomly initialized children
            for (int j = 0; j < numChildren - 1; j++)
            {
                // Create new child node
                int childColor = selectedNode.Color == 0 ? 1 : 0;
                TBETSNode childNode = new TBETSNode(childColor, selectedNode.Depth + 1, selectedNode);
                childNode.State = selectedNode.State.createDeepClone();

                // Generate random actions with attack bias
                childNode.setActions(treeManager.GenerateRandomActions(childNode));

                // Apply actions to the state
                childNode.ApplyActions();
                childNode.EndTurn();

                // Add the child node to the tree
                AddNode(selectedNode, childNode);
                // childrenToAdd.Add(childNode);
            }

            // // Update the node's fitness to be the lowest fitness among the children
            // if (selectedNode.Children.Count > 0)
            // {
            //     double lowestFitness = treeManager.GetLowestFitness(selectedNode.Children);
            //     treeManager.UpdateNodeFitness(selectedNode, lowestFitness);
            // }

            // Mark as explored
            selectedNode.Explored = true;
            treeManager.RemoveExploitedNode(selectedNode);

            // exploit should generate multiple children
            if (selectedNode.Children.Count == 1)
            {
                // foreach (TBETSNode child in childrenToAdd) {
                //     child.PrintSelf();
                // }
                // throw new Exception("AI_TBETS: Exploit should not result in a single child node. This indicates an issue with the child generation.");
            }
        }
        
        /// <summary>
        /// Add a node to the tree. This method handles checking for duplicates, adding it to children,
        /// calculating fitness, and adding to unexploited nodes.
        /// </summary>
        /// <param name="parent">Parent node</param>
        /// <param name="node">Node to add</param>
        private void AddNode(TBETSNode parent, TBETSNode node)
        {
            if (parent == null || node == null)
                throw new ArgumentNullException("Parent or node cannot be null.");
                
            // Check for identical action sequences - don't add if sequences match
            foreach (TBETSNode child in parent.Children)
            {
                if (child.HasSameActions(node))
                {
                    return;
                }
            }
        
            node.CalculateStateHash();
            
            // Check for state equivalence with existing nodes
            bool isDuplicate = treeManager.CheckForDuplicates(parent.Children, node);
            
            // Add the node as a child regardless of whether it's a duplicate
            parent.AddChild(node);

            // Only calculate fitness and add to unexploited nodes if it's a primary node
            if (node.IsPrimary)
            {
                FitnessUpdate(node);
                parent.incDescendents(); // counts as an actual descendent in the tree
            }

            if (node.Actions.Count == 0)
            {
                throw new Exception("AI_TBETS: Node with no actions should not be added.");
            }
        }

        private void FitnessUpdate(TBETSNode node) {
            treeManager.EvaluateFitness(node, PlayerColor);
            treeManager.PropagateFitness(node, PlayerColor);
            if (!node.IsLeaf) {
                treeManager.AddUnexploitedNode(node);
            }
        }
        
        /// <summary>
        /// Check if two maps represent equivalent game states.
        /// </summary>
        /// <param name="map1">First map</param>
        /// <param name="map2">Second map</param>
        /// <returns>True if maps are equivalent</returns>
        private bool MapsAreEquivalent(Map map1, Map map2)
        {
            if (map1 == null || map2 == null)
            {
                return false;
            }
            
            // Compare unit lists
            List<Unit> units1 = map1.getUnitsList(0, true, true, false);
            units1.AddRange(map1.getUnitsList(1, true, true, false));
            
            List<Unit> units2 = map2.getUnitsList(0, true, true, false);
            units2.AddRange(map2.getUnitsList(1, true, true, false));
            
            // Quick check - unit counts must match
            if (units1.Count != units2.Count)
            {
                return false;
            }
            
            // Sort units by ID for consistent comparison
            units1 = units1.OrderBy(u => u.getID()).ToList();
            units2 = units2.OrderBy(u => u.getID()).ToList();
            
            // Compare each unit
            for (int i = 0; i < units1.Count; i++)
            {
                Unit unit1 = units1[i];
                Unit unit2 = units2[i];
                
                if (unit1.getID() != unit2.getID() ||
                    unit1.getXpos() != unit2.getXpos() ||
                    unit1.getYpos() != unit2.getYpos() ||
                    unit1.getHP() != unit2.getHP() ||
                    unit1.getTeamColor() != unit2.getTeamColor() ||
                    unit1.getName() != unit2.getName())
                {
                    return false;
                }
            }
            
            // Compare turn count
            if (map1.getTurnCount() != map2.getTurnCount())
            {
                return false;
            }
            
            return true;
        }
    }
}
