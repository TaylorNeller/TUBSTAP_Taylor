// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;

// namespace SimpleWars {

//     /// <summary>
//     ///  ****  Sato AI **** 
//     ///  * Min-Max Tree search with several pruning techniques.
//     ///  *
//     /// </summary>
//     class AI_SatTS_D2 : Player {

//         private const int MY_INF_VALUE = 10000; //無限大

//         public static readonly int NODE_COUNT_LIMITATION = 100000; //1回のmakeAction()あたりの探索ノード数がこれを超えたら打切り
//         public static int nodeCnt = 0;

//         //Information about TeamColor. ターン開始時にちゃんとした値が格納される
//         private int teamColorLocal = -2;

//         //木探索で計算した最善手を保存しておく変数．
//         private Action selectedActHolder = null;


//         // AIの表示名（必須のpublic関数）
//         public string getName() {
//             return "Sat DepthFirst";
//         }

//         // パラメータ等の情報を返す関数
//         public string showParameters() {
//             return getName();
//         }

//         // 1ユニットの行動を決定する（必須のpublic関数）
//         // v1.06からは引数が4つに増えています
//         public Action makeAction(Map map, int teamColor, bool turnStart, bool gameStart) {
//             nodeCnt = 0;
//             if (gameStart) { init_GameStart(map, teamColor); }
//             if (turnStart) { init_TurnStart(map, teamColor); }
            
//             //敵が1体もいなくなってたらターンエンド（※不要かもしれないが，バグの発生を予防するため）
//             if (map.getNumOfAliveColorUnits(SAT_FUNC.REVERSE_COLOR(teamColor)) == 0) {
//                 return Action.createTurnEndAction();
//             }

//             Action act_decided = depthFirstSearch_main(map.createDeepClone());

//             if (act_decided == null) { act_decided =  Action.createTurnEndAction(); }//For Debug.           
//             Console.Write("Calculated. nodes(" + nodeCnt + ")" + act_decided.toString());
//             Logger.addLogMessage("Calculated. nodes(" + nodeCnt + ")" + act_decided.toString(), teamColor); 
//            return act_decided;
//         }



//         /// <summary>
//         /// 木探索メイン　副メソッドで算出した最善行動はグローバル変数「selectedActHolder」経由で受け取る
//         /// </summary>
//         private Action depthFirstSearch_main(Map map_parent) {
//             selectedActHolder = null;

//             Queue<int> unitIDQue_forward = SAT_FUNC.makeUnitQ_forward(map_parent, teamColorLocal);
//             Queue<int> unitIDQue_backward = SAT_FUNC.makeUnitQ_reverse(map_parent, teamColorLocal);


//             int alpha = DFSFrined_aux(map_parent, unitIDQue_forward, true, -MY_INF_VALUE);
//             if (nodeCnt > NODE_COUNT_LIMITATION / 2) { goto OMIT_THE_BACKWARD_SEARCH;}

//             DFSFrined_aux(map_parent, unitIDQue_backward, true, alpha);

//             OMIT_THE_BACKWARD_SEARCH: /* Label. */

//             return selectedActHolder;
//         }

//         /// <summary>
//         /// 味方ターン読み用の深さ優先探索部
//         /// </summary>
//         public int DFSFrined_aux(Map map_parent, Queue<int> unitIDs, bool isFirstMove, int alpha) {
//             nodeCnt++;
//             //All friend has moved. 
//             if (unitIDs.Count == 0) {
//                 return DFSEnemy_aux(map_parent, alpha);
//             }

//             //Terminal. All Enemy has died.
//             if (map_parent.getNumOfAliveColorUnits(SAT_FUNC.REVERSE_COLOR(teamColorLocal)) == 0) {
//                 return MY_INF_VALUE;
//             }
//             //Terminal. All Frined has died.
//             if (map_parent.getNumOfAliveColorUnits(teamColorLocal) == 0) {
//                 return -MY_INF_VALUE;
//             }

//             int maxEval = alpha;

//             //Deep Copy
//             Queue<int> unitIDQueCopied = new Queue<int>(unitIDs);
//             int movingUnitID = unitIDQueCopied.Dequeue();
//             Unit movingUnit = map_parent.getUnit(movingUnitID);
//             List<Action> moves = suggestMoves(map_parent, movingUnit);
            
//             foreach (Action move in moves) {
//                 Map map_child = map_parent.createDeepClone();
//                 map_child.executeAction(move);
//                 int eval = DFSFrined_aux(map_child, unitIDQueCopied, false, alpha);
//                 if (eval > maxEval) {
//                     maxEval = eval;
//                     alpha = maxEval;
//                     if (isFirstMove) {
//                         selectedActHolder = move;
//                     }
//                 }
//             }
//             return maxEval;
//         }

//         /// <summary>
//         /// 　敵ターン読み用の関する深さ優先探索
//         /// </summary>
//         private int DFSEnemy_aux(Map map_parent, int alpha) {
//             nodeCnt++;
//             if (nodeCnt > NODE_COUNT_LIMITATION) {
//                 return -MY_INF_VALUE;
//             }
//             //Terminal. All Enemy has died.
//             if (map_parent.getNumOfAliveColorUnits(SAT_FUNC.REVERSE_COLOR(teamColorLocal)) == 0) {
//                 return MY_INF_VALUE;
//             }
//             //Terminal. All Frined has died.
//             if (map_parent.getNumOfAliveColorUnits(teamColorLocal) == 0) {
//                 return -MY_INF_VALUE;
//             }
            
//             int minEval = MY_INF_VALUE;
            
//             List<Unit> eneUnits = map_parent.getUnitsList(teamColorLocal, false, false, true);
//             List<Action> atkActs = AiTools.getAllAttackActions(SAT_FUNC.REVERSE_COLOR(teamColorLocal), map_parent);

//             // 　ここからの enemyUnitとactionに対する2重 Forループ　は何やってるかややこしいが，
//             // 　やりたい事としては　敵ユニット(ID1), (ID2), (ID3), (ID5)がいるとき 
//             //   ID1か何か攻撃（またはパス）→ ID2が攻撃　→　ID3が攻撃  → ID5が攻撃　→　局面評価関数による評価値算出
//             //   ・・・という事をやりたい．計算の爆発を防ぐためID2が攻撃したあとID1が攻撃する順序は許容しないし，
//             //   また，1つのユニットが1体の相手ユニットに2通り以上の攻撃を試みる事も防ぎ，1ユニット毎に行動着手は３つまでにする
//             foreach (Unit eneU in eneUnits) {
//                 if (eneU.isActionFinished() || eneU.getHP() <= 0) { continue; }
//                 if (atkActs.Count == 0) {
//                     return evaluate(map_parent.createDeepClone());
//                 }
//                 HashSet<int> alreadyAtked = new HashSet<int>();

//                 foreach (Action act in atkActs) {
//                     if (act.operationUnitId != eneU.getID()) { continue; }
//                     if(alreadyAtked.Contains(act.targetUnitId)){continue;}
//                     alreadyAtked.Add(act.targetUnitId);

//                     if (alreadyAtked.Count > 3) { break; }

//                     Map child = map_parent.createDeepClone();
//                     child.executeAction(act);
//                     int eval = DFSEnemy_aux(child, alpha);
//                     if (eval <= alpha) { //Alpha-Cut. 
//                         return eval;
//                     }
//                     if (eval < minEval) {
//                         minEval = eval;
//                     }
//                 }
//             }
//             return minEval;
//         }





//         //ユニット合法手．
//         private List<Action> suggestMoves(Map map, Unit unit_to_move) {
//             return SAT_FUNC.suggestMoves(map, unit_to_move, teamColorLocal);
//         }

//         //局面評価
//         private int evaluate(Map map) {
//             int eval_forTurnPlayer = SAT_FUNC.evalState_withAtkPrediction(map, teamColorLocal);
//             return eval_forTurnPlayer;
//         }

        
//         private void init_GameStart(Map map, int teamColor) {
//             teamColorLocal = teamColor;
//             // マップ上の全ての2点間の「道のり」を計算して保存しておく関数．            
//             SAT_RangeController.initRangeMap(map.getFieldTypeArray());
            
//         }

//         private void init_TurnStart(Map map, int teamColor) {
//             //GameStart時に一回格納すれば十分だが，思わぬバグを予防するため
//             teamColorLocal = teamColor;
//             SAT_RangeController.initEnemyMastery(map, teamColorLocal);           
            
//         }


//     }
// }
