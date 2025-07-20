using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleWars
{
    /// <summary>
    /// Represents a node in the TBETS search tree.
    /// Each node holds actions for a full turn and maintains state information.
    /// </summary>
    class TBETSNode
    {
        // Node properties
        public List<Action> Actions { get; set; }        // Actions for this turn
        public List<Action> TakenActions { get; set; }           // Actions that have been executed before this turn
        public Map State { get; set; }                           // Game state at this node AFTER actions are taken
        public double Fitness { get; set; }                      // Node fitness/evaluation
        public int Color { get; private set; }                   // Team color of this node
        public List<TBETSNode> Children { get; private set; }    // Child nodes
        public bool Explored { get; set; }                       // Whether this node has been explored
        public int Depth { get; private set; }                   // Depth in the tree
        public long StateHash { get; private set; }              // Hash code for state comparison
        public bool IsPrimary { get; set; } = true;              // Whether this is a primary node or a duplicate
        public TBETSNode PrimaryNode { get; set; }               // Reference to the primary node if this is a duplicate
        public List<TBETSNode> Duplicates { get; private set; }  // List of duplicate nodes for this primary node
        public bool IsPhantom { get; set; } = false;             // Whether this is a phantom node (for evaluation only)
        public TBETSNode Parent { get; private set; }            // Parent node reference
        public bool IsLeaf { get; set; }                         // Whether game is over after this state (no children)
        public TBETSNode Successor { get; set; }                 // Child with highest/lowest fitness
        public bool Deleted { get; set; } = false;               // debugging flag
        public int nDescendents { get; set; } = 0;               // Number of descendants for this node
        /// <summary>
        /// Creates a new TBETS node.
        /// </summary>
        /// <param name="color">Team color of this node</param>
        /// <param name="depth">The depth of this node in the tree</param>
        /// <param name="parent">The parent node of this node</param>
        public TBETSNode(int color, int depth, TBETSNode parent = null)
        {
            Actions = new List<Action>();
            TakenActions = new List<Action>();
            Fitness = -2.0; // fitness should be in range -1.0 to 1.0, initialize to -2.0 to indicate uninitialized state
            Color = color;
            Children = new List<TBETSNode>();
            Duplicates = new List<TBETSNode>();
            Explored = false;
            Depth = depth;
            StateHash = 0;
            Parent = parent;
            IsPhantom = false;
            IsPrimary = true;
            PrimaryNode = null;
            IsLeaf = false;
            Successor = null;
            if (parent != null) {
                parent.incDescendents();
            }
        }
        
        /// <summary>
        /// Determines if this node represents the player's turn.
        /// </summary>
        /// <param name="playerColor">The current player's color</param>
        /// <returns>True if this is a player node, false if enemy node</returns>
        public bool IsPlayerNode => Color == AI_TBETS.PlayerColor;

        public void incDescendents() {
            // Increment the number of descendants for this node and all its ancestors
            nDescendents++;
            if (Parent != null) {
                Parent.incDescendents();
            }
        }
        
        /// <summary>
        /// Adds an action to this node's action list.
        /// </summary>
        /// <param name="action">The action to add</param>
        public void AddAction(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "Cannot add a null action.");
            }
            Actions.Add(action);
        }

        public void AddTakenAction(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "Cannot add a null action.");
            }
            TakenActions.Add(action);
        }

        public List<Action> getActions()
        {
            return Actions;
        }

        public void setActions(List<Action> actions)
        {
            Actions = actions;
        }

        public void ApplyActions()
        {
            foreach (Action action in TakenActions)
            {
                if (!ActionChecker.isTheActionLegalMove_Silent(action, State)) {
                    Console.WriteLine(State.toString());
                    throw new InvalidOperationException($"Action {action.ToString()} is illegal in the current state.");
                }
                State.executeAction(action);
            }
            foreach (Action action in Actions)
            {
                if (!ActionChecker.isTheActionLegalMove_Silent(action, State)) {
                    throw new InvalidOperationException($"Action {action.ToString()} is illegal in the current state.");
                }
                State.executeAction(action);
            }
            State.enableUnitsAction(Color);
        }

        public void EndTurn() {
            State.enableUnitsAction(1-Color);
            State.incTurnCount();
        }
        
        /// <summary>
        /// Adds a child node to this node's children list.
        /// </summary>
        /// <param name="child">The child node to add</param>
        public void AddChild(TBETSNode child)
        {
            Children.Add(child);
        }
        
        // Static Zobrist hash table
        private static readonly Dictionary<string, long> ZobristTable = InitializeZobristTable();

        /// <summary>
        /// Initializes the Zobrist hash table with random values
        /// </summary>
        private static Dictionary<string, long> InitializeZobristTable()
        {
            // Create a dictionary to store random values for each possible state element
            var table = new Dictionary<string, long>();
            var random = new Random(42); // Fixed seed for consistency
            
            // Initialize values for each possible unit property combination
            // Based on game constraints: X/Y max 10, HP max 10, team max 2, etc.
            
            // For each possible unit position (X, Y)
            for (int id = 0; id < 100; id++) // Assuming max 100 unit IDs
            {
                for (int x = 0; x < 20; x++) // X position (over-allocate for safety)
                {
                    for (int y = 0; y < 20; y++) // Y position (over-allocate for safety)
                    {
                        for (int hp = 0; hp < 15; hp++) // HP (over-allocate for safety)
                        {
                            for (int team = 0; team < 3; team++) // Team color (over-allocate for safety)
                            {
                                string key = $"Unit_{id}_{x}_{y}_{hp}_{team}";
                                table[key] = GetRandomLong(random);
                            }
                        }
                    }
                }
                
                // Also create unique values for unit names
                // We'll just hash each potential unit name's GetHashCode
                string nameKey = $"UnitName_{id}";
                table[nameKey] = GetRandomLong(random);
            }
            
            // Values for turn count
            for (int turn = 0; turn < 100; turn++) // Assuming max 100 turns
            {
                string key = $"Turn_{turn}";
                table[key] = GetRandomLong(random);
            }
            
            return table;
        }
        
        /// <summary>
        /// Helper to generate a random long value
        /// </summary>
        private static long GetRandomLong(Random random)
        {
            // Generate a random long by combining two ints
            byte[] buffer = new byte[8];
            random.NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }
        
        /// <summary>
        /// Calculates a hash code for the state using Zobrist hashing.
        /// This method provides excellent collision resistance for game states.
        /// </summary>
        public void CalculateStateHash()
        {     
            if (StateHash != 0)
            {
                // Hash already calculated, no need to recalculate
                return;
            }
            
            // Start with 0 and XOR with values from the Zobrist table
            long hash = 0;
            
            // Include turn count in the hash
            string turnKey = $"Turn_{State.getTurnCount()}";
            if (ZobristTable.ContainsKey(turnKey))
            {
                hash ^= ZobristTable[turnKey];
            }
            
            // Get all units and sort them for consistency
            List<Unit> allUnits = State.getUnitsList(0, true, true, false);
            allUnits.AddRange(State.getUnitsList(1, true, true, false));
            allUnits = allUnits.OrderBy(u => u.getID()).ToList();
            
            foreach (Unit unit in allUnits)
            {
                // Hash unit position, HP and team color
                string unitKey = $"Unit_{unit.getID()}_{unit.getXpos()}_{unit.getYpos()}_{unit.getHP()}_{unit.getTeamColor()}";
                if (ZobristTable.ContainsKey(unitKey))
                {
                    hash ^= ZobristTable[unitKey];
                }
                
                // Hash unit name - we use the name's hash code to avoid storing every possible name
                string nameKey = $"UnitName_{unit.getID()}";
                if (ZobristTable.ContainsKey(nameKey))
                {
                    // XOR with unit name hash to make it unique
                    hash ^= ZobristTable[nameKey] ^ (long)unit.getName().GetHashCode();
                }
            }
            
            StateHash = hash;
        }
        
        /// <summary>
        /// Checks if two nodes represent the same state.
        /// </summary>
        /// <param name="other">The other node to compare with</param>
        /// <returns>True if the states are the same, false otherwise</returns>
        public bool IsSameState(TBETSNode other)
        {
            if (other == null || this.State == null || other.State == null)
            {
                return false;
            }

            if (this.StateHash == 0 || other.StateHash == 0)
            {
                throw new InvalidOperationException("StateHash must be calculated before comparing states.");
            }

            long tempHash1 = this.StateHash;
            CalculateStateHash();
            long tempHash2 = other.StateHash;
            other.CalculateStateHash();
            if (tempHash1 != this.StateHash || other.StateHash != tempHash2)
            {
                throw new InvalidOperationException("StateHash must be consistent after calculation.");
            }
            
            // Quick check with hash codes
            if (this.StateHash != other.StateHash)
            {
                return false;
            }
            
            // Deep comparison of unit states
            List<Unit> thisUnits = this.State.getUnitsList(0, true, true, false);
            thisUnits.AddRange(this.State.getUnitsList(1, true, true, false));
            
            List<Unit> otherUnits = other.State.getUnitsList(0, true, true, false);
            otherUnits.AddRange(other.State.getUnitsList(1, true, true, false));
            
            // Quick check - unit counts must match
            if (thisUnits.Count != otherUnits.Count)
            {
                throw new InvalidOperationException("Unit counts must match for the same state.");
                return false;
            }
            
            // Sort units by ID for consistent comparison
            thisUnits = thisUnits.OrderBy(u => u.getID()).ToList();
            otherUnits = otherUnits.OrderBy(u => u.getID()).ToList();
            
            // Compare each unit
            for (int i = 0; i < thisUnits.Count; i++)
            {
                Unit thisUnit = thisUnits[i];
                Unit otherUnit = otherUnits[i];
                
                if (thisUnit.getID() != otherUnit.getID() ||
                    thisUnit.getXpos() != otherUnit.getXpos() ||
                    thisUnit.getYpos() != otherUnit.getYpos() ||
                    thisUnit.getHP() != otherUnit.getHP() ||
                    thisUnit.getTeamColor() != otherUnit.getTeamColor() ||
                    thisUnit.getName() != otherUnit.getName())
                {
                    // GetRoot().PrintRecursive();
                    Console.WriteLine("Mismatch found in units:");
                    PrintSelf();
                    other.PrintSelf();
                    Console.WriteLine("Hash 1: " + this.StateHash);
                    Console.WriteLine("Hash 2: " + other.StateHash);
                    throw new InvalidOperationException("Unit properties must match for the same state.");
                    return false;
                }
            }
            
            // Compare turn count
            if (this.State.getTurnCount() != other.State.getTurnCount())
            {
                throw new InvalidOperationException("Turn counts must match for the same state.");
                return false;
            }
            
            return true;
        }

        public TBETSNode GetRoot()
        {
            TBETSNode current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current;
        }
        
        /// <summary>
        /// Adds this node as a duplicate of the specified primary node.
        /// </summary>
        /// <param name="primaryNode">The primary node that this node duplicates</param>
        public void MarkAsDuplicateOf(TBETSNode primaryNode)
        {
            if (primaryNode == null)
                throw new ArgumentNullException(nameof(primaryNode), "Cannot mark as duplicate of a null node.");
            
            this.IsPrimary = false;
            this.PrimaryNode = primaryNode;
            this.Fitness = primaryNode.Fitness; // Sync fitness with primary
            primaryNode.AddDuplicate(this);
        }
        
        /// <summary>
        /// Adds a duplicate node to this primary node's list of duplicates.
        /// </summary>
        /// <param name="duplicate">The duplicate node to add</param>
        public void AddDuplicate(TBETSNode duplicate)
        {
            if (duplicate != null && !Duplicates.Contains(duplicate))
            {
                Duplicates.Add(duplicate);
            }
        }

        public void RemoveDuplicate(TBETSNode duplicate)
        {
            if (!Duplicates.Contains(duplicate))
            {
                throw new InvalidOperationException("Cannot remove a duplicate node that does not exist in the list.");
            }
            Duplicates.Remove(duplicate);
        }

        public void FitnessUpdate(int PlayerColor) {
            if (IsPhantom || IsLeaf || !IsPrimary) {
                // Don't check fitness for phantom or leaf nodes
                return;
            }
            double newFitness = Color == PlayerColor ? double.MaxValue : double.MinValue;
            bool unexploited = false;
            foreach (TBETSNode childNode in Children)
            {
                if (childNode.Fitness == -2.0) {
                    unexploited = true;
                    break;
                }
                if (Color == PlayerColor && childNode.Fitness < newFitness ||
                    Color != PlayerColor && childNode.Fitness > newFitness)
                {
                    newFitness = childNode.Fitness;
                }
            }
            if (unexploited) {
                if (Explored) {
                    throw new InvalidOperationException($"Fitness update failed at node: {ToString()}.\n" +
                        $"This node is explored but has unevaluated children.");
                }
                return;
            }
            Fitness = newFitness;
        }
        public void FitnessIntegrityCheck(int PlayerColor) {
            if (IsPhantom || IsLeaf || !IsPrimary) {
                // Don't check fitness for phantom or leaf nodes
                return;
            }
            double score = Color == PlayerColor ? double.MaxValue : double.MinValue;
            bool unexploited = false;
            foreach (TBETSNode childNode in Children)
            {
                if (childNode.Fitness == -2.0) {
                    unexploited = true;
                    break;
                }
                if (Color == PlayerColor && childNode.Fitness < score ||
                    Color != PlayerColor && childNode.Fitness > score)
                {
                    score = childNode.Fitness;
                }
            }
            if (unexploited) {
                if (Explored) {
                    throw new InvalidOperationException($"Fitness integrity check failed at node: {ToString()}.\n" +
                        $"This node is explored but has unevaluated children.");
                }
                return;
            }
            if (Fitness != score) {
                GetRoot().PrintRecursive();
                throw new InvalidOperationException($"Fitness integrity check failed at node: {ToString()}.\n" +
                    $"Expected fitness: {score}, Actual fitness: {Fitness}. " +
                    $"This may indicate an issue with the fitness propagation logic.");
            }
            foreach (TBETSNode childNode in Children)
            {
                childNode.FitnessIntegrityCheck(PlayerColor);
            }
        }

        public void FitnessCheck(TBETSNode child, int PlayerColor) {
            // Console.WriteLine($"Fitness Update [{this.ToString()}] -> \r\n[Child: {child.ToString()}]");
            // minimax update to determine if fitness should be updated based on the child node's fitness
            if (Color == PlayerColor && child.Fitness < this.Fitness ||
                Color != PlayerColor && child.Fitness > this.Fitness)
            {
                Successor = child;
                UpdateFitnessWithDuplicates(child.Fitness);
                if (Parent != null)
                {
                    Parent.FitnessCheck(this, PlayerColor);
                }
            }
            else if (Successor == child) {
                // run through all children and recompute fitness
                // set initial fitness to max or min double
                double oldFitness = Fitness;
                Fitness = Color == PlayerColor ? double.MaxValue : double.MinValue;
                foreach (TBETSNode childNode in Children)
                {
                    if (Color == PlayerColor && childNode.Fitness < Fitness ||
                        Color != PlayerColor && childNode.Fitness > Fitness)
                    {
                        Fitness = childNode.Fitness;
                        Successor = childNode;
                    }
                }
                if (Fitness != oldFitness) {
                    UpdateFitnessWithDuplicates(Fitness);
                    if (Parent != null)
                    {
                        Parent.FitnessCheck(this, PlayerColor);
                    }
                }
            }
        }
        
        /// <summary>
        /// Updates the fitness of this node and all its duplicates.
        /// </summary>
        /// <param name="newFitness">The new fitness value</param>
        public void UpdateFitnessWithDuplicates(double newFitness)
        {
            this.Fitness = newFitness;
            
            // Update all duplicates
            foreach (TBETSNode duplicate in Duplicates)
            {
                duplicate.Fitness = newFitness;
            }
        }
        
        /// <summary>
        /// Checks if two action sequences are identical.
        /// </summary>
        /// <param name="other">The other node to compare actions with</param>
        /// <returns>True if the action sequences are identical, false otherwise</returns>
        public bool HasSameActions(TBETSNode other)
        {
            if (other == null || this.Actions.Count != other.Actions.Count)
            {
                return false;
            }
            
            for (int i = 0; i < this.Actions.Count; i++)
            {
                Action thisAction = this.Actions[i];
                Action otherAction = other.Actions[i];
                
                if (thisAction.actionType != otherAction.actionType ||
                    thisAction.operationUnitId != otherAction.operationUnitId ||
                    thisAction.destinationXpos != otherAction.destinationXpos ||
                    thisAction.destinationYpos != otherAction.destinationYpos ||
                    (thisAction.actionType == Action.ACTIONTYPE_MOVEANDATTACK &&
                     thisAction.targetUnitId != otherAction.targetUnitId))
                {
                    return false;
                }
            }
            
            return true;
        }

        public bool HasSameTakenActions(TBETSNode other)
        {
            if (other == null || this.TakenActions.Count != other.TakenActions.Count)
            {
                return false;
            }
            
            for (int i = 0; i < this.TakenActions.Count; i++)
            {
                Action thisAction = this.TakenActions[i];
                Action otherAction = other.TakenActions[i];
                
                if (thisAction.actionType != otherAction.actionType ||
                    thisAction.operationUnitId != otherAction.operationUnitId ||
                    thisAction.destinationXpos != otherAction.destinationXpos ||
                    thisAction.destinationYpos != otherAction.destinationYpos ||
                    (thisAction.actionType == Action.ACTIONTYPE_MOVEANDATTACK &&
                     thisAction.targetUnitId != otherAction.targetUnitId))
                {
                    return false;
                }
            }
            
            return true;
        }

        public bool CheckLeaf() {
            if (State.getNumOfAliveColorUnits(Consts.RED_TEAM) == 0 ||
                State.getNumOfAliveColorUnits(Consts.BLUE_TEAM) == 0 ||
                State.getTurnCount() == State.getTurnLimit()) {
                // Game over, no more actions possible
                IsLeaf = true;
                Explored = true;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Returns a short string representation of this node.
        /// </summary>
        /// <returns>A string with node info</returns>
        public override string ToString()
        {
            return $"Node[Color={Color}, Depth={Depth}, Actions={Actions.Count}, Fitness={Fitness:F3}, Children={Children.Count}, Duplicates={Duplicates.Count}, Explored={Explored}, Primary={IsPrimary}, Phantom={IsPhantom}, Leaf={IsLeaf}, Hash={StateHash} {(this.Deleted ? "DELETED" : "")}]";
        }

        public void PrintRecursive()
        {
            Console.WriteLine($"{new string(' ', Depth * 2)}{ToString()}");

            foreach (TBETSNode child in this.Children)
            {
                child.PrintRecursive();
            }
        }

        public string StringRecursive() {
            
            string result = $"{new string(' ', Depth * 2)}{ToString()}\n";
            
            foreach (TBETSNode child in this.Children)
            {
                result += child.StringRecursive();
            }
            
            return result;
        }

        public void PrintSelf() {
            Console.WriteLine(ToString());
            Console.WriteLine("Taken: ");
            foreach (Action action in TakenActions)
            {
                Console.WriteLine($"{new string(' ', (this.Depth + 1) * 2)}Action: {action.ToString()}");
            }
            Console.WriteLine("Actions: ");
            foreach (Action action in Actions)
            {
                Console.WriteLine($"{new string(' ', (this.Depth + 1) * 2)}Action: {action.ToString()}");
            }
        }
        public string PrintChildren()
        {
            // build a string
            string result = "";

            foreach (TBETSNode child in this.Children)
            {
                result += $"{new string(' ', this.Depth * 2)}{child.ToString()}\n";
                result += $"{new string(' ', (this.Depth + 1) * 2)}Taken: \n";
                foreach (Action action in child.TakenActions)
                {
                    result += $"{new string(' ', (this.Depth + 1) * 2)}Action: {action.ToString()}\n";
                }
                result += $"{new string(' ', (this.Depth + 1) * 2)}Actions: \n";

                foreach (Action action in child.Actions)
                {
                    result += $"{new string(' ', (this.Depth + 1) * 2)}Action: {action.ToString()}\n";
                }
            }
            Console.WriteLine(result);
            return result;
        }
    }
}
