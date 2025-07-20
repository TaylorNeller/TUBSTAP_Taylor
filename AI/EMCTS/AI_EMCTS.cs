using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SimpleWars
{
    /// <summary>
    /// Evolutionary Monte‑Carlo Tree Search agent as described in
    /// "Evolutionary MCTS for Multi‑Action Adversarial Games" (Baier & Cowling, CIG‑2018).
    ///
    /// The agent searches in the space of complete **turn sequences** (genomes)
    /// rather than individual atomic actions.  Each edge in the search tree is a
    /// single *mutation* of the parent genome.  Nodes therefore represent fully
    /// specified turns and store the game state that would result from executing
    /// that turn.  No playouts/roll‑outs are required – a static heuristic is
    /// used to evaluate the leaf state (same heuristic the baseline agents use).
    ///
    /// The implementation follows the *bridge‑burning* variant from the paper:
    ///   1.  Search is split into a number of short **phases** (PHASE_COUNT).
    ///   2.  At the end of each phase we keep only the best child of the root
    ///       ("burn bridges"), treat it as the new root, and continue searching.
    ///   3.  After all phases the genome stored in the (current) root is the
    ///       algorithm's best estimate for the full turn; the first still‑legal
    ///       action of that genome is returned to the game engine.
    ///
    /// A greedy helper is used for
    ///   • root initialisation (kick‑start with a reasonable solution)
    ///   • repairing genomes that became illegal after a mutation.
    ///
    /// NOTE  This class has no external dependencies other than the public API
    ///       of SimpleWars.  It is self‑contained and can be dropped into the
    ///       project next to AI_M_UCT and AI_RHEA.
    /// </summary>
    class AI_EMCTS : Player
    {
        /* ------------------------------------------------------------------
         * ░░  Public meta‑information
         * ---------------------------------------------------------------- */
        public string getName() => "EMCTS";

        public string showParameters() =>
            $"TIME/turn = {LIMIT_TIME}ms\r\n" +
            $"PHASE_COUNT = {PHASE_COUNT}\r\n" +
            $"MAX_CHILDREN = {MAX_CHILDREN}\r\n" +
            $"UCB_C = {UCB_C:0.###}";

        /* ------------------------------------------------------------------
         * ░░  Search parameters – tweak to taste
         * ---------------------------------------------------------------- */
        // hard CPU budget identical to the other AIs
        private const long LIMIT_TIME = AI_Consts.LIMIT_TIME;           // ms per turn
        private const int PHASE_COUNT = 30;               // bridge‑burning phases
        private const double UCB_C = 0.15;                // exploration constant

        // to avoid generating *all* possible mutations we cap the fan‑out
        private const int MAX_CHILDREN = 120;             // ~5× larger than avg. branching factor of MCTS baseline

        /* ------------------------------------------------------------------
         * ░░  Internal state
         * ---------------------------------------------------------------- */
        private readonly Random rnd = new Random();
        private readonly Stopwatch stopwatch = new Stopwatch();
        private static long timeLeft;                     // remaining budget within current turn

        // shift‑buffer: best genome from the previous call (if we stay in the same turn)
        private List<Action> cachedGenome = null;

        // keep track of how deep the greedy initialisation had to go (for unit tests / debug)
        private int greedyInitLength;

        /* ------------------------------------------------------------------
         * ░░  Core search data structures
         * ---------------------------------------------------------------- */
        /// <summary>Represents a **complete turn** (genome).</summary>
        private class Genome
        {
            public readonly List<Action> Actions;
            public Genome(IEnumerable<Action> actions) => Actions = new List<Action>(actions);

            public Genome DeepClone()
            {
                var clone = new List<Action>();
                foreach (var a in Actions)
                    clone.Add(a.createDeepClone());
                return new Genome(clone);
            }
        }

        /// <summary>Tree node for EMCTS (stores genome + search statistics).</summary>
        private class Node
        {
            public Genome Genome;                  // full turn represented by this node
            public Map State;                      // state after executing Genome

            public readonly List<Node> Children = new List<Node>();
            public int Visits;
            public double TotalValue;              // accumulated heuristic value

            public double MeanValue => Visits == 0 ? 0.0 : TotalValue / Visits;
        }

        /* ==================================================================
         * ░░  Public entry – called once for every action we must output
         * ================================================================= */
        public Action makeAction(Map map, int teamColor, bool turnStart, bool gameStart)
        {
            /* Turn bookkeeping -------------------------------------------------- */
            if (turnStart)
            {
                // reset per‑turn state
                timeLeft = LIMIT_TIME;
                cachedGenome = null;
            }

            // If we already have a computed sequence from the previous
            // call within the *same* turn, simply pop the head action.
            if (cachedGenome != null && cachedGenome.Count > 0)
            {
                var next = PopFirstValidAction(cachedGenome, map, teamColor);
                if (next != null)
                    return next;
            }

            /* Phase 0 – root initialisation ------------------------------------- */
            stopwatch.Restart();

            // 1) Build greedy baseline solution (root genome)
            var rootGenome = BuildGreedyGenome(map, teamColor);
            var rootNode = new Node
            {
                Genome = rootGenome,
                State = ApplyGenome(map, rootGenome),
                Visits = 0,
                TotalValue = 0
            };

            // 2) Evaluate immediately – leaf evaluation (no roll‑outs)
            var rootValue = EvaluateState(rootNode.State, teamColor);
            rootNode.Visits = 1;
            rootNode.TotalValue = rootValue;


            long phaseBudget = timeLeft / PHASE_COUNT; // budget per phase
            /* Phase 1..N – EMCTS with bridge‑burning ---------------------------- */
            for (int phase = 0; phase < PHASE_COUNT; phase++)
            {
                // stop if we burned too much time (rough allocation per remaining actions)
                if (stopwatch.ElapsedMilliseconds >= timeLeft)
                    break;

                PerformSearchPhase(rootNode, map, teamColor, phaseBudget);

                // bridge‑burn: keep only best child, make it the new root
                if (rootNode.Children.Count == 0) break; // could not expand – e.g. only one legal mutation

                Node bestChild = rootNode.Children.OrderByDescending(c => c.MeanValue).First();
                rootNode = bestChild; // continue search from here
            }

            stopwatch.Stop();
            timeLeft -= stopwatch.ElapsedMilliseconds;

            /* Phase end – output ------------------------------------------------- */
            cachedGenome = new List<Action>(rootNode.Genome.Actions);
            var chosen = PopFirstValidAction(cachedGenome, map, teamColor);
            return chosen ?? Action.createTurnEndAction();
        }

        /* ==================================================================
         * ░░  EMCTS search helpers
         * ================================================================= */
        /// <summary>Perform one MCTS‑style phase starting from <paramref name="root"/>.</summary>
        private void PerformSearchPhase(Node root, Map baseMap, int teamColor, long phaseBudget)
        {
            var phaseWatch = Stopwatch.StartNew();

            // repeated simulations until out of budget
            while (phaseWatch.ElapsedMilliseconds < phaseBudget)
            {
                var path = new Stack<Node>();
                Node node = root;
                path.Push(node);

                /* Selection ---------------------------------------------------- */
                while (node.Children.Count > 0 && !NeedsExpansion(node))
                {
                    node = SelectChildUCB(node);
                    path.Push(node);
                }

                /* Expansion ---------------------------------------------------- */
                if (NeedsExpansion(node) && node.Visits > 0)
                {
                    var child = Expand(node, baseMap, teamColor);
                    if (child != null)
                    {
                        node = child;
                        path.Push(node);
                    }
                }

                /* Evaluation (leaf) ------------------------------------------- */
                double value;
                if (node.Visits == 0)
                {
                    value = EvaluateState(node.State, teamColor);
                    node.Visits = 1;
                    node.TotalValue = value;
                }
                else
                {
                    value = node.MeanValue; // already evaluated earlier
                }

                /* Back‑prop ---------------------------------------------------- */
                foreach (var n in path)
                {
                    n.Visits += 1;
                    n.TotalValue += value;
                }
            }

            phaseWatch.Stop();
        }

        /// <summary>True if *some* untried mutation is still available.</summary>
        private bool NeedsExpansion(Node node) => node.Children.Count < MAX_CHILDREN;

        /// <summary>UCB1 child selection.</summary>
        private Node SelectChildUCB(Node parent)
        {
            double logParent = Math.Log(parent.Visits);
            double bestScore = double.NegativeInfinity;
            Node best = null;

            foreach (var child in parent.Children)
            {
                double ucb = child.MeanValue + UCB_C * Math.Sqrt(logParent / child.Visits);
                if (ucb > bestScore)
                {
                    bestScore = ucb;
                    best = child;
                }
            }
            return best;
        }

        /// <summary>Generate *one* child by mutating <paramref name="parent"/>.</summary>
        private Node Expand(Node parent, Map baseMap, int teamColor)
        {
            // defensive cap; also protects against super‑thin genomes
            if (parent.Genome.Actions.Count == 0)
                return null;

            // 1) choose gene index to mutate
            int index = rnd.Next(parent.Genome.Actions.Count);

            // 2) recreate game state *after* executing genes [0 .. index‑1]
            Map prefixState = baseMap.createDeepClone();
            for (int i = 0; i < index; i++)
                prefixState.executeAction(parent.Genome.Actions[i]);

            // unit might be dead, map changed .. if so bail out (no feasible mutation)
            Unit opUnit = prefixState.getUnit(parent.Genome.Actions[index].operationUnitId);
            if (opUnit == null || opUnit.isActionFinished())
                return null; // Very unlikely – but guards against null refs.

            // 3) generate *one* random alternative action for that unit
            var legal = M_Tools.getUnitActions(opUnit, prefixState);
            if (legal.Count == 0) return null;
            Action newGene = legal[rnd.Next(legal.Count)];

            // 4) build mutated genome
            var newGenome = new List<Action>();
            // copy prefix (unchanged)
            for (int i = 0; i < index; i++)
                newGenome.Add(parent.Genome.Actions[i].createDeepClone());
            // mutation
            newGenome.Add(newGene);

            // 5) **repair** remainder of turn with greedy helper
            Map stateAfterMutation = prefixState.createDeepClone();
            stateAfterMutation.executeAction(newGene); // TODO: THIS MIGHT BE BAD

            var remainingGreedy = BuildGreedyGenome(stateAfterMutation, teamColor);
            newGenome.AddRange(remainingGreedy.Actions);

            // cap genome length to original (otherwise sequence keeps growing)
            while (newGenome.Count > parent.Genome.Actions.Count)
                newGenome.RemoveAt(newGenome.Count - 1);

            // 6) build node & evaluate state (= leaf)
            var child = new Node
            {
                Genome = new Genome(newGenome),
                State = ApplyGenome(baseMap, new Genome(newGenome)),
                Visits = 0,
                TotalValue = 0
            };

            parent.Children.Add(child);
            return child;
        }

        /* ==================================================================
         * ░░  Helper utilities
         * ================================================================= */
        /// <summary>Applies the genome to a fresh clone of <paramref name="origin"/>.</summary>
        private static Map ApplyGenome(Map origin, Genome genome)
        {
            var clone = origin.createDeepClone();
            foreach (var a in genome.Actions)
            {
                // ignore invalid actions (unit dead, destination occupied, …)
                var unit = clone.getUnit(a.operationUnitId);
                if (unit == null || unit.isActionFinished())
                    break;

                // For move/attack actions, double‑check occupancy as map may have changed.
                if (a.actionType == Action.ACTIONTYPE_MOVEONLY || a.actionType == Action.ACTIONTYPE_MOVEANDATTACK)
                {
                    var occupant = clone.getUnit(a.destinationXpos, a.destinationYpos);
                    if (occupant != null && occupant.getID() != a.operationUnitId)
                        break; // illegal now – abort execution
                }

                clone.executeAction(a);
            }
            return clone;
        }

        /// <summary>Greedy helper: build a *complete* turn by repeatedly picking the single action that maximises the immediate heuristic.</summary>
        private Genome BuildGreedyGenome(Map map, int teamColor)
        {
            var state = map.createDeepClone();
            var actions = new List<Action>();
            
            while (true)
            {
                var movable = state.getUnitsList(teamColor, false, true, false);
                if (movable.Count == 0) break;

                Action bestAct = null;
                double bestVal = double.NegativeInfinity;

                foreach (var u in movable)
                {
                    var allActs = M_Tools.getUnitActions(u, state);
                    foreach (var act in allActs)
                    {
                        var tmp = state.createDeepClone();
                        tmp.executeAction(act);
                        double val = EvaluateState(tmp, teamColor);
                        if (val > bestVal)
                        {
                            bestVal = val;
                            bestAct = act;
                        }
                    }
                }

                if (bestAct == null) break; // no legal action found (should not happen)

                actions.Add(bestAct);
                state.executeAction(bestAct);
            }

            greedyInitLength = actions.Count;
            return new Genome(actions);
        }

        /// <summary>Removes and returns the first action that is still legal in the current map.</summary>
        private Action PopFirstValidAction(List<Action> genome, Map map, int teamColor)
        {
            while (genome.Count > 0)
            {
                var act = genome[0];
                genome.RemoveAt(0);

                Unit unit = map.getUnit(act.operationUnitId);
                if (unit == null || unit.isActionFinished() || unit.getTeamColor() != teamColor)
                    continue; // skip invalid gene

                // validity checks similar to ApplyGenome()
                if (act.actionType == Action.ACTIONTYPE_MOVEONLY || act.actionType == Action.ACTIONTYPE_MOVEANDATTACK)
                {
                    var occupant = map.getUnit(act.destinationXpos, act.destinationYpos);
                    if (occupant != null && occupant.getID() != act.operationUnitId)
                        continue; // illegal now – try next gene
                }

                return act;
            }
            return null;
        }

        /// <summary>Static evaluation wrapper so we can swap easily.</summary>
        private static double EvaluateState(Map map, int teamColor)
        {
            return AI_M_UCT.evaluateStateValue(map, teamColor);
        }
    }
}
