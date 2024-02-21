using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars 
{
    /// <summary>
    /// 移動可能範囲，攻撃可能ユニットなどを取得できるクラス．
  /// システムも使うが，思考ルーチンでの利用も可能．
  /// AI_template や AI_Sample_MaxActEval もご覧ください
    /// </summary>
    class SAT_RangeController {


        //　##上下左右の方角コントロール系．藤木が実装した物とかなりルールが違うので注意が必要．
        //　##ちなみにCOMPASS列挙型は「SatQuickUnit_Aux.cs」にて定義されている．
        
        // 上下左右を表す，座標差分の定数配列  　　　 [_]  ,[E],[N],[W],[S]
        private static int[] CompassDX = new int[5] { -9999, +1, 0, -1, 0 };
        private static int[] CompassDY = new int[5] { -9999, 0, -1, 0, +1 };
        public static COMPASS[] REVERSE_DIR = new COMPASS[] { COMPASS._, COMPASS.W, COMPASS.S, COMPASS.E, COMPASS.N };

        public static COMPASS MIN_COMPASS_DIR = COMPASS.E;
        public static COMPASS MAX_COMPASS_DIR = COMPASS.S;
        public static int MIN_COMPASS_IND = 1;
        public static int MAX_COMPASS_IND = 4;





        // opUnitの全行動のリストを返す
        public static List<int> myGetAllActionList(SatQuickUnit opUnit, SatQuickMap map) {
            List<int> actions_atk = myGetAttackActionList(opUnit, map);
            List<int> actions_move = myGetMoveActionList(opUnit, map);
            actions_atk.AddRange(actions_move);
            return actions_atk;
        }


        // opUnitの攻撃行動のリストを返す
        public static List<int> myGetAttackActionList(SatQuickUnit opUnit, SatQuickMap map) {
            List<int> actions = new List<int>();

            int turnCnt = map.getTurnCount();
            if (opUnit.getSpec().isDirectAttackType() == true) {  // opUnitが近接攻撃タイプだった場合

            foreach (SatQuickUnit enUnit in map.teamUnits[SAT_FUNC.REVERSE_COLOR(opUnit.getTeamColor())]){
                if (enUnit == null || enUnit.Dead) { continue; }
                    int posX = enUnit.getXpos();// 敵ユニットの位置
                    int posY = enUnit.getYpos();

                    if (opUnit.routeMap_step[turnCnt][posX, posY] < 0) { continue; } //到達不能場所にいる
                    if (!isEffective(opUnit, enUnit)) { continue; } //攻撃効果なしユニットタイプ
                    
                        
                    //敵ユニットの四方をなぞって，攻撃行動を追加
                    for (int dir = MIN_COMPASS_IND; dir <= MAX_COMPASS_IND; dir++) {
                        int adjacentX = posX + CompassDX[dir];
                        int adjacentY = posY + CompassDY[dir];
                        
                        if (opUnit.routeMap_step[turnCnt][adjacentX, adjacentY] <= 0) {continue;}//移動到達不可
                        if (map.getMapUnit()[adjacentX, adjacentY] != null && //自分ではない味方ユニットが塞いでる
                            !(adjacentX == opUnit.getXpos() && adjacentY == opUnit.getYpos())) { continue; }
                    
                        actions.Add(SatQuickAction.createAttackAction(opUnit, adjacentX, adjacentY, enUnit));
                    }
                }                
            }
            else { // 間接攻撃タイプだった場合
                Spec uSpec = opUnit.getSpec();
                // 敵ユニットが攻撃可能範囲に入っているか否か見る
                foreach (SatQuickUnit enUnit in map.getUnitsList(opUnit.getTeamColor(), false, false, true)) {
                    int posX = enUnit.getXpos();// 敵ユニットの位置
                    int posY = enUnit.getYpos();
                    int dist = Math.Abs(posX - opUnit.getXpos()) + Math.Abs(posY - opUnit.getYpos()); //敵ユニットと自分との距離
                    if (uSpec.getUnitMinAttackRange() <= dist && dist <= uSpec.getUnitMaxAttackRange()) {
                        if (!isEffective(opUnit, enUnit)) { continue; }
                        actions.Add(SatQuickAction.createAttackAction(opUnit, opUnit.getXpos(), opUnit.getYpos(), enUnit));// 攻撃可能範囲に含まれるならリストに追加する
                    }
                }
            }
            
            return actions;
        }
        


        /// <summary>
        /// Return all the move actions for the unit
        /// </summary>
        /// <param name="map"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static List<int> myGetMoveActionList(SatQuickUnit unit, SatQuickMap map) {
            List<int> moveActions = new List<int>();
            int turnCnt = map.getTurnCount();

            //自分のマスは移動可能 ##これ入れておかないと，SatAIクラスは，ユニットが移動できない時バグる．
            moveActions.Add(SatQuickAction.createMoveOnlyAction(unit, unit.getXpos(), unit.getYpos()));
             

            //変数保持
            int[,] unitStepMap = unit.routeMap_step[turnCnt];
            int uni_step = unit.getSpec().getUnitStep();
            int curX = unit.getXpos();
            int curY = unit.getYpos();
            
            //走査域の計算
            int yMin = Math.Max(1, curY - uni_step);
            int yMax = Math.Min(map.getYsize()-2, curY + uni_step);

            for (int y = yMin; y <= yMax; y++) {

                int xMin = Math.Max(1, curX - (uni_step - Math.Abs(y - curY)));
                int xMax = Math.Min(map.getXsize()-2, curX + (uni_step - Math.Abs(y - curY)));
                for (int x = xMin; x <= xMax; x++) {
                    if (unitStepMap[x, y] > 0) {
                        if (map.getMapUnit()[x, y] != null) { continue; }   //味方ユニットが上にいる
                        moveActions.Add(SatQuickAction.createMoveOnlyAction(unit,x,y));
                    }
                }
            }
            return moveActions;
        }

        // operationUnitがtargetUnitに攻撃可能かどうか．ダメージ効果0の場合にはfalseが返る
        private static bool isEffective(SatQuickUnit operationUnit, SatQuickUnit targetUnit) {
            if (operationUnit.getSpec().getUnitAtkPower(targetUnit.getTypeOfUnit()) != 0) { return true; }
            return false;
        }
    }
}
