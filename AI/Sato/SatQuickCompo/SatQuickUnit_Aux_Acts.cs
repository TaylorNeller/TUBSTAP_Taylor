using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {

    /// <summary>
    /// SatQuickUnitクラスの分割定義．
    /// こちらにはユニットの合法手提案に関するメソッドが並ぶ
    /// 
    /// - public suggestMeaningfulMoveActs()
    /// - public suggest2AtkActsForEachEnemy()
    /// - public suggest1AtkActsForEachEnemy()
    /// - public suggestMostApproachingMoveOnlyAct()
    /// 
    /// </summary>
    partial class SatQuickUnit : Unit {



        //移動手の提案を行う．                       [敵重心x]   [敵重心y]
        public List<int> suggestMeaningfulMoveActs(int gravX, int gravY) {
            //行動格納用の基礎的な変数の定義
            List<int> selectedActs　= new List<int>();
            //   <[EnemyRangeFlag], [Action]> 
            Dictionary<int, int> movDict = new Dictionary<int, int>();
            //   <[EnemyRangeFlag], [Action_Value]> 
            Dictionary<int, int> movValueDict = new Dictionary<int, int>();
            int turnCnt = map.getTurnCount();
            
            //マップの走査に必要な変数の計算
            int uni_step = fSpec.getUnitStep();
            int curX = fXpos;
            int curY = fYpos;
            int yMin = Math.Max(1, curY - uni_step);
            int yMax = Math.Min(map.getYsize() - 2, curY + uni_step);

            //現在地から動かない行動の辞書登録（※これがないと，どこにも移動できない状況でバグる）
            int dontMove = SatQuickAction.createMoveOnlyAction(this, curX, curY);
            int dontMoveValue = calcMoveValue(curX, curY, gravX, gravY);
            int dontMoveRangeFlag = calcEnemyRangeFlag(curX, curY, turnCnt);
            movDict.Add(dontMoveRangeFlag, dontMove);
            movValueDict.Add(dontMoveRangeFlag, dontMoveValue);

            //走査開始　（xMinとxMaxは，走査必要のあるＸ範囲をＹ値に応じて計算してる）
            for (int y = yMin; y <= yMax; y++) {
                int xMin = Math.Max(1, curX - (uni_step - Math.Abs(y - curY)));
                int xMax = Math.Min(map.getXsize() - 2, curX + (uni_step - Math.Abs(y - curY)));
                for (int x = xMin; x <= xMax; x++) {
                    
                    //そもそも移動できない場所を弾く．
                    if (routeMap_step[turnCnt][x, y] <= 0) { continue; }
                    if (map.getMapUnit()[x, y] != null) { continue; }

                    //敵制空権フラグを計算
                    int rangeFlag = calcEnemyRangeFlag(x, y, turnCnt);
 
                    //その敵制空権フラグの行動が辞書にまだ無ければ登録()
                    if (!movDict.ContainsKey(rangeFlag)) {
                        int moveValue = calcMoveValue(x, y, gravX, gravY);
                        movDict.Add(rangeFlag, SatQuickAction.createMoveOnlyAction(this, x, y));
                        movValueDict.Add(rangeFlag, moveValue);
                        continue;
                    }

                    //その敵制空権フラグの行動が辞書にあれば価値を比較．適宜入れ替え．
                    int oldValue = movValueDict[rangeFlag];
                    int newValue = calcMoveValue(x, y, gravX, gravY);
                    if (newValue > oldValue) {
                        movDict[rangeFlag] =  SatQuickAction.createMoveOnlyAction(this, x, y);
                        movValueDict[rangeFlag] = newValue;
                    }
                }
            }

            foreach (int act in movDict.Values) {
                selectedActs.Add(act);
            }           
            return selectedActs;

        }

        //移動行動にスコア付
        private int calcMoveValue(int destX, int destY, int gravX, int gravY) {
            int moveValue = 0;
            int V1 = 0, V2 = 10, V3 = 1;//各評価項目の重み．まずは適当に決めた数値で．
            int adjacentEnemies = 0; //Cannon評価用

            //##　現在OFF　## (1). 味方脆弱な味方の補強，にスコア．[隣接した行動済み味方ユニット] * V1 点
            /*
            for(int dir = MIN_COMPASS_IND; dir <= MAX_COMPASS_IND; dir++){
                Unit friendU = map.getMapUnit()[destX + CompassDX[dir], destY + CompassDY[dir]];
                if (friendU == null) { continue; }
                if (friendU.getTeamColor() != fTeamColor) { //味方じゃなくて敵．
                    adjacentEnemies++; 
                    continue; 
                }
                if (friendU.getID() == fId) { continue; }//移動前の自分と隣接しててもしょうがない．
                movethe concrete processValue += V1;
            }
            */

            // (2). 地形ボーナス．　[地形防御効果] * V2 点
            if (!IsFlyer) {
                moveValue += V2 * map.getFieldDefensiveEffect(destX, destY);
            }

            // (3). 敵重心との距離近さボーナス．　[9 - 重心とのマンハッタン距離] * V3 点
            int manhattan_dist = SAT_FUNC.MANHATTAN_DIST(destX, destY, gravX, gravY);
            moveValue += V3 * (9 - manhattan_dist);

            // (4). キャノンなら，隣接した敵が少ないほどボーナス． -1 * [隣接した敵数] * 5 * V2　点．重みは適当．
            if (IsCannon) {
                moveValue -= 5 * V2 * adjacentEnemies;    
            }
            return moveValue;
        }

        private int calcEnemyRangeFlag(int x, int y, int turnCnt) {
            int rangeFlag = 0;
            
            SatQuickUnit[] unitArr = map.teamUnits[SAT_FUNC.REVERSE_COLOR(fTeamColor)];
            for (int i = 0; i < unitArr.Length; i++) {
                SatQuickUnit eneU = unitArr[i];
                if (eneU == null) { continue; }
                if (eneU.Dead) { continue; }
                //このユニットにあまり威力をもたないユニットの場合は考慮から外す
                if (SAT_FUNC.isAlmostIneffective(eneU, this)) { continue; }
                if (eneU.routeMap_step[turnCnt][x, y] < 0) { continue; }

                rangeFlag = rangeFlag | SAT_CNST.POW2[eneU.getID()];
            }
            return rangeFlag;            
        }


        //敵1体に2手ずつ攻撃手の提案を行う．（地形が良いもの，敵と隣接してない物，をそれぞれ高スコア）
        public List<int> suggest2AtkActsforEachEnemy() {

            //FIXME　敵ごとにAtkの種類を分けて，つめる！
            List<int> selectedAct = new List<int>();

            List<int> allAtkActs = SAT_RangeController.myGetAttackActionList(this, map);
            //int tekinokazu toka?
 
            int numTotalUnits = map.getUnits().Length;
            int[] act_MaxStar = new int[numTotalUnits];
            int[] maxStar = Enumerable.Repeat<int>(-1, numTotalUnits).ToArray();
            int[] act_mostFarFromEnemies = new int[numTotalUnits]; ;
            int[] maxApartScore = Enumerable.Repeat<int>(-64, numTotalUnits).ToArray(); ;

            foreach (int act in allAtkActs) {
                int eneID = SatQuickAction.getTarID(act);

                int star = map.getFieldDefensiveEffect(SatQuickAction.getDestX(act),SatQuickAction.getDestY(act)); 
                if(IsFlyer){ star = -1;}
                if(star > maxStar[eneID]){
                    maxStar[eneID] = star;
                    act_MaxStar[eneID] = act;
                }
                int apartScore = calcApartScore(act);
                if (apartScore > maxApartScore[eneID]) {
                    maxApartScore[eneID] = apartScore;
                    act_mostFarFromEnemies[eneID] = act;
                }
            }
            // 一個ずつ非ゼロな攻撃を抽出していく．
            for (int unitID = 0; unitID < numTotalUnits; unitID++) {
                if (act_mostFarFromEnemies[unitID] != 0) { selectedAct.Add(act_mostFarFromEnemies[unitID]); }
                if (act_mostFarFromEnemies[unitID] == act_MaxStar[unitID]) { continue; }
                if (act_MaxStar[unitID] != 0) { selectedAct.Add(act_MaxStar[unitID]); }
            }
            return selectedAct;
        }

        //敵1体に1手ずつ攻撃手の提案を行う．（地形が良いもの，敵と隣接してない物，をそれぞれ高スコア）
        public List<int> suggest1AtkActsforEachEnemy() {

            //FIXME　敵ごとにAtkの種類を分けて，つめる！
            List<int> selectedAct = new List<int>();

            List<int> allAtkActs = SAT_RangeController.myGetAttackActionList(this, map);
            
            int numTotalUnits = map.getUnits().Length;
            int[] act_mostFarFromEnemies = new int[numTotalUnits]; ;
            int[] maxApartScore = Enumerable.Repeat<int>(-64, numTotalUnits).ToArray(); ;

            foreach (int act in allAtkActs) {
                int eneID = SatQuickAction.getTarID(act);

                int apartScore = calcApartScore(act);
                //Console.WriteLine(SatQuickAction.toOneLineString(act)+" score "+ apartScore);

                if (apartScore > maxApartScore[eneID]) {
                    maxApartScore[eneID] = apartScore;
                    act_mostFarFromEnemies[eneID] = act;
                }
                else if (apartScore == maxApartScore[eneID]) {
                    // 50%の確率で入れ替え？　いらなかったらそのうち消そう
                    if(SAT_FUNC.GET_MY_RANDOM().Next(2) > 0){
                        act_mostFarFromEnemies[eneID] = act;
                    }
                }
            }
            // 一個ずつ非ゼロな攻撃を抽出していく．
            for (int unitID = 0; unitID < numTotalUnits; unitID++) {
                if (act_mostFarFromEnemies[unitID] != 0) { selectedAct.Add(act_mostFarFromEnemies[unitID]); }
            }
            return selectedAct;
        }

        //多くの敵の射程にいないほど高得点をつける．
        //-1*（射程敵数）
        private int calcApartScore(int act) {
            int x = SatQuickAction.getDestX(act);
            int y = SatQuickAction.getDestY(act);
           

            int numInRange = 0;
            int turnCnt = map.getTurnCount();
            foreach (SatQuickUnit eneU in map.teamUnits[SAT_FUNC.REVERSE_COLOR(fTeamColor)]) {
                if (eneU == null || eneU.Dead) { continue; }
                if (eneU.routeMap_step[turnCnt][x, y] >= 0) { numInRange++; }
            }
            return -(numInRange);
        }


        public int suggestMostApproachingMoveOnlyAct() {
            int[] eneGravity_xy = SAT_FUNC2.CALC_GRAVITY_POINT_ENEMY(map, fTeamColor);
            int graX = eneGravity_xy[0];
            int graY = eneGravity_xy[1];
            //Console.WriteLine("HERE x"+graX+","+graY);
            int curX = fXpos;
            int curY = fYpos;
            int turnCnt = map.getTurnCount();

            while (true) {
                //Console.WriteLine("HERE x"+curX+","+curY);
                int dXscale = graX - curX;
                if (dXscale != 0) { dXscale /= Math.Abs(dXscale); }
                int dYscale = graY - curY;
                if (dYscale != 0) { dYscale /= Math.Abs(dYscale); }
                if (dYscale == 0 && dXscale == 0) { break; }
                if (routeMap_step[turnCnt][curX + dXscale, curY + dYscale] > 0 
                    && map.getMapUnit()[curX+dXscale,curY+dYscale] ==null ) {
                    curX += dXscale;
                    curY += dYscale;
                    continue;
                }
                if (dXscale != 0 && routeMap_step[turnCnt][curX + dXscale, curY] > 0
                    && map.getMapUnit()[curX + dXscale, curY] == null) {
                    curX += dXscale;
                    continue;
                }
                if (dYscale != 0 && routeMap_step[turnCnt][curX, curY + dYscale] > 0
                    && map.getMapUnit()[curX, curY　+ dYscale] == null) {
                    curY += dYscale;
                    continue;
                }
                break;
            }
            return SatQuickAction.createMoveOnlyAction(this, curX, curY);
        }


    }

    
    
}
