using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleWars
{
    /// <summary>
    /// Handles node selection for exploration and exploitation in the TBETS algorithm.
    /// </summary>
    class TBETSNodeSelector
    {
        private readonly Random rnd;
        
        /// <summary>
        /// Creates a new node selector.
        /// </summary>
        /// <param name="random">Random number generator</param>
        public TBETSNodeSelector(Random random)
        {
            rnd = random ?? new Random();
        }
        
        /// <summary>
        /// Check if there are any explored nodes in the tree.
        /// </summary>
        /// <param name="root">Root node of the tree</param>
        /// <returns>True if there are any explored nodes, false otherwise</returns>
        public bool HasExploredNodes(TBETSNode root)
        {
            if (root.Explored && root.Children.Count > 0)
            {
                return true;
            }
            
            foreach (TBETSNode child in root.Children)
            {
                if (HasExploredNodes(child))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Select an explored node, biasing towards nodes closer to the root and nodes with higher fitness.
        /// </summary>
        /// <param name="root">Root node of the tree</param>
        /// <returns>Selected explored node</returns>
        public TBETSNode SelectExploredNode(TBETSNode root, int iter, int n_iters)
        {
            List<TBETSNode> candidates = new List<TBETSNode>();
            GetExploredNodes(root, candidates);
            
            if (candidates.Count == 0)
            {
                return root;
            }
            
            // Sort by depth (ascending) and fitness (descending)
            candidates.Sort((a, b) => {
                int depthComparison = a.Depth.CompareTo(b.Depth);
                if (depthComparison != 0)
                {
                    return depthComparison;
                }
                return b.Fitness.CompareTo(a.Fitness);
            });
            
            // Select one of the top nodes with some randomness
            // Use a squared random to bias towards the top of the list
            // int index = (int)(rnd.NextDouble() * rnd.NextDouble() * candidates.Count);
            // Use UCB to select a an index
            // TODO
            int index = UCB(candidates, iter, n_iters);

            return candidates[index];
        }

        private int UCB(List<TBETSNode> candidates, int iter, int n_iters)
        {
            double C = 1.414; // Exploration parameter (sqrt(2) is common)
            double depthBias =  .4; // Bias towards nodes closer to the root
            double bestScore = double.MinValue;
            int bestIndex = 0;
            
            double totalVisits = iter; // Current iteration count as total visits
            
            // Calculate a UCB score for each candidate node
            for (int i = 0; i < candidates.Count; i++)
            {
                TBETSNode node = candidates[i];
                
                // If node hasn't been visited, return it immediately
                if (node.nDescendents == 0)
                    return i;

                double depthPenalty = depthBias * node.Depth;
                    
                // Calculate UCB score
                // UCB = value + C * sqrt(ln(totalVisits) / nodeVisits)
                double exploitationTerm = node.Fitness;
                double explorationTerm = C * Math.Sqrt(Math.Log(totalVisits) / node.nDescendents);
                double ucbScore = exploitationTerm + explorationTerm - depthPenalty;
                
                // Track the best score
                if (ucbScore > bestScore)
                {
                    bestScore = ucbScore;
                    bestIndex = i;
                }
            }
            
            return bestIndex;
        }
        
        /// <summary>
        /// Helper to get all explored nodes.
        /// </summary>
        /// <param name="node">Root node to search from</param>
        /// <param name="exploredNodes">List to populate with explored nodes</param>
        private void GetExploredNodes(TBETSNode node, List<TBETSNode> exploredNodes)
        {
            if (node.Explored && node.Children.Count > 0)
            {
                exploredNodes.Add(node);
            }
            
            foreach (TBETSNode child in node.Children)
            {
                GetExploredNodes(child, exploredNodes);
            }
        }

        /// <summary>
        /// Select a high fitness node from a list.
        /// </summary>
        /// <param name="nodes">List of nodes to select from</param>
        /// <param name="PlayerColor">The TBETS agent's color</param>
        /// <param name="exclude">Optional node to exclude</param>
        /// <returns>Selected high fitness node</returns>
        public TBETSNode SelectHighFitnessChild(TBETSNode parent, int PlayerColor, int iter, int n_iters, TBETSNode exclude = null)
        {
            List<TBETSNode> nodes = parent.Children;
            bool isPlayerNode = parent.Color != PlayerColor;
            if (nodes == null || nodes.Count == 0)
            {
                throw new ArgumentException("TBETSNodeSelector: Cannot select from an empty or null list of nodes.");
            }
            
            // Create a copy of the list excluding the node to exclude
            List<TBETSNode> candidates = new List<TBETSNode>();
            foreach (TBETSNode node in nodes)
            {
                if (exclude == null || node != exclude)
                {
                    candidates.Add(node);
                }
            }
            
            if (candidates.Count == 0)
            {
                parent.PrintRecursive();
                throw new ArgumentException("TBETSNodeSelector: Sole node in list excluded.");
            }
            
            // Sort by fitness
            if (isPlayerNode)
            {
                // For player nodes, high fitness is good
                candidates.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
            }
            else
            {
                // For enemy nodes, low fitness is good (from player's perspective)
                candidates.Sort((a, b) => a.Fitness.CompareTo(b.Fitness));
            }
            
            // Tournament selection
            // int index = tournamentSelection(candidates);

            // UCB
            int index = UCB(candidates, iter, n_iters);

            // E-Greedy
            // int index = eGreedy(candidates, turn, nTurns, alpha);
            
            return candidates[index];
        }

    public int tournamentSelection(List<TBETSNode> candidates)
    {
        int tournamentSize = Math.Max(3, candidates.Count/2);
        int index = candidates.Count+1;
        TBETSNode best = null;
        
        for (int i = 0; i < tournamentSize; i++)
        {
            int randomIndex = rnd.Next(candidates.Count);
            TBETSNode candidate = candidates[randomIndex];
            // if (randomIndex < index || (rnd.NextDouble() < 0.7 && best != null && best.Depth < candidate.Depth))
            if (randomIndex < index)
            {
                index = randomIndex;
                best = candidate;
            }
        }
        return index;
    }

    private double epsilon = .8;
    public int eGreedy(List<TBETSNode> candidates, int currentTurn, int totalTurns, double alpha)
    {
        // Calculate the current epsilon using the annealing schedule
        // As currentTurn approaches totalTurns, epsilon will approach 0
        double currentEpsilon = epsilon * Math.Pow(1.0 - (double)currentTurn / totalTurns, alpha);
        
        // With probability (1-epsilon), exploit by choosing the best candidate (index 0)
        // With probability epsilon, explore by choosing a random candidate
        if (rnd.NextDouble() > currentEpsilon)
        {
            // Exploit: choose the best candidate (already sorted)
            return 0;
        }
        else
        {
            // Explore: choose a random candidate
            return rnd.Next(candidates.Count);
        }
    }
        
    /// <summary>
    /// Get the best player node from a list (node with highest fitness).
    /// Prioritizes primary nodes over duplicates when fitness is equal.
    /// Used for selecting next move, so does not return an unexplored node
    /// </summary>
    /// <param name="nodes">List of nodes</param>
    /// <returns>Node with highest fitness</returns>
    public TBETSNode GetBestPlayerNode(TBETSNode root)
    {
        List<TBETSNode> nodes = root.Children;
        if (nodes == null || nodes.Count == 0)
        {
            throw new ArgumentException("TBETSNodeSelector: Cannot get best player node from an empty or null list of nodes.");
        }
        
        TBETSNode bestNode = null;
        
        for (int i = 0; i < nodes.Count; i++)
        {
            // If fitness is higher, or equal but this is a primary node and best is not
            // only considers explored nodes
            if (nodes[i].Explored && (bestNode == null || (nodes[i].Fitness > bestNode.Fitness || 
                (nodes[i].Fitness == bestNode.Fitness && nodes[i].IsPrimary && !bestNode.IsPrimary))))
            {
                bestNode = nodes[i];
            }
        }

        if (bestNode == null)
        {
            return null;
        }
        
        // If the best node found is a duplicate, check if there's a primary node with the same fitness
        if (!bestNode.IsPrimary)
        {
            root.PrintRecursive();
            root.PrintChildren();
            throw new Exception("TBETSNodeSelector: Best node should always be primary but a duplicate was found instead.");
        }
        
        return bestNode;
    }
        
        /// <summary>
        /// Get the lowest fitness among a list of nodes.
        /// </summary>
        /// <param name="nodes">List of nodes</param>
        /// <returns>Lowest fitness value</returns>
        public double GetLowestFitness(List<TBETSNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return 0.0;
            }
            
            double lowestFitness = nodes[0].Fitness;
            
            for (int i = 1; i < nodes.Count; i++)
            {
                if (nodes[i].Fitness < lowestFitness)
                {
                    lowestFitness = nodes[i].Fitness;
                }
            }
            
            return lowestFitness;
        }
    }
}
