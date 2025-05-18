using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleWars
{
    /// <summary>
    /// Manages duplicate node detection and relationships for the TBETS algorithm.
    /// </summary>
    class TBETSDuplicateManager
    {

        
        /// <summary>
        /// Determines if a node is a duplicate of any node in the provided list.
        /// If it is a duplicate, establishes the primary/duplicate relationship.
        /// </summary>
        /// <param name="existingNodes">List of existing nodes to check against</param>
        /// <param name="newNode">New node to check for duplicates</param>
        /// <returns>True if the node is a duplicate and has been marked, false otherwise</returns>
        public bool HandleDuplicateNode(List<TBETSNode> existingNodes, TBETSNode newNode)
        {
            if (newNode == null || existingNodes == null)
                throw new ArgumentNullException("HandleDuplicateNode: existingNodes or newNode cannot be null");
                
            TBETSNode primary = null;
            foreach (TBETSNode existingNode in existingNodes)
            {
                if (newNode.IsSameState(existingNode))
                {
                    primary = FindPrimary(existingNode);
                }
            }

            if (primary != null) {
                newNode.MarkAsDuplicateOf(primary);
                return true;
            }
            
            // If no duplicates found, the node is primary
            newNode.IsPrimary = true;
            return false;
        }

        public TBETSNode FindPrimary(TBETSNode node)
        {
            TBETSNode current = node;
            while (current != null && !current.IsPrimary)
            {
                current = current.PrimaryNode;
            }
            return current;
        }

        
        /// <summary>
        /// Updates the fitness of a node and propagates the change to all its duplicates.
        /// </summary>
        /// <param name="node">The node whose fitness is being updated</param>
        /// <param name="newFitness">The new fitness value</param>
        public void UpdateNodeFitness(TBETSNode node, int PlayerColor)
        {
            if (node == null)
                return;
                
            // If this is a primary node, update it and all duplicates
            if (node.IsPrimary)
            {
                // Console.WriteLine(node);
                node.UpdateFitnessWithDuplicates(node.Fitness);
                if (node.Parent != null)
                {
                    node.Parent.FitnessCheck(node, PlayerColor);
                }
            }
            // If this is a duplicate, find its primary and update that instead
            else
            {
                throw new InvalidOperationException("Cannot update fitness of a duplicate node directly. ");
            }
        }
        
        /// <summary>
        /// Ensures that primary node relationships are maintained when adding nodes
        /// to the exploitable nodes list.
        /// </summary>
        /// <param name="node">The node to potentially add to unexploited list</param>
        /// <returns>True if the node should be added to unexploited list, false otherwise</returns>
        public bool ShouldAddToExploitableNodes(TBETSNode node)
        {
            // Only add primary nodes to the unexploited list, not duplicates
            return node != null && node.IsPrimary;
        }
    }
}
