using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SimpleWars
{
    /// <summary>
    /// Evolutionary MCTS with Flexible Horizon (FH‑EMCTS).
    ///
    /// This implementation extends the original bridge‑burning EMCTS agent
    /// (Baier & Cowling — CIG 2018) with the “flexible search horizon” idea
    /// presented in their AIIDE 2018 follow‑up.  Instead of optimising only the
    /// *current* turn, genomes here span a fixed number of complete turns
    /// (<see cref="HORIZON_TURNS"/>).  After every mutation we rebuild all
    /// *future* actions greedily so legality is preserved without inflating the
    /// branching factor.
    ///
    /// The code is intentionally self‑contained — it depends only on the public
    /// SimpleWars API and can be dropped straight into the project next to
    /// AI_EMCTS.cs.  Where possible, naming and style follow the existing
    /// EMCTS conventions to make diff‑based reviews easy.
    /// </summary>
    class AI_EMCTS_FH : Player
    {
        /* ==================================================================
         * ░░  Meta / parameters
         * ================================================================= */
        public string getName() => "EMCTS-FH";

        public string showParameters() =>
            $"TIME/turn      = {LIMIT_TIME} ms (≈{LIMIT_TIME/1_000:0.0}s)\r\n" +
            $"HORIZON_TURNS  = {HORIZON_TURNS}\r\n" +
            $"PHASE_COUNT    = {PHASE_COUNT}\r\n" +
            $"MAX_CHILDREN   = {MAX_CHILDREN}\r\n" +
            $"UCB_C          = {UCB_C:0.###}";

        // Hard CPU budget identical to the baseline AIs
        private const long   LIMIT_TIME     = AI_Consts.LIMIT_TIME;  // ms per *real* game turn
        private const int    HORIZON_TURNS  = 2;      // each horizon increment is current turn + opponent turn
        private const int    PHASE_COUNT    = 30;     // bridge‑burning phases
        private const int    MAX_CHILDREN   = 100;    // ≤ 5× typical branching factor
        private const double UCB_C          = 0.15;   // UCB exploration constant

        /* ==================================================================
         * ░░  Internal runtime state
         * ================================================================= */
        private readonly Random    rnd       = new Random();
        private readonly Stopwatch stopwatch = new Stopwatch();
        private static   long      timeLeft;          // remaining budget (ms)

        // Shift‑buffer holding the best genome found earlier in the same turn
        private List<Action> cachedGenome = null;

        /* ==================================================================
         * ░░  Core data structures
         * ================================================================= */
        /// <summary>A genome is a *sequence* of actions spanning several turns.</summary>
        private class Genome
        {
            public readonly List<Action> Actions;
            public Genome() => Actions = new List<Action>();
            public Genome(IEnumerable<Action> acts) => Actions = new List<Action>(acts);
            public Genome DeepClone() => new Genome(Actions.Select(a => a.createDeepClone()));
        }

        /// <summary>MCTS node that stores a genome and accumulated statistics.</summary>
        private class Node
        {
            public Genome       Genome;
            public Map          State;
            public readonly List<Node> Children = new List<Node>();
            public int    Visits;
            public double TotalValue;
            public double MeanValue => Visits == 0 ? 0.0 : TotalValue / Visits;
        }

        /* ==================================================================
         * ░░  Public entry – the game engine calls this once per required action
         * ================================================================= */
        public Action makeAction(Map map, int teamColor, bool turnStart, bool gameStart)
        {
            /* ---- 1 / 3  Book‑keeping ------------------------------------- */
            if (turnStart)
            {
                timeLeft     = LIMIT_TIME;
                cachedGenome = null;
            }

            // Fast path: we already hold a valid action sequence from the last
            // call (still in the same turn) — just pop the next legal action.
            if (cachedGenome != null && cachedGenome.Count > 0)
            {
                var nextFast = PopFirstValidAction(cachedGenome, map, teamColor);
                if (nextFast != null) return nextFast;
            }

            /* ---- 2 / 3  Root initialisation ----------------------------- */
            stopwatch.Restart();

            var rootGenome = BuildFHGenome(map, teamColor);


            var rootNode = new Node();
            rootNode.Genome = rootGenome;
            rootNode.State = ApplyGenome(map, rootGenome);
            rootNode.Visits = 1;                      // avoids div‑by‑zero in UCB
            rootNode.TotalValue = EvaluateState(rootNode.State, teamColor);

            long phaseBudget = timeLeft / PHASE_COUNT; // equal split per phase

            /* ---- 3 / 3  Bridge‑burning EMCTS search ---------------------- */
            for (int phase = 0; phase < PHASE_COUNT; phase++)
            {
                if (stopwatch.ElapsedMilliseconds >= timeLeft) break;
                PerformSearchPhase(rootNode, map, teamColor, phaseBudget);

                if (rootNode.Children.Count == 0) break; // could not expand further

                // Bridge‑burn: keep only the best child as the new root
                rootNode = rootNode.Children.OrderByDescending(c => c.MeanValue).First();
            }

            stopwatch.Stop();
            timeLeft -= stopwatch.ElapsedMilliseconds;

            /* ---- Emit the chosen action ---------------------------------- */
            cachedGenome = new List<Action>(rootNode.Genome.Actions);
            var chosen   = PopFirstValidAction(cachedGenome, map, teamColor);
            return chosen ?? Action.createTurnEndAction();
        }

        /* ==================================================================
         * ░░  Search‑phase helpers
         * ================================================================= */
        private void PerformSearchPhase(Node root, Map baseMap, int teamColor, long phaseBudget)
        {
            var watch = Stopwatch.StartNew();

            while (watch.ElapsedMilliseconds < phaseBudget)
            {
                // Path from root to leaf
                var path = new Stack<Node>();
                Node node = root;
                path.Push(node);

                /* Selection ------------------------------------------------ */
                while (node.Children.Count > 0 && !NeedsExpansion(node))
                {
                    node = SelectChildUCB(node);
                    path.Push(node);
                }

                /* Expansion ------------------------------------------------ */
                if (NeedsExpansion(node) && node.Visits > 0)
                {
                    var child = Expand(node, baseMap, teamColor);
                    if (child != null)
                    {
                        node = child;
                        path.Push(node);
                    }
                }

                /* Evaluation (leaf) --------------------------------------- */
                double value;
                if (node.Visits == 0)
                {
                    value          = EvaluateState(node.State, teamColor);
                }
                else
                {
                    value = node.MeanValue; // already evaluated before
                }

                /* Back‑propagation ---------------------------------------- */
                foreach (var n in path)
                {
                    n.Visits     += 1;
                    n.TotalValue += value;
                }
            }
            watch.Stop();
        }

        private bool NeedsExpansion(Node node) => node.Children.Count < MAX_CHILDREN;

        private Node SelectChildUCB(Node parent)
        {
            double logParent = Math.Log(parent.Visits);
            double bestScore = double.NegativeInfinity;
            Node   best      = null;

            foreach (var child in parent.Children)
            {
                // there shouldn't be div/0 because no unvisited children
                double ucb = child.MeanValue + UCB_C * Math.Sqrt(logParent / child.Visits);
                if (ucb > bestScore)
                {
                    bestScore = ucb;
                    best      = child;
                }
            }
            return best;
        }

        private Node Expand(Node parent, Map baseMap, int teamColor)
        {
            if (parent.Genome.Actions.Count == 0) return null;

            /* 1. Determine indices owned by *us* -------------------------------- */
            var ownIndices = new List<int>();
            var tmpState   = baseMap.createDeepClone();
            for (int i = 0; i < parent.Genome.Actions.Count; i++)
            {
                if (tmpState.getTurnColor() == teamColor) ownIndices.Add(i);
                tmpState.executeAction(parent.Genome.Actions[i]);
            }
            if (ownIndices.Count == 0) return null; // should not happen

            /* 2. Pick random gene to mutate ---------------------------------- */
            int index = ownIndices[rnd.Next(ownIndices.Count)];

            var prefixState = baseMap.createDeepClone();
            for (int i = 0; i < index; i++) prefixState.executeAction(parent.Genome.Actions[i]);

            var opUnit = prefixState.getUnit(parent.Genome.Actions[index].operationUnitId);
            if (opUnit == null || opUnit.isActionFinished()) return null;

            var legal = M_Tools.getUnitActions(opUnit, prefixState);
            if (legal.Count == 0) return null;
            var newGene = legal[rnd.Next(legal.Count)];

            /* 3. Assemble mutated genome ----------------------------------- */
            var newActs = new List<Action>();
            for (int i = 0; i < index; i++) newActs.Add(parent.Genome.Actions[i].createDeepClone());
            newActs.Add(newGene);

            // Repair tail greedily (flexible horizon)
            var stateAfterMutation = prefixState.createDeepClone();
            stateAfterMutation.executeAction(newGene);
            var greedyTail = BuildFHGenome(stateAfterMutation, stateAfterMutation.getTurnColor(), HORIZON_TURNS * 2);
            newActs.AddRange(greedyTail.Actions);

            /* 4. Trim to original length to keep trees shallow --------------- */
            while (newActs.Count > parent.Genome.Actions.Count) newActs.RemoveAt(newActs.Count - 1);

            /* 5. Create child node ------------------------------------------ */
            var child = new Node
            {
                Genome      = new Genome(newActs),
                State       = ApplyGenome(baseMap, new Genome(newActs)),
                Visits      = 0,
                TotalValue  = 0
            };
            parent.Children.Add(child);
            return child;
        }

        /* ==================================================================
         * ░░  Greedy helper (multi‑turn)
         * ================================================================= */
        private Genome BuildFHGenome(Map map, int perspectiveColor, int maxSlices = HORIZON_TURNS * 2)
        {
            var actions = new List<Action>();
            var sim     = map.createDeepClone();
            int slices  = 0;
            int color = perspectiveColor;

            while (slices < maxSlices)
            {
                if (sim.isAllUnitActionFinished(color))
                {
                    color = 1 - color;
                    slices++;
                    sim.enableUnitsAction(color);
                    sim.incTurnCount();
                    continue;
                }

                var movable = sim.getUnitsList(color, false, true, false);
                if (movable.Count == 0)
                {
                    sim.executeAction(Action.createTurnEndAction());
                    continue;
                }

                Action bestAct = null;
                double bestVal = double.NegativeInfinity;

                foreach (var u in movable)
                {
                    var allActs = M_Tools.getUnitActions(u, sim);
                    foreach (var act in allActs)
                    {
                        var tmp = sim.createDeepClone();
                        tmp.executeAction(act);
                        double val = EvaluateState(tmp, perspectiveColor);
                        if (val > bestVal)
                        {
                            bestVal = val;
                            bestAct = act;
                        }
                    }
                }

                if (bestAct == null)
                {
                    sim.executeAction(Action.createTurnEndAction());
                    continue;
                }

                actions.Add(bestAct);
                sim.executeAction(bestAct);
            }

            return new Genome(actions);
        }

        /* ==================================================================
         * ░░  Misc helpers
         * ================================================================= */
        private static Map ApplyGenome(Map origin, Genome genome)
        {
            var clone = origin.createDeepClone();
            foreach (var a in genome.Actions)
            {
                var unit = clone.getUnit(a.operationUnitId);
                if (unit == null || unit.isActionFinished() || unit.getTeamColor() != clone.getTurnColor())
                    continue; // illegal or wrong phase — skip

                if (a.actionType == Action.ACTIONTYPE_MOVEONLY || a.actionType == Action.ACTIONTYPE_MOVEANDATTACK)
                {
                    var occ = clone.getUnit(a.destinationXpos, a.destinationYpos);
                    if (occ != null && occ.getID() != a.operationUnitId) continue;
                }

                clone.executeAction(a);
            }
            return clone;
        }

        private Action PopFirstValidAction(List<Action> genome, Map map, int teamColor)
        {
            while (genome.Count > 0)
            {
                var act = genome[0];
                genome.RemoveAt(0);

                var unit = map.getUnit(act.operationUnitId);
                if (unit == null || unit.isActionFinished() || unit.getTeamColor() != map.getTurnColor())
                    continue;

                if (act.actionType == Action.ACTIONTYPE_MOVEONLY || act.actionType == Action.ACTIONTYPE_MOVEANDATTACK)
                {
                    var occ = map.getUnit(act.destinationXpos, act.destinationYpos);
                    if (occ != null && occ.getID() != act.operationUnitId) continue;
                }

                return act;
            }
            return null;
        }

        private static double EvaluateState(Map map, int teamColor) => AI_RHEA.EvaluateState(map, teamColor);
    }
}
