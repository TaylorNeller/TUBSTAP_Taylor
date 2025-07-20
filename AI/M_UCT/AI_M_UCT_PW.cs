using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace SimpleWars
{
    /// <summary>
    /// 2016年度GAT杯優勝AI (Muto) — modified to use Progressive Widening (PW).
    /// </summary>
    class AI_M_UCT_PW : Player
    {
        // ------------------------------------------------------------------
        // Original Parameters
        // ------------------------------------------------------------------
        private const int MAX_SIM = 2000;            // simulations per move
        private const int SIM_SIKI = 10;            // (legacy) expansion threshold – now unused but kept for compatibility
        private const double ATTACK_PRIORITY = 0.8; // chance of favoring attack in rollout
        private const double UCB_CONST = 0.15;      // UCB exploration constant

        // ------------------------------------------------------------------
        // Progressive Widening Parameters
        // k(s) = ceil(PW_C * N(s)^PW_ALPHA)
        // Tune these! α in (0,1).
        // ------------------------------------------------------------------
        private const double PW_ALPHA = 0.4;        // growth rate of branching factor
        private const int    PW_C     = 2;          // scale of branching factor

        // debug
        private static int max_depth;

        // UCT globals
        private static int totalsim;            // total # sims run this turn (for ln(N) in UCB)
        //private static int lastid;            // legacy backprop id (unused w/ PW)
        private static int movablenum;          // # units that can act this turn (time-slice)

        // stopwatch / time mgmt
        private Stopwatch stopwatch = new Stopwatch();
        private static long timeLeft;           // ms left this turn across remaining units
        private const long LIMIT_TIME = AI_Consts.LIMIT_TIME;   // ms budget per turn

        // RNG (single static to avoid reseeding issues)
        private static readonly Random RNG = new Random();

        #region 表示名、パラメータ情報
        public string getName() { return "M-UCT-PW"; }
        public string showParameters() { return ""; }
        #endregion

        // ------------------------------------------------------------------
        // Top-level action selection (called once per unit move)
        // ------------------------------------------------------------------
        public Action makeAction(Map map, int teamColor, bool turnStart, bool gameStart)
        {
            stopwatch.Start();

            // build / refresh root
            M_GameTree_PW root = makeroot(map, teamColor);

            if (turnStart)
            {
                timeLeft = LIMIT_TIME; // reset turn budget
            }

            movablenum = map.getUnitsList(teamColor, false, true, false).Count;
            if (movablenum <= 0) movablenum = 1; // safety

            totalsim = 0;

            int n_iters = MAX_SIM;
            if (MAX_SIM < 0)
            {
                n_iters = int.MaxValue;
            }

            for (int i = 0; i < n_iters; i++)
            {
                if (stopwatch.ElapsedMilliseconds > (timeLeft / movablenum))
                {
                    break; // out of budget slice
                }
                search(root, teamColor);
                totalsim++;
            }

            stopwatch.Stop();
            Logger.addLogMessage("sim_time: " + stopwatch.ElapsedMilliseconds + "\r\n", teamColor);
            Logger.log("depth: " + max_depth + "\r\n", teamColor);
            Logger.log("sim_num: " + totalsim + "\r\n", teamColor);

            timeLeft -= stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();

            return maxRateAction(root);
        }

        // ------------------------------------------------------------------
        // Root node construction (now PW-aware)
        // ------------------------------------------------------------------
        public static M_GameTree_PW makeroot(Map map, int teamcolor)
        {
            M_GameTree_PW root = new M_GameTree_PW();
            root.board = map.createDeepClone();
            root.depth = 0;

            // collect all legal actions from all currently-movable units
            List<Action> allActions = new List<Action>();
            List<Unit> allUnits = map.getUnitsList(teamcolor, false, true, false);
            foreach (Unit u in allUnits)
            {
                List<Action> acts = M_Tools.getUnitActions(u, map);
                allActions.AddRange(acts);
            }

            // stash in untried; actual child creation gated by PW
            root.untried.AddRange(allActions);

            // perform initial PW expansion so root has at least 1 child
            ProgressiveExpand(root, teamcolor);
            return root;
        }

        // ------------------------------------------------------------------
        // Core MCTS search with Progressive Widening
        // ------------------------------------------------------------------
        public static void search(M_GameTree_PW n, int teamcolor)
        {
            if (totalsim == 0) max_depth = 0;
            if (n.depth > max_depth) max_depth = n.depth;

            // ensure node widened enough for current visit counts
            ProgressiveExpand(n, teamcolor);
            if (n.next.Count == 0)
            {
                // no legal actions: rollout from this state
                double val = evaluateStateValue(n.board, teamcolor);
                n.simnum++;
                n.lastscore = val;
                n.totalscore += val;
                n.housyuu = n.totalscore / n.simnum;
                return;
            }

            // UCB child selection
            int maxID = 0; double maxUCB = double.NegativeInfinity;
            for (int j = 0; j < n.next.Count; j++)
            {
                M_GameTree_PW child = n.next[j];
                double tmpUCB = evaluateUCT(child);
                if (tmpUCB > maxUCB)
                {
                    maxUCB = tmpUCB;
                    maxID = j;
                }
            }
            M_GameTree_PW sel = n.next[maxID];

            // before descending, widen selected child if allowed
            int enemycolor = getenemycolor(teamcolor);
            if (sel.untried.Count > 0)
            {
                int whoActs = sel.board.getUnitsList(teamcolor, false, true, false).Count > 0 ? teamcolor :
                              (sel.board.getUnitsList(enemycolor, false, true, false).Count > 0 ? enemycolor : teamcolor);
                ProgressiveExpand(sel, whoActs);
            }

            // If child has further children, recurse; else rollout from that child's state.
            double resultValue;
            if (sel.next.Count > 0)
            {
                search(sel, teamcolor); // deeper search updates sel's stats
                resultValue = sel.lastscore; // value backed up from deeper call
            }
            else
            {
                Map result = randomsimulation(sel.board, teamcolor);
                resultValue = evaluateStateValue(result, teamcolor);
                sel.simnum++;
                sel.lastscore = resultValue;
                sel.totalscore += resultValue;
                sel.housyuu = sel.totalscore / sel.simnum;
            }

            // backprop to n
            n.simnum++;
            n.lastscore = resultValue;
            n.totalscore += resultValue;
            n.housyuu = n.totalscore / n.simnum;
        }

        // ------------------------------------------------------------------
        // Select max-mean child action from root
        // ------------------------------------------------------------------
        public static Action maxRateAction(M_GameTree_PW root)
        {
            int rtnID = 0; double maxrate = double.NegativeInfinity;
            for (int i = 0; i < root.next.Count; i++)
            {
                double tmprate = root.next[i].housyuu;
                if (tmprate > maxrate)
                {
                    maxrate = tmprate;
                    rtnID = i;
                }
            }
            return root.next[rtnID].act;
        }

        // ------------------------------------------------------------------
        // Random playout / rollout policy (unchanged)
        // ------------------------------------------------------------------
        public static Map randomsimulation(Map map, int teamcolor)
        {
            int enemycolor = getenemycolor(teamcolor);
            Map simmap = map.createDeepClone();

            while (simmap.getTurnCount() < simmap.getTurnLimit())
            {
                // ----- our side -----
                List<Unit> simUnits = simmap.getUnitsList(teamcolor, false, true, false);
                while (simUnits.Count > 0)
                {
                    Action simact;
                    if (RNG.NextDouble() < ATTACK_PRIORITY && M_Tools.getAllAttackActions(teamcolor, simmap).Count > 0)
                    {
                        List<Action> simacts = M_Tools.getAllAttackActions(teamcolor, simmap);
                        simact = simacts[RNG.Next(simacts.Count)];
                    }
                    else
                    {
                        Unit simUnit = simUnits[RNG.Next(simUnits.Count)];
                        List<Action> simacts = M_Tools.getUnitActions(simUnit, simmap);
                        simact = simacts[RNG.Next(simacts.Count)];
                    }
                    simUnits.Remove(simmap.getUnit(simact.operationUnitId));
                    simmap.executeAction(simact);
                }
                simmap.enableUnitsAction(teamcolor);
                simmap.incTurnCount();
                if (simmap.getTurnCount() >= simmap.getTurnLimit()) break;

                // ----- enemy side -----
                List<Unit> enemies = simmap.getUnitsList(enemycolor, false, true, false);
                while (enemies.Count > 0)
                {
                    Action enact;
                    if (RNG.NextDouble() < ATTACK_PRIORITY && M_Tools.getAllAttackActions(enemycolor, simmap).Count > 0)
                    {
                        List<Action> enacts = M_Tools.getAllAttackActions(enemycolor, simmap);
                        enact = enacts[RNG.Next(enacts.Count)];
                    }
                    else
                    {
                        Unit enUnit = enemies[RNG.Next(enemies.Count)];
                        List<Action> enacts = M_Tools.getUnitActions(enUnit, simmap);
                        enact = enacts[RNG.Next(enacts.Count)];
                    }
                    enemies.Remove(simmap.getUnit(enact.operationUnitId));
                    simmap.executeAction(enact);
                }
                simmap.enableUnitsAction(enemycolor);
                simmap.incTurnCount();
                if (simmap.getTurnCount() >= simmap.getTurnLimit()) break;

                // terminate if game ended
                if (simmap.getUnitsList(teamcolor, true, true, false).Count == 0 ||
                    simmap.getUnitsList(teamcolor, false, false, true).Count == 0)
                    break;
            }
            return simmap;
        }

        // ------------------------------------------------------------------
        // Progressive Widening expansion helper
        // ------------------------------------------------------------------
        private static void ProgressiveExpand(M_GameTree_PW n, int teamColor)
        {
            // how many children are allowed at this visit count?
            int allowed = Math.Max(1, (int)Math.Ceiling(PW_C * Math.Pow(n.simnum + 1, PW_ALPHA)));
            while (n.next.Count < allowed && n.untried.Count > 0)
            {
                int pick = RNG.Next(n.untried.Count);
                Action act = n.untried[pick];
                n.untried.RemoveAt(pick);

                M_GameTree_PW child = new M_GameTree_PW();
                child.board = n.board.createDeepClone();

                // maintain original turn-count-adjustment logic
                if (n.board.getUnitsList(teamColor, true, false, false).Count == 0)
                    child.board.incTurnCount();

                child.act = act;
                child.depth = n.depth + 1;
                child.board.executeAction(act);
                n.next.Add(child);
            }
        }

        // ------------------------------------------------------------------
        // Legacy development() kept for compatibility; now just seeds untried + PW
        // ------------------------------------------------------------------
        public static void development(M_GameTree_PW n, int teamcolor)
        {
            // gather legal actions
            List<Action> allActions = new List<Action>();
            List<Unit> allUnits = n.board.getUnitsList(teamcolor, false, true, false);
            foreach (Unit u in allUnits)
            {
                List<Action> acts = M_Tools.getUnitActions(u, n.board);
                allActions.AddRange(acts);
            }
            // store & let PW control actual expansion
            n.untried.AddRange(allActions);
            ProgressiveExpand(n, teamcolor);
        }

        // ------------------------------------------------------------------
        // UCB score
        // ------------------------------------------------------------------
        public static double evaluateUCT(M_GameTree_PW n)
        {
            if (n.simnum == 0) return double.PositiveInfinity; // force exploration
            return n.housyuu + UCB_CONST * Math.Sqrt(Math.Log(Math.Max(1, totalsim), Math.E) / n.simnum);
        }

        // ------------------------------------------------------------------
        // Terminal state evaluation after rollout
        // Returns 1 (win), 0.5 (draw-ish), 0 (loss)
        // ------------------------------------------------------------------
        public static double evaluateStateValue(Map map, int teamcolor)
        {
            int enemycolor = getenemycolor(teamcolor);
            List<Unit> myTeamUnits = map.getUnitsList(teamcolor, true, true, false);
            List<Unit> enemyTeamUnits = map.getUnitsList(enemycolor, true, true, false);

            if (myTeamUnits.Count == 0) return 0;
            if (enemyTeamUnits.Count == 0) return 1;

            double myHP = 0, enemyHP = 0;
            for (int i = 0; i < myTeamUnits.Count; i++) myHP += myTeamUnits[i].getHP();
            for (int i = 0; i < enemyTeamUnits.Count; i++) enemyHP += enemyTeamUnits[i].getHP();

            if (myHP - enemyHP >= map.getDrawHPThreshold()) return 1;
            if (Math.Abs(myHP - enemyHP) < map.getDrawHPThreshold()) return 0.5;
            return 0;
        }

        public static int getenemycolor(int teamcolor) { return (teamcolor == 1) ? 0 : 1; }

        // ------------------------------------------------------------------
        // Max possible damage a unit could suffer next enemy turn (unchanged)
        // ------------------------------------------------------------------
        public static int possible_damaged(Map map, Unit unit, int teamcolor)
        {
            Map clone = map.createDeepClone();
            int enemycolor = getenemycolor(teamcolor);
            List<Unit> enemys = clone.getUnitsList(enemycolor, false, true, false);
            int damage_sum = 0;
            while (enemys.Count > 0)
            {
                int maxdamage = 0;
                int unit_id = 0;
                Action tmpact = new Action();
                for (int j = 0; j < enemys.Count; j++)
                {
                    List<Action> attacks = RangeController.getAttackActionList(enemys[j], clone);
                    for (int i = 0; i < attacks.Count; i++)
                    {
                        if (attacks[i].targetUnitId != unit.getID()) continue;
                        int[] tmpdamage = DamageCalculator.calculateDamages(clone, attacks[i]);
                        if (tmpdamage[0] > maxdamage)
                        {
                            maxdamage = tmpdamage[0];
                            unit_id = j;
                            tmpact = attacks[i].createDeepClone();
                        }
                    }
                }
                if (maxdamage == 0) break;
                damage_sum += maxdamage;
                clone.executeAction(tmpact);
                enemys.RemoveAt(unit_id);
            }
            if (damage_sum >= unit.getHP()) damage_sum = unit.getHP();
            return damage_sum;
        }
    }

    // ======================================================================
    // M_GameTree node object — extended for Progressive Widening
    // ======================================================================
    class M_GameTree_PW
    {
        public Map board;
        public Action act;
        public int depth;

        public double housyuu;     // average return
        public double totalscore;  // sum of returns
        public int    simnum;      // visit count
        public double lastscore;   // most recent rollout value

        public List<M_GameTree_PW> next = new List<M_GameTree_PW>(); // expanded children
        public List<Action>     untried = new List<Action>();  // PW action pool
    }
}
