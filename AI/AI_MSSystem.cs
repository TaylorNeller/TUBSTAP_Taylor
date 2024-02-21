using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWars {
	/// <summary>
	/// 2015年GPW杯優勝AI
	/// 2016年GAT杯3位AI
	/// </summary>
    class AI_MSSystem : Player
    {
        //nの指定
        const int _N = 2;                   //n次作戦
        const int VISIBLE_FLAG = 0;         //視界内にいるかどうかを判別するフラグ位置
        const int ACTION_X = 10;            //Actionクラスで持つ変数の数
        const double NO_ENEMY = 0.5;        //敵が見つからない場合の勝率
        const double ATK_BONUS = 0.2;       //攻撃に対する勝率報酬

        const int UNIT_PO = 100;     //部隊毎の試行回数
        const int UNIT_DP = 10;     //部隊毎の試行深さ
        const int STAFF_PO = 100;   //全体思考の試行回数
        const int STAFF_DP = 10;    //全体思考の試行深さ

        private List<Action> retActionList;     //生成されたアクションリスト
        private List<List<Action>> nPlanActionList; //第n次作戦におけるアクションリスト
        private int turnCount;                  //手番変更を監視

        #region
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AI_MSSystem()
        {
        }

        // AIの表示名を返す
        public string getName() {
            return "Military Staff System";
        }
        
        // パラメータ等の情報を返す
        public string showParameters() {
            return "N=2, UPO=100, SPO=100";
        }
        #endregion

        /// <summary>
        /// 1ユニットの行動を決定する
        /// </summary>
        /// <param name="map">ディープコピーされたmap情報</param>
        /// <param name="teamColor">手番色</param>
        /// <returns>行動リスト</returns>
        public Action makeAction(Map map, int teamColor, bool turnStart, bool gameStart) {
            if(gameStart == true){
                turnCount = -1;
            }

            //ターン始めの呼び出し（手番目が前回と異なる）
            //1ターン目に勝った場合、次のAutoBattleでバグる可能性あり
            if (turnCount != map.getTurnCount())
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                InitAI(map, teamColor);   //初期化
                turnCount = map.getTurnCount();     //手番目を更新
                Logger.addLogMessage(Environment.NewLine + "-----------------------------" + 
                    Environment.NewLine + turnCount + "手番目の思考開始" + Environment.NewLine, teamColor);

                //n次作戦を計画
                for (int i = 1; i <= _N; i++)
                {
                    createNPlan(i, map, teamColor);
                }

                //返す行動計画を記憶
                for (int i = 0; i < nPlanActionList[_N].Count; i++)
                {
                    retActionList.Add(nPlanActionList[_N][i]);
                }

                sw.Stop();
                Logger.addLogMessage((sw.ElapsedMilliseconds / 1000.0) + "[sec]" + Environment.NewLine, teamColor);
            }

            //アクションリストがある場合の呼び出し
            if (retActionList.Count != 0)
            {
                //最初のアクションを取り出し、削除してから返す
                Action retAction = retActionList[0].createDeepClone();
                retActionList.RemoveAt(0);
                return retAction;
            }

            //アクションが空になっている（エラー）
            return Action.createTurnEndAction();
        }

        /// <summary>
        /// 初期化関数
        /// </summary>
        private void InitAI(Map map, int teamColor)
        {
            //前回作戦結果を初期化
            nPlanActionList = new List<List<Action>>();
            retActionList = new List<Action>();

            //第0次作戦（全員動かない）を生成
            nPlanActionList.Add(new List<Action>());
            int id = 0;
            List<Unit> uList = map.getUnitsList(teamColor, false, true, false);            
            foreach (Unit u in uList)
            {
                nPlanActionList[0].Add(Action.createMoveOnlyAction(u, u.getXpos(), u.getYpos()));
                id++;
            }
        }

        /// <summary>
        /// 第n次作戦の合法手リストを生成する
        /// </summary>
        /// <param name="n">n</param>
        /// <param name="map">行動前map</param>
        /// <param name="teamColor">合法手リストを得る部隊の色</param>
        private void createNPlan(int n, Map map, int teamColor)
        {
            //三次作戦以降でも使える汎用的手法考案中

            //第n次作戦を立てる
            nPlanActionList.Add(new List<Action>());
            //一時的な第n次作戦における行動リスト
            List<List<Action>> tmpNPlanActionList = new List<List<Action>>();

            #region Unit Thinking
            //各部隊ごと行動表生成
            foreach(Unit u in map.getUnitsList(teamColor, false, true, false))
            {
                Map forUnitMap = map.createDeepClone();     //部隊専用map

                //第n-1次作戦を元に部隊専用mapを生成
                for (int i = 0; i < nPlanActionList[n - 1].Count; i++)
                {
                    //自分のID以外にn-1次作戦行動を取らせる
                    if (nPlanActionList[n - 1][i].operationUnitId != u.getID())
                    {
                        //自分以外がn-1次計画を実行する
                        if (!executeAction(ref forUnitMap, nPlanActionList[n - 1][i]))
                        {
                            //行動に失敗する場合について考案中
                            Logger.addLogMessage(Environment.NewLine + "計画_行動失敗" + Environment.NewLine, teamColor);
                            continue;
                        }
                    }
                }

                //部隊の合法手を検討
                List<Action> uActionList = getUnitAllAction(forUnitMap, u);
                foreach (Action act in uActionList)
                {
                    Map lvUnitActMap = lowVisibility(forUnitMap, act, teamColor);     //行動後の視界不良マップを取得

                    //視界不良マップにおいて特殊な状況での勝率確定
                    {
                        int lvOpUnitNum = lvUnitActMap.getNumOfAliveColorUnits((teamColor + 1) % 2);    //視界不良内の敵影数
                        //敵影がない場所まで逃亡の場合
                        if (lvOpUnitNum == 0 && act.actionType != Action.ACTIONTYPE_MOVEANDATTACK)
                        {
                            act.X_actionEvaluationValue = NO_ENEMY;  //特殊な勝率に変更
                            continue;
                        }
                    }
                    //合法手の評価をモンテカルロで行う（要調整）
                    MCTS mcts = new MCTS(UNIT_PO, UNIT_DP); //po=100, depth=100
                    double bonus = act.actionType == Action.ACTIONTYPE_MOVEANDATTACK ? ATK_BONUS : 0.0;    //勝率に対する報酬
                    act.X_actionEvaluationValue = mcts.search(lvUnitActMap, u.getTeamColor()) + bonus;     //勝率を記録
                }
                //行動を良い順番に並び替える
                uActionList.Sort(compareActionByWR);
                tmpNPlanActionList.Add(uActionList);     //一時行動表に追加

                Logger.addLogMessage("\n    作戦" + n + " : ", teamColor);           //デバッグ
                Logger.addLogMessage("ユニット(" + u.getXpos() + ", " + u.getYpos() + 
                    ") : Wr = " + uActionList[0].X_actionEvaluationValue + Environment.NewLine, teamColor);           //デバッグ
            }
            #endregion

            #region Military Staff Thinking
            //作戦をまとめる
            //局面一覧表を作成
            //1次作戦では順番だけを考慮する（乱数順100局面）
            //n次作戦(n>1)では前作戦との配合率を考える
            //暫定(n:n-1)：1:3, 1:1, 3:1 それぞれ30,40,30局面(10x3,20x2)
            double winRate = -1.0;
            for (int i = 0; i < 100; i++)
            {
                Map testMap = map.createDeepClone();    //作戦調査するマップ
                List<Action> tmpFinalActionList = new List<Action>();
                Random rnd = new Random(unchecked(DateTime.Now.Ticks.GetHashCode() + tmpFinalActionList.GetHashCode()));

                //第1次作戦以降では、一定量の部隊が順番に行動する
                if (n > 1)
                {
                    List<Unit> testUnitList = testMap.getUnitsList(teamColor, false, true, false);    //testMap上の部隊
                    int unitNum;    //n-1次行動する部隊数

                    if (i < 30)                     unitNum = (int)Math.Round(((double)testUnitList.Count / 10.0) * 7.0);   //n-1次作戦を7割採用
                    else if (i >= 30 && i < 60)     unitNum = (int)Math.Round((double)testUnitList.Count / 2.0);            //n-1次作戦を5割採用
                    else                            unitNum = (int)Math.Round(((double)testUnitList.Count / 10.0) * 3.0);   //n-1次作戦を3割採用

                    //部隊数の乱数表作成
                    List<int> randList = new List<int>();
                    for (int j = 0; j < testUnitList.Count; j++)
                    {
                        randList.Add(j);
                    }

                    for (int j = unitNum; j < testUnitList.Count; j++)
                    {
                        randList.RemoveAt(rnd.Next(randList.Count));   //ランダムに削除
                    }

                    //n-1次作戦を乱雑部隊で実行、実行順は前回計画通り
                    for (int j = 0; j < randList.Count; j++)
                    {
                        //行動させる
                        if (!executeAction(ref testMap, nPlanActionList[n - 1][randList[j]]))
                        {
                            //失敗する場合について考案中
                            Logger.addLogMessage(Environment.NewLine + "n-1乱雑_行動失敗" + Environment.NewLine, teamColor);
                            continue;
                        }
                        tmpFinalActionList.Add(nPlanActionList[n - 1][randList[j]]);    //行動記憶
                    }
                }

                //残り部隊は乱数順でn次作戦行動を取る
                List<Unit> nUnitList = testMap.getUnitsList(teamColor, false, true, false);     //残り部隊
                int nUnitListCount = nUnitList.Count;
                for (int j = 0; j < nUnitListCount; j++)
                {
                    int rndUnit = rnd.Next(nUnitList.Count);     //乱数取得

                    //部隊の行動計画検索
                    for (int unitIndex = 0; unitIndex < tmpNPlanActionList.Count; unitIndex++)
                    {
                        //ある部隊のn次行動計画を発見
                        if (nUnitList[rndUnit].getID() == tmpNPlanActionList[unitIndex][0].operationUnitId)
                        {
                            //行動問題解決
                            bool kaihiFlag = false;
                            for (int list = 0; list < tmpNPlanActionList[unitIndex].Count; list++)
                            {
                                //行動に問題ない場合
                                if (executeAction(ref testMap, tmpNPlanActionList[unitIndex][list]))
                                {
                                    kaihiFlag = true;
                                    tmpFinalActionList.Add(tmpNPlanActionList[unitIndex][list]);    //記憶
                                    break;
                                }
                            }

                            //問題回避できなかった場合（ほぼない）
                            if (kaihiFlag == false)
                            {
                                //とりあえずランダム行動させておく
                                Logger.addLogMessage(Environment.NewLine + "!!!衝突!!!" + Environment.NewLine, teamColor);   //衝突情報表示
                                List<Action> kaihiAct = getUnitAllAction(testMap, nUnitList[unitIndex]);    //合法手取得
                                int rndNum = rnd.Next(kaihiAct.Count);
                                testMap.executeAction(kaihiAct[rndNum]);
                                tmpFinalActionList.Add(kaihiAct[rndNum]);
                            }

                            nUnitList.RemoveAt(rndUnit);    //終わった部隊は消す
                            break;  //部隊検索終了
                        }
                    }
                }

                //MCTS
                MCTS mcts = new MCTS(STAFF_PO, STAFF_DP);
                double tmpWinRate = mcts.search(testMap, teamColor);    //想定開始

                //良い勝率を記録したら記憶する
                if (tmpWinRate > winRate)
                {
                    winRate = tmpWinRate;
                    nPlanActionList[n] = new List<Action>();
                    for (int j = 0; j < tmpFinalActionList.Count; j++)
                    {
                        nPlanActionList[n].Add(tmpFinalActionList[j].createDeepClone());
                    }
                }
            }
            #endregion

            Logger.addLogMessage(Environment.NewLine + "作戦" + n + " : ", teamColor);           //デバッグ
            Logger.addLogMessage("第" + n + "次作戦行動勝率：" + winRate + Environment.NewLine + Environment.NewLine, teamColor);    //デバッグ
        }

        /// <summary>
        /// 現在マップを視界不良状態にする
        /// </summary>
        /// <param name="map">元のマップ</param>
        /// <param name="u">視界元の行動</param>
        /// <returns>視界不良マップ</returns>
        private Map lowVisibility(Map map, Action baseAct, int teamColor)
        {
            //入れ替わりで攻撃してくる敵、味方の視界の取り方について考案中

            Map lvMap = map.createDeepClone();          //視界不良マップ
            Action tmpAct = baseAct.createDeepClone();  //一時的行動
            bool AtkFlag = false;                       //攻撃フラグ

            //攻撃行動の場合、まずは移動だけさせる
            if (baseAct.actionType == Action.ACTIONTYPE_MOVEANDATTACK)
            {
                AtkFlag = true;
                tmpAct.actionType = Action.ACTIONTYPE_MOVEONLY;
            }

            //行動させる
            if (!executeAction(ref lvMap, tmpAct))
            {
                //失敗時について考案中
                Logger.addLogMessage(Environment.NewLine + "視界_行動失敗" + Environment.NewLine, teamColor);
            }

            Unit u = lvMap.getUnit(baseAct.operationUnitId);  //行動元部隊取得

            //味方部隊
            List<Unit> lvMyUnits = lvMap.getUnitsList(u.getTeamColor(), true, true, false);
            //敵ユニットリスト
            List<Unit> lvOpUnits = lvMap.getUnitsList(u.getTeamColor(), false, false, true);

            //値保存領域確保
            u.initX_ints(ACTION_X);
            for (int i = 0; i < lvMyUnits.Count; i++) lvMyUnits[i].initX_ints(ACTION_X);
            for (int i = 0; i < lvOpUnits.Count; i++) lvOpUnits[i].initX_ints(ACTION_X);

            u.setX_int(VISIBLE_FLAG, -1);   //自分を視界に入れる

            //自部隊が攻撃可能な敵
            {
                //自部隊の攻撃行動リストを取得
                List<Action> myAtkList = RangeController.getAttackActionList(u, lvMap);
                foreach (Action act in myAtkList)
                {
                    //敵部隊と攻撃行動を照らし合わせ
                    for (int i = 0; i < lvOpUnits.Count; i++)
                    {
                        //自部隊が攻撃可能な敵に視界フラグを立てる
                        if (act.targetUnitId == lvOpUnits[i].getID())
                        {
                            lvOpUnits[i].setX_int(VISIBLE_FLAG, -1);
                            break;
                        }
                    }
                }
            }

            //自部隊に攻撃可能な敵
            {
                foreach (Unit opUnit in lvOpUnits)
                {
                    //敵部隊の攻撃行動リストを取得
                    List<Action> opAtkList = RangeController.getAttackActionList(opUnit, lvMap);
                    foreach (Action act in opAtkList)
                    {
                        //自部隊に攻撃可能な敵部隊がいれば、そいつに視界フラグを立てる
                        if (act.targetUnitId == u.getID())
                        {
                            opUnit.setX_int(VISIBLE_FLAG, -1);
                        }
                    }
                }
            }

            //視界内の敵に攻撃可能な味方
            {
                foreach (Unit myUnit in lvMyUnits)
                {
                    //援護部隊の攻撃行動リストを取得する
                    List<Action> spAtkList = RangeController.getAttackActionList(myUnit, lvMap);
                    //援護部隊の攻撃行動と敵部隊IDと視界情報を照らし合わせ
                    foreach (Action act in spAtkList)
                    {
                        for (int i = 0; i < lvOpUnits.Count; i++)
                        {
                            //援護部隊の攻撃先が自部隊の視界内なら、援護部隊を視界内に入れる
                            if (act.targetUnitId == lvOpUnits[i].getID() && lvOpUnits[i].getX_int(VISIBLE_FLAG) < 0)
                            {
                                myUnit.setX_int(VISIBLE_FLAG, -1);
                                break;
                            }
                        }
                    }
                }
            }

            foreach (Unit myUnit in lvMyUnits)
            {
                //視界内にない味方を削除
                if (!(myUnit.getX_int(VISIBLE_FLAG) < 0))
                {
                    lvMap.deleteUnit(myUnit);
                }
            }

            foreach (Unit opUnit in lvOpUnits)
            {
                //視界内にない敵を削除
                if (!(opUnit.getX_int(VISIBLE_FLAG) < 0))
                {
                    lvMap.deleteUnit(opUnit);
                }
            }

            //攻撃行動だった場合、攻撃させる
            if (AtkFlag == true)
            {
                //位置を戻す
                lvMap.changeUnitLocation(baseAct.fromXpos, baseAct.fromYpos, lvMap.getUnit(baseAct.operationUnitId));
                lvMap.getUnit(baseAct.operationUnitId).setActionFinished(false);    //行動前にする
                //実行
                if (!executeAction(ref lvMap, baseAct))
                {
                    //失敗時について考案中
                    Logger.addLogMessage(Environment.NewLine + "視界2_行動失敗" + Environment.NewLine, teamColor);
                }
            }

            //視界不良マップを返す
            return lvMap;
        }

        /// <summary>
        /// あるユニットの合法手を全て返す
        /// </summary>
        /// <param name="map">合法手を出すためのマップ</param>
        /// <param name="u">このユニットの合法手を返す</param>
        /// <returns>合法手リスト（攻撃行動が先に入る）</returns>
        private List<Action> getUnitAllAction(Map map, Unit u)
        {
            //まず攻撃行動リストを取得
            List<Action> allAction = RangeController.getAttackActionList(u, map);

            //移動行動リストを得る
            bool[,] movable = RangeController.getReachableCellsMatrix(u, map);  //移動可能範囲
            for (int x = 1; x < map.getXsize() - 1; x++)
            {
                for (int y = 1; y < map.getYsize() - 1; y++)
                {
                    //移動可能なら、そのアクションを作成
                    if (movable[x, y] == true)
                    {
                        allAction.Add(Action.createMoveOnlyAction(u, x, y));
                    }
                }
            }

            return allAction;
        }

        /// <summary>
        /// 途中停止させないようにする行動関数
        /// </summary>
        /// <param name="map"></param>
        /// <param name="act"></param>
        /// <returns></returns>
        private bool executeAction(ref Map map, Action act)
        {
            if (ActionChecker.isTheActionLegalMove_Silent(act, map))
            {
                map.executeAction(act);
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 降順で行動を勝率でソート
        /// </summary>
        private static int compareActionByWR(Action a, Action b)
        {
            if (a.X_actionEvaluationValue - b.X_actionEvaluationValue > 0)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// 部隊並び替え用
        /// </summary>
        private static int compareActionByUnitID(Action a, Action b)
        {
            return a.operationUnitId - b.operationUnitId;
        }

    }   //Class end

    /// <summary>
    /// モンテカルロ探索を行うクラス
    /// </summary>
    class MCTS
    {
        private int playout;    //プレイアウト回数
        private int poDepth;    //プレイアウトの深さ
        Random rnd;

        //コンストラクタ
        public MCTS()
        {
            playout = 100;
            poDepth = 100;
            //乱数種
            rnd = new Random(unchecked(DateTime.Now.Ticks.GetHashCode() + this.GetHashCode()));
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="po">プレイアウト回数</param>
        /// <param name="pd">プレイアウトの深さ</param>
        public MCTS(int po, int pd)
        {
            playout = po;
            poDepth = pd;
            //乱数種
            rnd = new Random(unchecked(DateTime.Now.Ticks.GetHashCode() + this.GetHashCode()));
        }

        /// <summary>
        /// 探索実行
        /// </summary>
        /// <param name="map">探索を行うマップ</param>
        /// <param name="teamColor">現在手番</param>
        /// <returns>勝率を返す</returns>
        public double search(Map map, int teamColor)
        {
            int winNum = 0;

            //指定回数プレイアウトする
            Parallel.For(0, playout, i => {
                if(getPlayOut(map, teamColor) > 0)
                {
                    winNum++;   //勝った回数を記録
                }
            });

            return (double)winNum / playout;
        }

        /// <summary>
        /// 一回のプレイアウトを取得する
        /// </summary>
        /// <param name="map">対象マップ</param>
        /// <param name="teamColor">現在手番</param>
        private int getPlayOut(Map map, int teamColor)
        {
            Map rndMap = map.createDeepClone();  //ランダム実行用マップ
            int color = teamColor;              //手番部隊

            for (int i = 0; i < poDepth; i++)
            {
                //自チームがいないなら敗北
                if (map.getNumOfAliveColorUnits(teamColor) == 0)
                {
                    return -1;
                }
                //相手チームがいないなら勝利
                else if(map.getNumOfAliveColorUnits((teamColor + 1) % 2) == 0)
                {
                    return 1;
                }

                //行動前部隊リストを取得
                List<Unit> uList = rndMap.getUnitsList(color, false, true, false);
                
                //部隊を乱雑に行動させる
                int unitNum = uList.Count;
                for (int n = 0; n < unitNum; n++)
                {
                    //乱数取得
                    int rndNum = rnd.Next(uList.Count);
                    //部隊の合法手取得
                    List<Action> actList = getUnitMCAction(rndMap, uList[rndNum]);

                    //乱数取得
                    int rndNum2 = rnd.Next(actList.Count);
                    //合法手を実行
                    rndMap.executeAction(actList[rndNum2]);

                    //行動した部隊を削除
                    uList.RemoveAt(rndNum);
                }

                //手番変更
                color = (color + 1) % 2;
                rndMap.enableUnitsAction(color);    //変更側手番を行動可能にする
            }

            //設定深さで決着がついていない場合
            List<Unit> myUnits = rndMap.getUnitsList(teamColor, true, true, false);
            List<Unit> opUnits = rndMap.getUnitsList(teamColor, false, false, true);
            int myHP = 0;
            int opHP = 0;
            foreach (Unit myUnit in myUnits)
            {
                int bonus = myUnit.getTypeOfUnit() == 5 ? 1 : 5;
                myHP += myUnit.getHP() * bonus;
            }
            foreach (Unit opUnit in opUnits)
            {
                int bonus = opUnit.getTypeOfUnit() == 5 ? 1 : 5;
                opHP += opUnit.getHP() * bonus;
            }

            //体力総量の多い方が勝利
            return (myHP - opHP) > 0 ? 1 : -1;
        }

        /// <summary>
        /// ある部隊のモンテカルロ探索専用の合法手リストを得る
        /// </summary>
        /// <param name="map">対象マップ</param>
        /// <param name="u">対象部隊</param>
        /// <returns>合法手リストを返す</returns>
        private List<Action> getUnitMCAction(Map map, Unit u)
        {
            //攻撃行動を部隊行動リストに追加
            List<Action> unitMCActionList = RangeController.getAttackActionList(u, map);

            //攻撃行動がない場合
            if (unitMCActionList.Count <= 0)
            {
                //一番近い敵を探す
                Unit nearestEnemy = getNearestEnemy(map, u);
                //近い敵と自分との間における移動命令を合法手に追加
                int minX = Math.Min(u.getXpos(), nearestEnemy.getXpos());
                int maxX = Math.Max(u.getXpos(), nearestEnemy.getXpos());
                int minY = Math.Min(u.getYpos(), nearestEnemy.getYpos());
                int maxY = Math.Max(u.getYpos(), nearestEnemy.getYpos());

                //移動範囲を取得
                bool[,] movable = RangeController.getReachableCellsMatrix(u, map);
                //近くに移動する手を合法手とする
                for(int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        if (movable[x, y] == true)
                        {
                            unitMCActionList.Add(Action.createMoveOnlyAction(u, x, y));
                        }
                    }
                }
            }

            return unitMCActionList;
        }

        /// <summary>
        /// ある部隊に最も近い敵を探す
        /// </summary>
        /// <param name="map">対象マップ</param>
        /// <param name="u">探索元部隊</param>
        /// <returns>敵部隊</returns>
        private Unit getNearestEnemy(Map map, Unit u)
        {
            Unit nearestEnemy = new Unit();                       //近い敵
            int mDistance = map.getXsize() + map.getYsize();      //近い敵とのマンハッタン距離

            //全敵リストを取得
            foreach (Unit enemy in map.getUnitsList(u.getTeamColor(), false, false, true))
            {
                //各敵とのマンハッタン距離を取得
                int subDis = Math.Abs(u.getXpos() - enemy.getXpos()) + Math.Abs(u.getYpos() - enemy.getYpos());
                //最も近い物を記憶
                if (subDis < mDistance)
                {
                    mDistance = subDis;
                    nearestEnemy = enemy;
                }
            }

            return nearestEnemy;
        }
    }

}   //name end
