using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {
    /// <summary>
    /// My Static Function Class for heavier functions.
    /// </summary>
    class SAT_FUNC2 {
        // 上下左右を表す定数        
        private static int[] DX = new int[4] { +1, 0, -1, 0 };
        private static int[] DY = new int[4] { 0, -1, 0, +1 };


        private const int UNITVALUE_INFANTRY_OTHERS_RATIO = 4;
        
        

        /// <summary>
        /// return the gravity point of enemy positions. 
        ///     1   2   3
        /// 1: En1 ___ En2
        /// 2: ___ ___ ___    -> Then, return the point (2,2)
        /// 3: En3 ___ En4
        /// </summary>
        /// <param name="map"></param>
        /// <param name="myTeamColor"></param>
        /// <returns></returns>
        public static int[] CALC_GRAVITY_POINT_ENEMY(SatQuickMap map, int myTeamColor) {
            int[] grav_xy = new int[2];
            SatQuickUnit[] enemyList = map.teamUnits[SAT_FUNC.REVERSE_COLOR(myTeamColor)];
            int total = 0;
            foreach (SatQuickUnit u in enemyList) {
                if (u==null || u.Dead || u.isActionFinished()) { continue; }
                grav_xy[0] += u.getXpos();
                grav_xy[1] += u.getYpos();
                total++;
            }
            if (total == 0) { return new int[2] { map.getXsize() / 2, map.getYsize() / 2 }; }
            grav_xy[0] /= total;
            grav_xy[1] /= total;
            return grav_xy;
        }

        /// <summary>
        ///     0   1   2   3   4   5
        /// 0  My1 ___ My2 ___ ___ ___
        /// 1  ___ ___ ___ _*_ ___ En1  => return {x= 3, y= 1} (the "*" point).
        /// 2  My3 ___ My4 ___ ___ ___     Because, my team gravity == {x= 1, y= 1}.
        ///                                Enemy team gravity == {x=5, y=1}. 
        ///                                Thus, the average of them =>{3,1}.
        /// </summary>
        /// <returns></returns>
        public static int[] CALC_AVERAGED_2TEAM_GRAVITY_POINT(SatQuickMap map){
            int[] grav_xy = new int[2];
            List<SatQuickUnit> enemyList = map.getUnitsList(map.phaseColor, false, false, true);
            int total = 0;
            foreach (SatQuickUnit u in enemyList)
            {
                grav_xy[0] += u.getXpos();
                grav_xy[1] += u.getYpos();
                total++;
            }
            if (total == 0) { grav_xy = new int[2] { map.getXsize() / 2, map.getYsize() / 2 }; }
            grav_xy[0] /= total;
            grav_xy[1] /= total;
            int[] grav_xy2 = new int[2];
            List<SatQuickUnit> myList = map.getUnitsList(SAT_FUNC.REVERSE_COLOR(map.phaseColor), false, false, true);
            total = 0;

            foreach (SatQuickUnit u in myList)
            {
                grav_xy2[0] += u.getXpos();
                grav_xy2[1] += u.getYpos();
                total++;
            }
            if (total == 0) { grav_xy2 =  new int[2] { map.getXsize() / 2, map.getYsize() / 2 }; }
            grav_xy2[0] /= total;
            grav_xy2[1] /= total;
            grav_xy[0] = (grav_xy[0] + grav_xy2[0]) / 2;
            grav_xy[1] = (grav_xy[1] + grav_xy2[1]) / 2;
            return grav_xy;
        }

        //重心座標の導出（Mapクラス用．高速MAPには使わないこと．）
        public static int[] CALC_AVERAGED_2TEAM_GRAVITY_POINT(Map map) {
            if (map is SatQuickMap) { SAT_FUNC.WriteLine("CLASS ERROR!"); int xxx = 0; xxx = 1 / xxx; }

            int[] grav_xy_R = new int[2];
            int[] grav_xy_B = new int[2];
            int total_R = 0;
            int total_B = 0;
            Unit[] uniList = map.getUnits();
            foreach (Unit u in uniList) {
                if (u == null || u.isDead()) { continue; }
                if (u.getTeamColor() == Consts.RED_TEAM) {
                    grav_xy_R[0] += u.getXpos();
                    grav_xy_R[1] += u.getYpos();
                    total_R++;
                } else {
                    grav_xy_B[0] += u.getXpos();
                    grav_xy_B[1] += u.getYpos();
                    total_B++;
                }
            }
            if (total_R == 0) {
                grav_xy_R = new int[2] { map.getXsize() / 2, map.getYsize() / 2 };
            } else {
                grav_xy_R[0] /= total_R;
                grav_xy_R[1] /= total_R;            
            }
            if (total_B == 0) {
                grav_xy_B = new int[2] { map.getXsize() / 2, map.getYsize() / 2 };
            } else {
                grav_xy_B[0] /= total_B;
                grav_xy_B[1] /= total_B;
            }  
            grav_xy_R[0] = (grav_xy_R[0] + grav_xy_B[0]) / 2;
            grav_xy_R[1] = (grav_xy_R[1] + grav_xy_B[1]) / 2;
            return grav_xy_R;
        }



        //For cannon units
        public static int COUNT_ADJACENT_ENEMY(SatQuickMap map, int myTeamColor, int x, int y) {
            int enemy_unit_num = 0;
            for (int di = 0; di < 4; di++) {
                int x_look = x + DX[di];
                int y_look = y + DY[di];
                if (map.getUnit(x_look, y_look) != null) {
                    if (map.getUnit(x_look, y_look).getTeamColor() != myTeamColor) {
                        enemy_unit_num++;
                    }
                }
            }
            return enemy_unit_num;
        }

        public static int COUNT_AROUND_ACTED_FRIEND(SatQuickMap map, int myTeamColor, int x, int y) {
            return COUNT_AROUND_ACTED_FRIEND(map, myTeamColor, x, y, null);
        }

        public static int COUNT_AROUND_ACTED_FRIEND(SatQuickMap map, int myTeamColor, int x, int y, bool[] maskedIDs) {
            int actedFriend_unit_num = 0;
            for (int di = 0; di < 4; di++) {
                int x_look = x + DX[di];
                int y_look = y + DY[di];
                SatQuickUnit unit_around = map.getUnit(x_look, y_look);
                if ( unit_around != null) {
                    if ( unit_around.getTeamColor() == myTeamColor) {
                        if ( unit_around.isActionFinished()) {
                            if (maskedIDs != null && !maskedIDs[unit_around.getID()]) {
                                actedFriend_unit_num++;
                            }
                        }
                    }
                }
            }
            return actedFriend_unit_num;
        }




        public static int[] makeIDMask(Map map, int phaseColor, int sizeFriendActiveUni, int sizeEnemyActiveUni){
            List<Unit> myActiveUnits  = map.getUnitsList(phaseColor, false, true, false);
            List<Unit> eneWholeUnits  = map.getUnitsList(phaseColor, false, false, true);

            List<int> maskIDs_fri = new List<int>();
            List<int> maskIDs_ene = new List<int>();
            List<int> weightedIDs_fri = new List<int>();
            List<int> weightedIDs_ene = new List<int>();

            int[] grav_xy = CALC_AVERAGED_2TEAM_GRAVITY_POINT(map);
            //AddWeight Whole Units
            foreach (Unit unit in myActiveUnits) {
                weightedIDs_fri.Add(ADD_WEIGHT_UNIT(unit, grav_xy));
            }
            foreach (Unit unit in eneWholeUnits) {
                if (unit.isActionFinished()) { continue; }
                weightedIDs_ene.Add(ADD_WEIGHT_UNIT(unit, grav_xy));
            }

            //Sort -価値が低いユニットから順に．例：{ u(価値3), u(6), u(8) }
            weightedIDs_fri.Sort();
            weightedIDs_ene.Sort();

            //詰め込み．価値の低いIDから．（詰め込んだ数）＋（盤に残るべきActiveユニ数）＜＝　myActiveUnits.Size
            for (int i = 0; maskIDs_fri.Count + sizeFriendActiveUni <  myActiveUnits.Count; i++) {
                maskIDs_fri.Add(WEIGHTED_ID_TO_SIMPLE_ID(weightedIDs_fri.ElementAt(i)));
            }
            for (int i = 0; maskIDs_ene.Count + sizeEnemyActiveUni < weightedIDs_ene.Count; i++) {
                maskIDs_ene.Add(WEIGHTED_ID_TO_SIMPLE_ID(weightedIDs_ene.ElementAt(i)));
            }
            maskIDs_fri.AddRange(maskIDs_ene);
            return maskIDs_fri.ToArray();
        }


        private static int WEIGHTED_ID_TO_SIMPLE_ID(int weightedID)
        {
            return weightedID % 100;
        }

        //TODO 沢山ある種のユニットを省くなどする？
        private static int ADD_WEIGHT_UNIT(Unit unit, int[] gravity_xy){
            int weighted = unit.getID();
            
            //if (unit.IsInfantry){
            //    weighted += 0;
            //} else {
            //    weighted += 70 * 100;//歩兵じゃなければ70点ボーナス
            //}
            weighted += 10 * 100 * unit.getHP();//HP * 10点ボーナス
            int dis = SAT_FUNC.MANHATTAN_DIST(unit.getXpos(), unit.getYpos(), gravity_xy[0], gravity_xy[1]);
            weighted += (15 - dis) * 100;//15 - [両軍ユニット重心座標からの距離] 点ボーナス
            //weighted += unit.getSpec().getUnitStep() * 100; // [ユニットの移動力] 点ボーナス
            return weighted;
        }


       

        
        //QuickMapに改造したのに従い，ちゃんとMapの情報を破壊しないよう注意．
        public static int EVAL_STATE_BY_ATTACK_SIMULATION(SatQuickMap map, int teamColor) {
            SatQuickUnit[] myUnits = map.teamUnits[teamColor];
            SatQuickUnit[] eneUnits = map.teamUnits[SAT_FUNC.REVERSE_COLOR(teamColor)];
            int turnCnt = map.getTurnCount();

            //初期HPの保存
            foreach (SatQuickUnit eneU in eneUnits) {
                if (eneU.Dead) { continue; }//死んだユニットは，どんなバグ出すか解らんので触らない！
                eneU.MyFlexibleBuffer = eneU.getHP();
            }

            //attack "Friend -> Enemy"
            foreach (SatQuickUnit myU in myUnits) {
                if (myU.Dead) { continue; }
                myU.MyFlexibleBuffer = myU.getHP();//HP保存

                SatQuickUnit targetU = null;
                int damGive = 0;
                int damBack = 0;
                int benefit = -8;
                foreach (SatQuickUnit eneU in eneUnits) {
                    if (eneU.Dead)  { continue; }
                    if (!IN_RANGE_ESTIMATE(map, myU, eneU))  { continue; }
                    if (!SAT_FUNC.isEffective(myU, eneU))    { continue; }

                    //##このCalcDamages()はダメージが敵HPを超えないよう自動的に調整してくれてる事に留意##
                    int[] dam_GiveBack = DamageCalculator.calculateDamages(myU.getSpec(), myU.getHP(),
                                eneU.getSpec(), eneU.getHP(),
                                map.getFieldDefensiveEffect(myU.getXpos(), myU.getYpos()),
                                map.getFieldDefensiveEffect(eneU.getXpos(), eneU.getYpos())
                                );
                    int beneTmp = 0;
                    beneTmp += (myU.IsInfantry)? dam_GiveBack[0] : dam_GiveBack[0] * UNITVALUE_INFANTRY_OTHERS_RATIO;
                    beneTmp -= (myU.IsInfantry) ? dam_GiveBack[1] : dam_GiveBack[1] * UNITVALUE_INFANTRY_OTHERS_RATIO; 
                    if (beneTmp > benefit) {
                        benefit = beneTmp;
                        damGive = dam_GiveBack[0];
                        damBack = dam_GiveBack[1];
                        targetU = eneU;
                    }
                }
                if (targetU != null) {
                    targetU.reduceHP(damGive);
                    myU.reduceHP(damBack);
                    //myU.setX_ints(new int[] { 1 }); //For revenge calculation.
                }
            }


            goto SKIP_UNTOUCHABLE_WIPEOUT; //このコメントアウトを外すと，この部分のルーチンをOFFにできる．
            //Calc. Damage from untouchable Units. 
            //ダメージを受け得ないユニットは，盤上の相手ユニットのうちダメージが通る物を全て破壊する．
            int[] friendFAPURIcounter = new int[6];
            int[] enemyFAPURIcounter = new int[6];
            setFAPURIcounter(map, teamColor, friendFAPURIcounter, enemyFAPURIcounter);
            //Console.WriteLine(String.Join(",",friendFAPURIcounter));
            //Console.WriteLine(String.Join(",",enemyFAPURIcounter));

            foreach(SatQuickUnit unit in map.getUnits()){
                //Console.WriteLine(unit.toString());
                //以下の条件のユニットはダメージを受けえないユニット候補ではないとする．
                if (unit == null) { continue; }
                if (unit.getHP() <= 1) { continue; }
                if (unit.IsCannon || unit.IsInfantry) { continue; }
                //敵が触れないユニットなら，敵軍の，有利なユニット全部を破壊する．
                if (checkUntouchable(unit, teamColor, friendFAPURIcounter, enemyFAPURIcounter)) {
                    wipeOutEnemies(unit, map);
                }
            }
            SKIP_UNTOUCHABLE_WIPEOUT:

            //Calc. HP-Diff. HP情報を復元する都合上，下記のEval＿Map_HPメソッドを流用してはいけない．
            int HPdiff = EVAL_MAP_HP(map, map.phaseColor);
            //Reset buffers
            foreach (SatQuickUnit myU in myUnits) {
                if (myU == null || myU.Dead ) { continue; }
                myU.setHP(myU.MyFlexibleBuffer);
                myU.MyFlexibleBuffer = 0;
            }
            foreach (SatQuickUnit eneU in eneUnits) {
                if (eneU == null || eneU.Dead) { continue; }
                eneU.setHP(eneU.MyFlexibleBuffer);
                eneU.MyFlexibleBuffer = 0;
            }
            return HPdiff;
        }


        //ダメージを負いえないユニットが敵軍の有利ユニット全てのHPをゼロにする．
        private static void wipeOutEnemies(SatQuickUnit unit_atk, SatQuickMap map) {
            SatQuickUnit[] units_against = map.teamUnits[SAT_FUNC.REVERSE_COLOR(unit_atk.getTeamColor())];
            foreach (SatQuickUnit unit_against in units_against) {
                if (unit_against != null) {
                    if (Spec.atkPowerArray[unit_atk.getSpec().getUnitType(), unit_against.getSpec().getUnitType()] > 0) {
                        unit_against.setHP(0);
                    }
                }
            }
        }

        //Check if the unit is untouchable from enemy units. 
        private static bool checkUntouchable(SatQuickUnit unit, int teamColor, int[] friFAPURI, int[] eneFAPURI) {            
            int[] FAPURI_to_refer;
            if (unit.getTeamColor() == teamColor) {
                FAPURI_to_refer = eneFAPURI;
            } else {
                FAPURI_to_refer = friFAPURI;
            }

            int myTypeIndex = unit.getSpec().getUnitType();
            for (int i = 0; i <= 5; i++) {
                if (FAPURI_to_refer[i] == 0) { continue; }
                if (Spec.atkPowerArray[i, myTypeIndex] > 10) {
                    return false;
                }                    
            }
            return true;
        }

        //Count the number of F, A, P, U, R, I for both sides. 
        private static void setFAPURIcounter(SatQuickMap map, int teamColor, int[] friFAPURI, int[] eneFAPURI) {

            foreach (SatQuickUnit unit in map.getUnits()) {
               if (unit == null) { continue; }
                if (unit.getHP() <= 1) { continue; }
                if (unit.getTeamColor() == teamColor) {
                    friFAPURI[unit.getSpec().getUnitType()]++;// F-> 0, A-> 1, P-> 2, U-> 3, I-> 5.
                } else {
                    eneFAPURI[unit.getSpec().getUnitType()]++;
                }
            }
        }

        /// <summary>
        /// input ( Map{Red-A10, Red-F4, Blue-I9}, TEAMCOLOR.BLUE )
        /// --> output (-5)
        /// </summary>
        /// <returns></returns> 
        public static int EVAL_MAP_HP(SatQuickMap map, int teamColor) {
            SatQuickUnit[] myUnits = map.teamUnits[teamColor];
            int eval_hp = 0;
            foreach (SatQuickUnit unit in myUnits) {
                if (unit == null || unit.Dead) { continue; }
                eval_hp += (unit.IsInfantry)? unit.getHP() : UNITVALUE_INFANTRY_OTHERS_RATIO * unit.getHP();
            }

            SatQuickUnit[] eneUnits = map.teamUnits[SAT_FUNC.REVERSE_COLOR(teamColor)];
            foreach (SatQuickUnit unit in eneUnits) {
                if (unit == null || unit.Dead) { continue; }
                eval_hp -= (unit.IsInfantry) ? unit.getHP() : UNITVALUE_INFANTRY_OTHERS_RATIO * unit.getHP();
            }
            return eval_hp;
        }


        //【rangeMap】は，ある地点から見ての，マップの各点への移動コスト余裕を見る．※値「0」で攻撃だけ可能な点
        //1次元MAPで左端(1,0)に戦車がいれば， [0, 7, 6, 5, 4, 3, 2, 1, 0, -1, -1].　※左端右端はマップのエッジ
        //地形が森だけの1次元MAPなら， 　[0, 7, 5, 3, 1, 0,-1,-1,-1, -1, -1].
        //
        //こんな風にマップのある座標（x,y）を始点とした「戦車の」移動コスト余裕が2次元配列の形で格納される，
        //2次元*2次元な配列． （ちなみに戦車である理由は，飛行機やキャノンにこの種のマップが必要なく，対空戦車は戦車と
        //　同じ移動力なためである．）
        private static int[,][,] rangeMap = null;
        

        public static void initRangeMap(int[,] fieldType) {
            rangeMap = new int[fieldType.GetLength(0),fieldType.GetLength(1)][,];
            for (int y = 1; y <= fieldType.GetLength(1)-2; y++) {
                for (int x = 1; x <= fieldType.GetLength(0) - 2; x++) {
                    initSingleRangeMap(x, y, fieldType);
                }
            }
        }

        //移動コスト配列初期化．ほとんどSatQuickUnit_Aux_Mv のinitRouteMapメソッドの使いまわし．
        public static void initSingleRangeMap(int x, int y, int[,] fieldType) {
            int[] DX = new int[4] { +1, 0, -1, 0 };
            int[] DY = new int[4] { 0, -1, 0, +1 };
            //rangeMap[x, y] = aRangeMap
            int[,] aRangeMap = new int[fieldType.GetLength(0), fieldType.GetLength(1)];
            int step = 7;//Magic Number.
            for (int xx = 0; xx <= fieldType.GetLength(0) - 1; xx++) {
                for (int yy = 0; yy <= fieldType.GetLength(1) - 1; yy++) {
                    aRangeMap[xx, yy] = -1;
                }
            }

            int[] posCounts = new int[step + 1];  // 残り移動力 index になる移動可能箇所数．0で初期化される
            int[,] posX = new int[step + 1, 64]; // 残り移動力 index になる移動可能箇所． 100は適当な数，本来 Const化すべき
            int[,] posY = new int[step + 1, 64];

            posCounts[step] = 1;
            posX[step, 0] = x;
            posY[step, 0] = y;
            
            aRangeMap[x, y] = step;
            
            // 残り移動力 restStep になる移動可能箇所から，その上下左右を見て，移動できるならリストに追加していく
            for (int restStep = step; restStep > 0; restStep--) { // 現在の残り移動コスト
                for (int i = 0; i < posCounts[restStep]; i++) {
                    int xlook = posX[restStep, i];  // 注目する場所
                    int ylook = posY[restStep, i];

                    // この (x,y) の上下左右を見る
                    for (int r = 0; r <= 3; r++) {
                        int newx = xlook + DX[r]; // 上下左右
                        int newy = ylook + DY[r];

                        if (newx == 0 || newx == aRangeMap.GetLength(0) - 1 ||
                            newy == 0 || newy == aRangeMap.GetLength(1) - 1) { continue; }

                        int newrest = restStep - Spec.moveCost[2, fieldType[newx, newy]]; // 移動後の残り移動コスト
                        //負なら，0を入れて終わり（単なる攻撃可能範囲．）
                        if (newrest <= 0 && aRangeMap[newx, newy] < 0) {
                            aRangeMap[newx, newy] = 0;
                            continue;  // 周囲にあたったか，移動力が足りない →進入不可
                        }

                        // すでに移動可能マークがついた場所じゃなければ，移動可能箇所に追加する
                        if (newrest > aRangeMap[newx, newy]) {
                            aRangeMap[newx, newy] = newrest;
                            posX[newrest, posCounts[newrest]] = newx;
                            posY[newrest, posCounts[newrest]] = newy;
                            posCounts[newrest]++;
                        }
                    }
                }
            }
            rangeMap[x, y] = aRangeMap;
        }



        /// <summary>
        /// Estimate the attackUnit can attack to the defendUnit.
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool IN_RANGE_ESTIMATE(SatQuickMap map, SatQuickUnit attackU, 
                                                SatQuickUnit defendU) {
            //遠距離ユニットは判定が簡単なので，精密近似/ラフ近似の別に関わらず，さっさと処理して値を返してしまう．
            if (attackU.IsCannon) {
                int distCannon = SAT_FUNC.MANHATTAN_DIST(attackU.getXpos(), attackU.getYpos(), 
                    defendU.getXpos(), defendU.getYpos());
                return (2<= distCannon && distCannon <= 3);
            }            

            //ラフに推測して返す．(飛行系ユニットと歩兵)
            if (attackU.IsFlyer || attackU.IsInfantry) {
                int dist = SAT_FUNC.MANHATTAN_DIST(attackU.getXpos(), attackU.getYpos(), defendU.getXpos(), defendU.getYpos());
                return (dist <= attackU.getSpec().getUnitStep() + 1);
            }
            //ラフに推測して返す．(戦車系ユニット)
            int remStep = rangeMap[attackU.getXpos(), attackU.getYpos()][defendU.getXpos(), defendU.getYpos()];
            return remStep >= 0;

        }


    
    }
}
