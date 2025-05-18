using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace SimpleWars
{
    /// <summary>
    /// 2016年度GAT杯優勝AI
	/// 製作者 Muto
    /// </summary>
    class AI_M_UCT : Player
    {
        // パラメータ
        private const int MAX_SIM = 200;       // 1行動あたりのシミュレーション回数    2016/1/26までは5000回を使用
        private const int SIM_SIKI = 10;        // 木探索での子ノードを展開する閾値 3, 5, 10, 20あたり？
        private const double UCB_CONST = 0.15;  // UCB値の特性を定める定数
        //private const float Threshold = 0.5f;

        // デバッグ用変数
        //private static int dev_num;
        private static int max_depth;

        private static int totalsim;            // UCB値計算に用いる変数
        private static int lastid;              // 木探索の再帰で直前のノードidを記憶するための変数
        private static int movablenum;          // 未行動ユニットの数

        //ストップウォッチ関連
        private Stopwatch stopwatch = new Stopwatch();
        private static long timeLeft;           // 残り時間
        private const long LIMIT_TIME = 9700;   // 1ターンにかける時間(ミリ秒)     2016/1/26までは9500回を使用

        #region 表示名、パラメータ情報
        // AIの表示名を返す（必須）
        // 改行文字 \r, \n，半角大カッコ [, ]，システム文字は使えない
        public string getName()
        {
            return "M-UCT";
        }

        // パラメータ等の情報を返す（必須だが，空でも良い）
        // 改行は含んでも構わない．半角大カッコ [, ], システム文字は使えない
        public string showParameters()
        {
            return "";

            // 例えば PARAM1, PARAM2 というパラメータがあって，棋譜等に残したい場合
            // return "PARAM1 = " + PARAM1 + "\r\n" + "PARAM2 = " + PARAM2;
        }
        #endregion

        // 1ユニットの行動を決定する（必須）
        // なお，ここでもらった map オブジェクトはコピーされたものなので，どのように利用・変更しても問題ない．
        public Action makeAction(Map map, int teamColor, bool turnStart, bool gameStart)
        {
            stopwatch.Start();

            // ルートノード作成
            M_GameTree root = makeroot(map, teamColor);

            //Logger.addLogMessage("next node: " + root.next.Count + "\r\n", teamColor);

            // UCT探索
            if (turnStart)
            {
                timeLeft = LIMIT_TIME;
            }

            movablenum = map.getUnitsList(teamColor, false, true, false).Count;

            totalsim = 0;

            for (int i = 0; i < MAX_SIM; i++)
            {
                if (stopwatch.ElapsedMilliseconds > (timeLeft / movablenum))
                {
                    //Logger.addLogMessage("timeover.(Normal)\r\n", teamColor);
                    break;
                }
                search(root, teamColor);

                totalsim++;
            }

            // デバッグする時はここで探索木の様子をログ表示したりする
            stopwatch.Stop();
            Logger.addLogMessage("sim_time: " + stopwatch.ElapsedMilliseconds + "\r\n", teamColor);
            Logger.log("depth: " + max_depth + "\r\n", teamColor);
            Logger.log("sim_num: " + totalsim + "\r\n", teamColor);

            timeLeft -= stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();

            return maxRateAction(root);
        }

        // root node 作成
        public static M_GameTree makeroot(Map map, int teamcolor)
        {
            Random fRand = new Random();

            M_GameTree root = new M_GameTree();
            root.board = map.createDeepClone();

            List<Action> allActions = new List<Action>(); // 全合法手
            List<Unit> allUnits = map.getUnitsList(teamcolor, false, true, false);  // 未行動自ユニット

            foreach (Unit u in allUnits)
            {
                List<Action> the_acts = M_Tools.getUnitActions(u, map); // 選択したユニットの全行動
                allActions.AddRange(the_acts);
            }

            foreach (Action act in allActions)
            {
                // ここで枝刈り（というか木に追加しない）する関数

                M_GameTree child = new M_GameTree();
                child.board = map.createDeepClone();
                child.act = act;
                child.depth = 1;
                child.board.executeAction(act);
                root.next.Add(child);
            }
            return root;
        }

        // 主探索部
        public static void search(M_GameTree n, int teamcolor)
        {
            if (totalsim == 0)
                max_depth = 0;

            if (n.depth > max_depth)
                max_depth = n.depth;

            int enemycolor = getenemycolor(teamcolor);
            int maxID = 0;
            double maxUCB = -1;
            double tmpUCB;

            for (int j = 0; j < n.next.Count; j++)
            {
                M_GameTree child = n.next[j];
                tmpUCB = evaluateUCT(child);

                if (tmpUCB == 100) // simnum = 0
                {
                    maxUCB = tmpUCB;
                    maxID = j;
                    break;
                }

                if (tmpUCB > maxUCB)
                {
                    maxUCB = tmpUCB;
                    maxID = j;
                }
            }

            if (n.next[maxID].simnum > SIM_SIKI) // 展開閾値を超えている)
            {
                if (n.next[maxID].next.Count == 0) // 展開されてない
                {
                    if (n.next[maxID].board.getUnitsList(teamcolor, false, true, false).Count > 0) // 身行動自ユニットがいる
                    {
                        development(n.next[maxID], teamcolor);
                    }
                    else if (n.next[maxID].board.getUnitsList(enemycolor, false, true, false).Count > 0) // 身行動相手ユニットならいる
                    {
                        development(n.next[maxID], enemycolor);
                    }
                    else // 未行動ユニットが存在しない
                    {
                        // 2015_6_8 修正
                        n.next[maxID].board.enableUnitsAction(teamcolor);
                        n.next[maxID].board.enableUnitsAction(enemycolor);
                        // // 2015_11_17修正
                        if (n.next[maxID].board.getUnitsList(teamcolor, false, true, false).Count > 0) // 生存自ユニットがいる
                            development(n.next[maxID], teamcolor);
                        else
                            development(n.next[maxID], enemycolor);
                    }
                }

                search(n.next[maxID], teamcolor);

                // 子ノードから返ってきた結果を反映させる
                n.next[maxID].simnum++;
                n.next[maxID].lastscore = n.next[maxID].next[lastid].lastscore;
                n.next[maxID].totalscore += n.next[maxID].lastscore;
                n.next[maxID].housyuu = n.next[maxID].totalscore / n.next[maxID].simnum;

                lastid = maxID;
            }
            else // 末端の葉ノード
            {
                // ランダムシミュレーション
                Map result = randomsimulation(n.next[maxID].board, teamcolor);

                n.next[maxID].simnum++;
                n.next[maxID].lastscore = evaluateStateValue(result, teamcolor);
                n.next[maxID].totalscore += n.next[maxID].lastscore;
                n.next[maxID].housyuu = n.next[maxID].totalscore / n.next[maxID].simnum;

                lastid = maxID;
            }

        }

        // 勝率が最も高いノードの行動を返す
        public static Action maxRateAction(M_GameTree root)
        {
            int rtnID = 0;
            double maxrate = 0;

            for (int i = 0; i < root.next.Count; i++)
            {
                M_GameTree rtnnode = root.next[i];
                double tmprate = root.next[i].housyuu;
                if (tmprate > maxrate)
                {
                    maxrate = tmprate;
                    rtnID = i;
                }
            }

            return root.next[rtnID].act;
        }

        //ランダムシミュレーション
        public static Map randomsimulation(Map map, int teamcolor)
        {
            int enemycolor = getenemycolor(teamcolor);

            Random fRand = new Random();

            Map simmap = map.createDeepClone();

            while (simmap.getTurnCount() < simmap.getTurnLimit())
            {
                List<Unit> simUnits = simmap.getUnitsList(teamcolor, false, true, false);

                while (simUnits.Count > 0)
                {
                    Action simact;
                    if (fRand.NextDouble() >= 0.2 && M_Tools.getAllAttackActions(teamcolor, simmap).Count > 0)
                    {
                        List<Action> simacts = M_Tools.getAllAttackActions(teamcolor, simmap);
                        simact = simacts[fRand.Next(simacts.Count)];//ランダムに１つを選択

                    }
                    else
                    {
                        Unit simUnit = simUnits[fRand.Next(simUnits.Count)]; // ランダムに１つを選択
                        List<Action> simacts = M_Tools.getUnitActions(simUnit, simmap);//選択したユニットの全行動
                        simact = simacts[fRand.Next(simacts.Count)];//ランダムに１つを選択
                    }

                    // 2015_6_26 こちらに変更
                    simUnits.Remove(simmap.getUnit(simact.operationUnitId));

                    simmap.executeAction(simact);

                    //simUnits = simmap.getUnitsList(teamcolor, false, true, false);  // 未行動自ユニット
                }

                simmap.enableUnitsAction(teamcolor); // 活性化

                simmap.incTurnCount(); // ターンインクリメント

                if (simmap.getTurnCount() >= simmap.getTurnLimit())
                    break;

                List<Unit> enemies = simmap.getUnitsList(enemycolor, false, true, false);  // 未行動敵リスト

                while (enemies.Count > 0)
                {
                    Action enact;
                    if (fRand.NextDouble() >= 0.2 && M_Tools.getAllAttackActions(enemycolor, simmap).Count > 0)
                    {
                        List<Action> enacts = M_Tools.getAllAttackActions(enemycolor, simmap);
                        enact = enacts[fRand.Next(enacts.Count)];//ランダムに１つを選択

                    }
                    else
                    {
                        Unit enUnit = enemies[fRand.Next(enemies.Count)];//ランダムに１つを選択
                        List<Action> enacts = M_Tools.getUnitActions(enUnit, simmap);//選択したユニットの全行動
                        enact = enacts[fRand.Next(enacts.Count)];//ランダムに１つを選択
                    }

                    // 2015_6_26 こちらに変更
                    enemies.Remove(simmap.getUnit(enact.operationUnitId));

                    simmap.executeAction(enact);

                    //enemies = simmap.getUnitsList(enemycolor, false, true, false);  // 全ての敵リスト
                }

                simmap.enableUnitsAction(enemycolor);//活性化

                simmap.incTurnCount(); // ターンインクリメント

                if (simmap.getTurnCount() >= simmap.getTurnLimit())
                    break;

                if (simmap.getUnitsList(teamcolor, true, true, false).Count == 0 ||
                    simmap.getUnitsList(teamcolor, false, false, true).Count == 0)
                    break;

            }

            return simmap;
        }

        // ノードの展開
        public static void development(M_GameTree n, int teamcolor)
        {
            Random fRand = new Random();

            List<Action> allActions = new List<Action>();
            List<Unit> allUnits = n.board.getUnitsList(teamcolor, false, true, false);  // 未行動自ユニット

            foreach (Unit u in allUnits)
            {
                List<Action> the_acts = M_Tools.getUnitActions(u, n.board);//選択したユニットの全行動
                allActions.AddRange(the_acts);
            }

            foreach (Action act in allActions)
            {
                // ここで枝刈り（というか木に追加しない）する関数

                M_GameTree child = new M_GameTree();
                child.board = n.board.createDeepClone();

                // 2015_6_26
                // ターンカウントを正しく数えられるように変更
                if (n.board.getUnitsList(teamcolor, true, false, false).Count == 0)  // 行動済みユニットが存在しない（ターン変更直後）
                    child.board.incTurnCount();

                child.act = act;
                child.depth = n.depth + 1;
                child.board.executeAction(act);
                n.next.Add(child);
            }

        }

        //UCB値計算
        public static double evaluateUCT(M_GameTree n)
        {
            if (n.simnum == 0)
                return 100;//return Fuzzy_Table.returnValue(n.act, n.board);//return 100;
            else
                return n.housyuu + UCB_CONST * Math.Sqrt(Math.Log(totalsim, Math.E) / n.simnum);
        }

        //状態評価関数(シミュレーション後)    return 1, 0.5, or 0
        public static double evaluateStateValue(Map map, int teamcolor)
        {
            int enemycolor = getenemycolor(teamcolor);

            List<Unit> myTeamUnits = map.getUnitsList(teamcolor, true, true, false);//自分チームの全てのユニット
            List<Unit> enemyTeamUnits = map.getUnitsList(enemycolor, true, true, false);//相手チームの全てのユニット

            if (myTeamUnits.Count == 0)
                return 0;
            if (enemyTeamUnits.Count == 0)
                return 1;

            double mytotalHP = 0;
            double enemytotalHP = 0;

            for (int i = 0; i < myTeamUnits.Count; i++)
                mytotalHP += myTeamUnits[i].getHP();

            for (int i = 0; i < enemyTeamUnits.Count; i++)
                enemytotalHP += enemyTeamUnits[i].getHP();

            if (mytotalHP - enemytotalHP >= map.getDrawHPThreshold())
                return 1;
            else if (Math.Abs(mytotalHP - enemytotalHP) < map.getDrawHPThreshold())
                return 0.5;
            else
                return 0;

        }

        // enemycolor定義
        public static int getenemycolor(int teamcolor)
        {
            if (teamcolor == 1)
                return 0;
            else
                return 1;
        }

        // その時点でunitが次の相手ターンに受けうる最高ダメージ
        public static int possible_damaged(Map map, Unit unit, int teamcolor)
        {
            Map clone = map.createDeepClone();

            int enemycolor = getenemycolor(teamcolor);

            List<Unit> enemys = clone.getUnitsList(enemycolor, false, true, false);
            //List<Action> enemyattacks = new List<Action>();

            int damage_sum = 0;

            while (enemys.Count > 0)
            {
                int maxdamage = 0;
                int unit_id = 0;
                Action tmpact = new Action();

                for (int j = 0; j < enemys.Count; j++)
                {
                    List<Action> attacks = RangeController.getAttackActionList(enemys[j], clone);
                    //enemyattacks.AddRange(attacks);

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

                if (maxdamage == 0)
                    break;
                else
                    damage_sum += maxdamage;

                clone.executeAction(tmpact);
                enemys.RemoveAt(unit_id);
            }

            if (damage_sum >= unit.getHP())
                damage_sum = unit.getHP();

            return damage_sum;
        }


    }

}
