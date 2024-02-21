using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {



    // Sato AI. 
    // Min-Max Tree search. Depth-2(or 3, in case there are few units in the map). 
    class AI_SatD2 : Player{

        //DEBUG flag (for print logs)
        private bool debug_ = false;
        //private bool debug_ = true;

        private bool testMode_ = false;
        //private bool testMode_ = true;

        //Measurement for Tree Search
        private int nodeCounter = 0;
        private System.Diagnostics.Stopwatch sw_local = new System.Diagnostics.Stopwatch();
        
        // 0 なら，1回行動して状態評価．１なら，2回行動（自-敵1回ずつ）して状態評価　
        const int DEFAULT_MAX_SEARCH_DEPTH = 1;

        const int MAX_ACTIVE_UNIT_friend = 5;
        const int MAX_ACTIVE_UNIT_enemy  = 20;


        //Information about TeamColor
        private int teamColorLocal = -2;
        
        //Store chain actions to execute.
        private int[] actionsToExecute = null;
        private int[] tmp_actionsToExecute = null;
        
        //For Map-Units Separation.
        private Queue<int> exeActionQueue = new Queue<int>();

        //Control Unit move orders. 
        private SAT_UNIT_MOVE_ORDER[] moveOrderOptions = new SAT_UNIT_MOVE_ORDER[]
                        //{ SAT_UNIT_MOVE_ORDER.FORWARD };
                        { SAT_UNIT_MOVE_ORDER.FORWARD, SAT_UNIT_MOVE_ORDER.REVERSE };
                        //{ SAT_UNIT_MOVE_ORDER.FORWARD , SAT_UNIT_MOVE_ORDER.REVERSE ,
                        // SAT_UNIT_MOVE_ORDER.CUT_FORWARD, SAT_UNIT_MOVE_ORDER.CUT_REVERSE, };

        private bool atkPrune = false;
        private bool movePrune = false;

        
        //For test. After several initialization.
        public void test1(SatQuickMap map) {
            map.printMap_veryDetailed();
            //SAT_FUNC.PRINT_MAP(map);
            //testStateEval(map);
            //testTreeSearchD2(map);
            testSuggestMoves(map);
            //SAT_FUNC.WriteLine(u.getRouteMapString());
            //SAT_FUNC.WriteLine(u.getRouteMapString());
        }

        // AIの表示名（必須のpublic関数）
        public string getName() {
            return "SatD"+DEFAULT_MAX_SEARCH_DEPTH +"_Order"+moveOrderOptions.Length + "_atkPrn_"+atkPrune+"_mvPrn_"+movePrune+
                "_ActiveLimits("+MAX_ACTIVE_UNIT_friend+"_"+MAX_ACTIVE_UNIT_enemy+")";
        }

        // パラメータ等の情報を返す関数
        public string showParameters() {
            return "";
        }

        private void init_GameStart(Map map, int teamColor) {
            teamColorLocal = teamColor;
            SAT_FUNC2.initRangeMap(map.getFieldTypeArray());
           
        }

        private void init_TurnStart(Map map, int teamColor) {
            nodeCounter = 0;
            exeActionQueue.Clear();
        }



        private int decideMaxTurn_forSearch(int num_frineds_inPartial, int num_enemies_inPartial) {            
            /*if (num_frineds_inPartial >= 4) { return 1; }
            if (num_frineds_inPartial <= 3 || num_enemies_inPartial <= 3) {
                return 2;
            } 
            return 1;
             */
            return DEFAULT_MAX_SEARCH_DEPTH;
        }

        //敵のルートマップ動的更新用のフラグを立てるか？
        private bool decideReviseEnemyRouteMap(int currentTurn) {
            if (DEFAULT_MAX_SEARCH_DEPTH == 0) {
                return false;
            }
            if (currentTurn == 0) { return true; }//深さ2の探索で，深さ0の着手実行時はTrue.
            return false;
        }

        //自軍の2回目の行動(深さ2-3間)に備えて，または敵ユニの移動行動切り抜き用に，自軍ルートマップ更新フラグを立てるか？
        private bool decidePrepareFriendRouteMap_TurnChange(int currentTurn) {
            return false;   //深さ2以下の探索（自軍が2回行動しない）なら，ここはずっとFalseで良い．
        }

        private SatQuickMap INIT_QUICK_MAP(Map map) {
            int countFri = map.getUnitsList(teamColorLocal, false, true, false).Count;
            int countEne = map.getUnitsList(SAT_FUNC.REVERSE_COLOR(teamColorLocal), false, true, false).Count;
            SatQuickMap q_map;
            if (countFri > MAX_ACTIVE_UNIT_friend || countEne > MAX_ACTIVE_UNIT_enemy) {
                // HACK DUPULICATED.<<Construct SatQuickMap instance>> Though, compromise this inefficiency for now.
                int[] IDMask = SAT_FUNC2.makeIDMask(map, teamColorLocal, MAX_ACTIVE_UNIT_friend, MAX_ACTIVE_UNIT_enemy);
                q_map = SatQuickMap.mapToQuickMap(map, IDMask);
            } else {
                q_map = SatQuickMap.mapToQuickMap(map);
            }
            q_map.initMovableArea_AllUnit();
            return q_map;    
        }

        //ユニット合法手．
        private List<int> suggestMoves(SatQuickMap map, SatQuickUnit unit_to_move) {
            List<int> actList = new List<int>();

            if (map.getTurnCount() == 0) {
                //## ATTACK ##
                if (atkPrune) {
                    actList.AddRange(unit_to_move.suggest1AtkActsforEachEnemy());
                } else {
                    actList.AddRange(SAT_RangeController.myGetAttackActionList(unit_to_move, map));
                }
                //## MOVE ## 
                if (movePrune) {
                    int[] grav_xy = SAT_FUNC2.CALC_GRAVITY_POINT_ENEMY(map, unit_to_move.getTeamColor());
                    actList.AddRange(unit_to_move.suggestMeaningfulMoveActs(grav_xy[0], grav_xy[1]));
                } else {
                    actList.AddRange(SAT_RangeController.myGetMoveActionList(unit_to_move, map));
                }
            } else if (map.getTurnCount() == 1) {
                actList.AddRange(unit_to_move.suggest1AtkActsforEachEnemy());
                if (actList.Count == 0) {
                    actList.Add(SatQuickAction.createMoveOnlyAction(
                            unit_to_move, unit_to_move.getXpos(), unit_to_move.getYpos()));
                }
            
            }

            return actList;

            /*  //深さごとに違う合法手生成をするとき用．今はまだ使わないのでコメントアウトしておく．
            switch (map.getTurnCount()) {
                case -1:
                    //int[] grav_xy = SAT_FUNC2.CALC_GRAVITY_POINT_ENEMY(map, unit_to_move.getTeamColor());
                    actList.AddRange(unit_to_move.suggest2AtkActsforEachEnemy());
                    actList.AddRange(unit_to_move.suggestMeaningfulMoveActs(grav_xy[0], grav_xy[1]));
                    break;
                case 0:
                    actList.AddRange(unit_to_move.suggest1AtkActsforEachEnemy());
                    if (actList.Count == 0) {
                        actList.Add(SatQuickAction.createMoveOnlyAction(
                                unit_to_move, unit_to_move.getXpos(), unit_to_move.getYpos()));
                    }
                    break;
            }
             */
            //return actList;
        }

        // 1ユニットの行動を決定する（必須のpublic関数）
        // v1.06からは引数が4つに増えています
        public Action makeAction(Map map, int teamColor, bool turnStart, bool gameStart) {
            
            if (gameStart) { init_GameStart(map, teamColor); }
            if (turnStart) { init_TurnStart(map, teamColor); }
            
            //Execute an action, if there is any planned action.
            if (exeActionQueue.Count > 0) { 
                //HACK ここで作ったアクションは「AttackX, Y」をアクセスされるとバグる．だが現仕様では参照されないハズ．
                int executeAct = exeActionQueue.Dequeue();
                SAT_FUNC.WriteLine("Output Action-" + SatQuickAction.toOneLineString(executeAct));
                return SatQuickAction.toAction(executeAct); 
            }

            //No Enemy Units, return Turn End. (Maybe, do not neede. But insert this line to prevent BUG, in case.)
            if (map.getNumOfAliveColorUnits(SAT_FUNC.REVERSE_COLOR(teamColorLocal)) == 0) {
                return Action.createTurnEndAction();
            }

            //##ActionQueueが空っぽ. 探索開始
            //######

            SatQuickMap q_map = INIT_QUICK_MAP(map);
            //testSuggestMoves(q_map);

            int moveUniSize = q_map.getUnitsList(SAT_FUNC.CALC_TURN_HOLDER(q_map), false, true, false).Count;
            actionsToExecute = new int[moveUniSize];
            tmp_actionsToExecute = new int[moveUniSize];

            sw_local.Start();

            int searchMaxTurn = decideMaxTurn_forSearch(q_map.getNumOfAliveColorUnits(q_map.phaseColor),
                q_map.getNumOfAliveColorUnits(SAT_FUNC.REVERSE_COLOR(q_map.phaseColor)));
            

            //### Test Code. ###
            if (testMode_) { test1(q_map); if (teamColor != 9876) { return Action.createTurnEndAction(); } }

            depthFirstSearch(q_map, searchMaxTurn);
            
            for (int act_i = 0; act_i < actionsToExecute.Length; act_i++) {
                int fixedAct = SatQuickAction.fixIDs(actionsToExecute[act_i], map, q_map);
                SAT_FUNC.WriteLine("ActionID-Fixed:"+SatQuickAction.toOneLineString(fixedAct));
                exeActionQueue.Enqueue(fixedAct);
            }
            
            //「一部ID Disabled」処理使用時は，先頭の要素のみをゲームシステムに出力する
            if (q_map.applyDisableProcedure) {
                exeActionQueue = SAT_FUNC.trancateQueue(0, 1, exeActionQueue);
            }

            sw_local.Stop();
            SAT_FUNC.WriteLine("Time(ms):" + (sw_local.ElapsedMilliseconds/1000)+"."
                +(sw_local.ElapsedMilliseconds%1000).ToString().PadLeft(3,'0'));
            SAT_FUNC.WriteLine("nodes:" + nodeCounter);
            sw_local.Reset();
            
            return makeAction(q_map,teamColorLocal,false,false);
            //return Action.createTurnEndAction();
        }



        public int depthFirstSearch(SatQuickMap map_parent, int max_turn) {
            return depthFirstSearch(map_parent, 0, 0, max_turn, 
                -SAT_CNST.MY_INF_VALUE, SAT_CNST.MY_INF_VALUE, SAT_UNIT_MOVE_ORDER.NULL);
        }


        public int depthFirstSearch(SatQuickMap map_parent, int num_MovedFriendUnits, 
                                    int currentTurn, int maxTurn, int alpha, int beta, 
                                    SAT_UNIT_MOVE_ORDER inheretMoveOrder) {
            if (debug_) {
                SAT_FUNC.Write("【TreeSearch Info】Turn:" + currentTurn + 
                    "/Max(" + maxTurn + ") numMvd:" + num_MovedFriendUnits);
                SAT_FUNC.WriteLine(", Alpha:" + alpha + ", beta:" + beta );
                SAT_FUNC.Write("【MAP Info】");
                SAT_FUNC.PRINT_MAP(map_parent);
            }
                        
            if (currentTurn > maxTurn ) {
                    nodeCounter++;
                    if (debug_) { SAT_FUNC.WriteLine("*** ReturnEvalHere:" + EvalState_turnHolder(map_parent)); }
                return EvalState_turnHolder(map_parent);
            }


            List<SatQuickUnit> units_unmoved = map_parent.getUnitsList(SAT_FUNC.CALC_TURN_HOLDER(map_parent), false, true, false);
            if (units_unmoved.Count == 0) { // => (All friend Units have been killed.)
                nodeCounter++;
                if (debug_) { SAT_FUNC.WriteLine("** ReturnEvalHere:" + EvalState_turnHolder(map_parent)); }
                return EvalState_turnHolder(map_parent);
            }

            int maxEval = alpha;

            // ## ユニット行動順（IDに関して順向き，逆向き）に関するループです
            // ##########
            for (int order_i = 0; order_i < moveOrderOptions.Length; order_i++) {
                //各手番最初じゃなければ現在セットされてる順序に従う（ので，他順序に関するループはスキップする）
                if (num_MovedFriendUnits > 0 && order_i > 0) { break; }
                //There is only one unit. (Thus, there's only one way to order units.)
                if (units_unmoved.Count == 1 && order_i > 0) { break; }

                SAT_UNIT_MOVE_ORDER unitOrder;

                if (num_MovedFriendUnits == 0) {
                    unitOrder = moveOrderOptions[order_i];
                } else {
                    unitOrder = inheretMoveOrder;
                }

                SatQuickUnit unit_to_move = SELECT_MOVE_UNIT(units_unmoved, num_MovedFriendUnits, unitOrder);

                //List<Action> suggested_moves = RangeController.getAttackActionList(unit_to_move, map_parent);
                
                List<int> suggested_moves = suggestMoves(map_parent, unit_to_move);               

                foreach (int selected_act in suggested_moves) {
                    
                    if (debug_) { SAT_FUNC.WriteLine("ExeAct->:" + SatQuickAction.toOneLineString(selected_act)); }

                    bool reviseEnemyRouteMap = decideReviseEnemyRouteMap(currentTurn);

                    EXE_ACTION(map_parent, selected_act, reviseEnemyRouteMap);
                    bool changeTurn = map_parent.isAllUnitActionFinished(map_parent.phaseColor);
                    if (changeTurn) { TURN_CHANGE(map_parent, decidePrepareFriendRouteMap_TurnChange(currentTurn)); }

                    int childEval;
                    if (changeTurn) {
                        childEval = -depthFirstSearch(map_parent, 0, currentTurn + 1, maxTurn, -beta, -alpha, unitOrder);
                        map_parent.backTurnChangeAct();  //If change the turn, back moves twice. 
                    }
                    else {
                        childEval = depthFirstSearch(map_parent, num_MovedFriendUnits + 1, 
                            currentTurn, maxTurn, alpha, beta, unitOrder);
                    }
                    map_parent.backAtkMoveAct(reviseEnemyRouteMap);
                    
                    if (childEval > maxEval) {
                        maxEval = childEval;
                        if (currentTurn == 0) { planTheAction(selected_act, num_MovedFriendUnits); }
                        alpha = Math.Max(alpha, maxEval);
                        if (maxEval >= beta) {
                            if (debug_) { SAT_FUNC.WriteLine("moved:" + num_MovedFriendUnits + ";BETA-CUT:" + maxEval
                                + "(<= "+beta+")"); }
                            return maxEval;
                        }
                    }
                }
            }

            if (debug_) { SAT_FUNC.WriteLine("moved:" + num_MovedFriendUnits + ", EV:" + maxEval); }
            return maxEval;
        }


        private void TURN_CHANGE(SatQuickMap map, bool prepareFriRouteMap) {
            map.exeAction_quick_TurnChange(SatQuickAction.createTurnEndAction(), prepareFriRouteMap);
        }

        private void EXE_ACTION(SatQuickMap map, int act, bool reviseEneRouteMap) {
            map.exeAction_quick_MoveAtk(act,reviseEneRouteMap);
        }

        private int EvalState_turnHolder(SatQuickMap map) {
            //int eval_forTurnPlayer = SAT_FUNC2.EVAL_MAP_HP(map, SAT_FUNC.CALC_TURN_HOLDER(map));
            int eval_forTurnPlayer = SAT_FUNC2.EVAL_STATE_BY_ATTACK_SIMULATION(map, map.phaseColor);
            
            return eval_forTurnPlayer;
        }




        

        

        //decide(ActionX , 2) -> execute the actionX at the third of the turn.
        private void planTheAction(int act, int order) {
            SAT_FUNC.WriteLine("【Plan-Action】Order" + order + "," + SatQuickAction.toOneLineString(act));
            tmp_actionsToExecute[order] = act;
            if (order == 0) {
                Array.Copy(tmp_actionsToExecute, actionsToExecute, actionsToExecute.Length);
            }
        }

#region TestCodes

        private void testSuggestMoves(SatQuickMap map) {
            List<SatQuickUnit> list_u = map.getUnitsList(map.phaseColor, false, true, false);
            foreach (SatQuickUnit u in list_u) {
                SAT_FUNC.WriteLine("Unit "+u.toString());
                SAT_FUNC.PRINT_ACTIONS(suggestMoves(map, u));            
            }
        }

        private void testGravityPoint(SatQuickMap map) {
            int[] grav_xy = SAT_FUNC2.CALC_GRAVITY_POINT_ENEMY(map, teamColorLocal);
            Console.WriteLine("GRAV_ENEMYx{0}-y{1}", grav_xy[0], grav_xy[1]);
        }


       

        private void testTreeSearchD2(SatQuickMap map) {
            int eval = depthFirstSearch(/*QuickMap*/ map,  /*Num_movedUnits*/ 0 , /*CurrentTurn*/ 0,  
                                        /*MaxTurn*/ DEFAULT_MAX_SEARCH_DEPTH,
                                        /*Alpha*/ -SAT_CNST.MY_INF_VALUE, 
                                        /*Beta*/ SAT_CNST.MY_INF_VALUE, SAT_UNIT_MOVE_ORDER.NULL);
            SAT_FUNC.WriteLine("eval:" + eval);
            SAT_FUNC.WriteLine("nodes:" + nodeCounter);
            sw_local.Stop();
            SAT_FUNC.WriteLine("Time(ms):" + (sw_local.ElapsedMilliseconds / 1000) + "."
                + (sw_local.ElapsedMilliseconds % 1000).ToString().PadLeft(3, '0'));
            sw_local.Reset();
            SAT_FUNC.PRINT_ACTIONS(actionsToExecute);
            
            return;
        }


        

        private void testStateEval(SatQuickMap map_arg) {
            //int eval = SAT_FUNC.EVAL_MAP_HP(map_arg, teamColorLocal);
            int eval = EvalState_turnHolder(map_arg);
            SAT_FUNC.WriteLine("eval:" + eval);
            return;
        }
        private void testTurnHolder(SatQuickMap map) {
            int turnHolder = map.phaseColor;
            String str = (turnHolder == Consts.BLUE_TEAM) ? "BLUE" : "RED";
            SAT_FUNC.WriteLine("turn:" + str);
            return; 
        }
#endregion


        //ユニット行動順序制御用配列．　int[総味方ユニット数][現在行動済み味方ユニ数] => アクセスすべきunmovedUnitリストindex
        private static int[][] ORDER_PICK_CUT_FORWARD = new int[][]{  new int[]{},
            new int[]{},            new int[]{},            new int[]{1,1,0},            new int[]{2,2,0,0},    
            new int[]{2,2,2,0,0},   new int[]{3,3,3,0,0,0}, new int[] {3,3,3,3,0,0,0},   new int[]{4,4,4,4,0,0,0,0}
        };
        private static int[][] ORDER_PICK_CUT_REVERSE = new int[][]{  new int[]{},
            new int[]{},            new int[]{},            new int[]{1,0,0},            new int[]{1,0,1,0},    
            new int[]{2,1,0,1,0},   new int[]{2,1,0,2,1,0}, new int[] {3,2,1,0,2,1,0},   new int[]{3,2,1,0,3,2,1,0}
        };
        private SatQuickUnit SELECT_MOVE_UNIT(List<SatQuickUnit> units_unmoved,
           int movedFriendNum, SAT_UNIT_MOVE_ORDER order) {
            if (order == SAT_UNIT_MOVE_ORDER.FORWARD) {
                return units_unmoved.ElementAt(0);
            }
            if (order == SAT_UNIT_MOVE_ORDER.REVERSE) {
                return units_unmoved.ElementAt(units_unmoved.Count - 1);
            }
            //総味方ユニット数
            int maxFrinedNum = units_unmoved.Count + movedFriendNum;
            if (maxFrinedNum <= 2) {
                return units_unmoved.ElementAt(0);
            }
            if (order == SAT_UNIT_MOVE_ORDER.CUT_FORWARD) {
                return units_unmoved.ElementAt(ORDER_PICK_CUT_FORWARD[maxFrinedNum][movedFriendNum]);
            }
            if (order == SAT_UNIT_MOVE_ORDER.CUT_REVERSE) {
                return units_unmoved.ElementAt(ORDER_PICK_CUT_REVERSE[maxFrinedNum][movedFriendNum]);            
            }
            Console.WriteLine("SELECT_MOVE_UNIT !!ERROR!! o" + order.ToString() + "moved" + movedFriendNum);
            return null; //ERROR
        }
    }

    enum SAT_UNIT_MOVE_ORDER {
        NULL,
        FORWARD,
        REVERSE,
        CUT_FORWARD,
        CUT_REVERSE,
    }
}
