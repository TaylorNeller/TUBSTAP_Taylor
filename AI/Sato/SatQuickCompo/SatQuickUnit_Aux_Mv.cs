using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {
    /// <summary>
    /// SatQuickUnitクラスの分割定義．
    /// こちらにはユニット移動可能エリアのフィールドに関する更新メソッド群を詰め込む．
    /// 
    /// public void initMovableMap(int turnCnt) 
    /// public void routeReload_enemyLeave(int turnCnt, int x, int y) {
    /// public void routeReload_enemyEnter(int turnCnt, int x, int y) {    
    /// 
    /// </summary>
    partial class SatQuickUnit : Unit {

        // 上下左右を表す定数                         [_],  [E],[N],[W],[S]
        private static int[] CompassDX = new int[5] { -9999, +1, 0, -1, 0 };
        private static int[] CompassDY = new int[5] { -9999, 0, -1, 0, +1 };

        public static COMPASS MIN_COMPASS_DIR = COMPASS.E;
        public static COMPASS MAX_COMPASS_DIR = COMPASS.S;
        public static int MIN_COMPASS_IND = 1;
        public static int MAX_COMPASS_IND = 4;
        public static COMPASS[] REVERSE_DIR = new COMPASS[] { COMPASS._, COMPASS.W, COMPASS.S, COMPASS.E, COMPASS.N };

          
        public bool isMovableMapPrepared() { return !(routeMap_step[0] == null); }

        public bool isOutsideMap(int x, int y) {
            return (x <= 0 ||  map.getXsize() - 1 <= x || y <= 0 || map.getYsize() - 1 <= y);
        }

        /// <summary>
        /// [Map]        [RouteMap_Step]     [RouteMap_Compass]
        ///  _ _ _ _     3  2  1  0           N  E(N) E(N) _
        /// [I]_ _ _ ==> 4  3  2  1       ==> _  E    E    E
        ///  _ _ _ _     3  2  1  0           S  S(E) S(E) _
        ///  _ _ _ _     2  1  0 -1           S  S(E) _    _
        /// 
        ///  N (>0) means, can move there. 
        /// (0) means, cannot move, but, can attack an unit upon the cell.
        /// </summary>
        public void initMovableMap(int turnCnt) {
            if (Dead) { return; }
            if ( !isMovableMapPrepared() ) {
                for(int i =0; i < routeMap_step.Length; i++){ 
                    routeMap_step[i] = new int [map.getXsize(), map.getYsize()];
                    routeMap_compass[i] = new COMPASS[map.getXsize(), map.getYsize()];
                    SAT_FUNC.INIT_2D_IntARRAY_AS(routeMap_step[i], -1);
                }
            }
            //ターンが1以上のときは，木のDepth間をいったりきたりする過程で配列の値がバグりうる．
            if (turnCnt != 0) {
                SAT_FUNC.INIT_2D_IntARRAY_AS(routeMap_step[turnCnt], -1);
                routeMap_compass[turnCnt] = new COMPASS[map.getXsize(), map.getYsize()];
            }

            //Initialize from current position.
            int step = fSpec.getUnitStep() + 1; // 1を足す事で，攻撃のみ可能な範囲，も計算できる．
            routeMap_step[turnCnt][fXpos, fYpos] = step;

            int[] posCounts = new int[step + 1];  // 残り移動力 index になる移動可能箇所数．0で初期化される
            int[,] posX = new int[step + 1, 64]; // 残り移動力 index になる移動可能箇所． 100は適当な数，本来 Const化すべき
            int[,] posY = new int[step + 1, 64];

            //＃　以下，だいたいRangeContorollerの「ReachableCell()」コードの使いまわし．
            //＃＃＃

            // 今いる場所は残り移動力 unitStep の移動可能箇所であり，最終的にも移動可能
            posCounts[step] = 1;
            posX[step, 0] = fXpos;
            posY[step, 0] = fYpos;

            // 残り移動力 restStep になる移動可能箇所から，その上下左右を見て，移動できるならリストに追加していく
            for (int restStep = step; restStep > 0; restStep--) { // 現在の残り移動コスト
                for (int i = 0; i < posCounts[restStep]; i++) {
                    int x = posX[restStep, i];  // 注目する場所
                    int y = posY[restStep, i];

                    // この (x,y) の上下左右を見る
                    for (int r = MIN_COMPASS_IND; r <= MAX_COMPASS_IND; r++) {
                        int newx = x + CompassDX[r]; // 上下左右
                        int newy = y + CompassDY[r];

                        if(isOutsideMap(newx,newy)){continue;}

                        int newrest = restStep - fSpec.getMoveCost(map.getFieldType(newx, newy)); // 移動後の残り移動コスト
                        //負なら，0を入れて終わり（単なる攻撃可能範囲．）
                        if (newrest <= 0 && routeMap_step[turnCnt][newx, newy] < 0) {
                            routeMap_step[turnCnt][newx, newy] = 0;
                            routeMap_compass[turnCnt][newx, newy] = (COMPASS)r;
                            continue;  // 周囲にあたったか，移動力が足りない →進入不可
                        }

                        SatQuickUnit u = map.getUnit(newx, newy);
                        if (u != null) {
                            if (u.getTeamColor() != fTeamColor) {
                                routeMap_step[turnCnt][newx, newy] = 0;
                                routeMap_compass[turnCnt][newx, newy] = (COMPASS)r;
                                continue;  // 敵ユニットにあたった →進入不可
                            }
                        }

                        // すでに移動可能マークがついた場所じゃなければ，移動可能箇所に追加する
                        if (newrest > routeMap_step[turnCnt][newx, newy]) {
                            posX[newrest, posCounts[newrest]] = newx;
                            posY[newrest, posCounts[newrest]] = newy;
                            posCounts[newrest]++;
                            routeMap_step[turnCnt][newx, newy] = newrest;
                            routeMap_compass[turnCnt][newx, newy] = (COMPASS) r;
                        }
                    }
                }
            }
        }


        public void routeReload_enemyLeave(int turnCnt, int x, int y, int destX, int destY) {
            if (routeMap_step[turnCnt][x, y] < 0) { return; }

            //Reload the cost of the cell that have enemy upon it. 
            int tmpMaxStep = 0;
            COMPASS tmpMaxCompass = COMPASS._;
            for(int dir = MIN_COMPASS_IND; dir <= MAX_COMPASS_IND; dir++){
                int adjacentStep = routeMap_step[turnCnt][x+CompassDX[dir],y+CompassDY[dir]];
                if(adjacentStep > tmpMaxStep){
                    tmpMaxStep = adjacentStep;
                    tmpMaxCompass = (COMPASS)dir;                    
                }
            }
            int new_step = Math.Max(0, tmpMaxStep - fSpec.getMoveCost(map.getFieldType(x, y)));
            routeMap_step[turnCnt][x, y]= new_step;
            routeMap_compass[turnCnt][x , y] = REVERSE_DIR[(int)tmpMaxCompass];
            if(routeMap_step[turnCnt][x, y] == 0){ return; }


            //ここからこのユニットの移動マップを一から再計算．
            int[] posCounts = new int[new_step + 1];  // 残り移動力 index になる移動可能箇所数．0で初期化される
            int[,] posX = new int[new_step + 1, 16]; // 残り移動力 index になる移動可能箇所． 16は適当な数，本来 Const化すべき
            int[,] posY = new int[new_step + 1, 16];

            // 今いる場所は残り移動力 unitStep の移動可能箇所であり，最終的にも移動可能
            posCounts[new_step] = 1;
            posX[new_step, 0] = x;
            posY[new_step, 0] = y;

            for (int restStep = new_step; restStep > 0; restStep--) { // 現在の残り移動コスト
                for (int i = 0; i < posCounts[restStep]; i++) {
                    int xPivot = posX[restStep, i];  // 注目する場所
                    int yPivot = posY[restStep, i];

                    // この (x,y) の上下左右を見る
                    for (int r = MIN_COMPASS_IND; r <= MAX_COMPASS_IND; r++) {
                        int newx = xPivot + CompassDX[r]; // 上下左右
                        int newy = yPivot + CompassDY[r];

                        if (isOutsideMap(newx, newy)) { continue; }

                        int newrest = restStep - fSpec.getMoveCost(map.getFieldType(newx, newy)); // 移動後の残り移動コスト
                        //負か0なら，0を入れて終わり（単なる攻撃可能範囲．）
                        if (newrest <= 0 && routeMap_step[turnCnt][newx, newy] < 0) {
                            routeMap_step[turnCnt][newx, newy] = 0;
                            routeMap_compass[turnCnt][newx, newy] = (COMPASS)r;
                            continue;
                        }

                        SatQuickUnit u = map.getUnit(newx, newy);
                        //敵ユニットが空でなく，なおかつその敵ユニットが「つい今移動した敵」の物でない場合．
                        //※　「つい今移動した敵」による経路塞ぎは，別の「EnemyEnter()」メソッドで独立して処理するので，
                        //※　ここでその影響を考慮してはいけない．
                        if (u != null && !(newx == destX && newy == destY)) {　
                            if (u.getTeamColor() != fTeamColor) {
                                routeMap_step[turnCnt][newx, newy] = 0;
                                routeMap_compass[turnCnt][newx, newy] = (COMPASS)r;
                                continue;  // 敵ユニットにあたった →進入不可
                            }
                        }

                        // すでに移動可能マークがついた場所じゃなければ，移動可能箇所に追加する
                        if (newrest > routeMap_step[turnCnt][newx, newy]) {
                            posX[newrest, posCounts[newrest]] = newx;
                            posY[newrest, posCounts[newrest]] = newy;
                            posCounts[newrest]++;
                            routeMap_step[turnCnt][newx, newy] = newrest;
                            routeMap_compass[turnCnt][newx, newy] = (COMPASS)r;
                        }
                    }
                }
            }
        }


        public void routeReload_enemyEnter(int turnCnt, int x, int y) {
            if (routeMap_step[turnCnt][x, y] <= 0) { return; } //Outside the range.
            routeMap_step[turnCnt][x, y] = 0;
            routeReload_eneEnter_aux(turnCnt, x, y);

            //SAT_FUNC.WriteLine("call:"+callCnt);
        }

        //private int callCnt = 0;

        /// <summary>
        /// 副処理メソッド．ややこしい事に，
        /// 引き数X-Y地点マス　の，４方隣接マス (map[new_x, new_y])だけではなく，
        /// その隣接マスのさらに４方隣接マス　(map[new_new_x, new_new_y])という概念も出てくる．
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void routeReload_eneEnter_aux(int turnCnt, int x, int y) {
            //Console.WriteLine("x " + x + ", y " + y);
            //Console.WriteLine(getRouteMapString());
            //callCnt++;    /* DEBUG. */
            //周囲の４マスそれぞれに（もし経路上塞ぎの関係を持つなら）新しいStep値を割り当てる．
            for (int dir = MIN_COMPASS_IND; dir <= MAX_COMPASS_IND; dir++) {
                int newx = x + CompassDX[dir];
                int newy = y + CompassDY[dir];
                if (routeMap_compass[turnCnt][newx, newy] != (COMPASS)dir) {
                    continue;
                }
                if (isOutsideMap(newx, newy)) { continue; }
                
                //まずは簡単な，もともと「攻撃のみが可能」だったマスの処理
                if (routeMap_step[turnCnt][newx, newy] == 0) {
                    routeMap_step[turnCnt][newx, newy] = -1; //一旦「-1」つまり接触不能点にしてみる
                    routeMap_compass[turnCnt][newx, newy] = COMPASS._;
                    for (int dir2 = MIN_COMPASS_IND; dir2 <= MAX_COMPASS_IND; dir2++) {
                        int newnewx = newx + CompassDX[dir2];
                        int newnewy = newy + CompassDY[dir2];
                        if (routeMap_step[turnCnt][newnewx, newnewy] > 0) {
                            routeMap_step[turnCnt][newx, newy] = 0; //隣に移動可能点があれば「-1」から再び攻撃可能点に昇進
                            routeMap_compass[turnCnt][newx, newy] = REVERSE_DIR[dir2];
                            break;
                        }
                    }
                    continue;
                }

                // # 進入も可能だったマス　の処理
                // ######
                //  - そのマスから最小ステップで進入できる方角の探索
                //    [ ?,          4           , ? ]
                //    [ -1, [マスX(地形コスト1)], 3 ]　　　とかで，マスXが Step = 4, 侵入方角 N(北) となる
                //    [ ?,          5           , ? ]
                int tmp_max = -1;
                COMPASS tmp_compass = COMPASS._;
                int step_toEnter = fSpec.getMoveCost(map.getFieldType(newx, newy));
                
                for (int dir2 = MIN_COMPASS_IND; dir2 <= MAX_COMPASS_IND; dir2++) {                    
                    int newnewx = newx + CompassDX[dir2]; 
                    int newnewy = newy + CompassDY[dir2];

                    // HACK ここで例えばStep[ 6 5 4 3] -> [ 6 X 4 3]と敵が(5)に来たとき一旦[6 X 2 3]と計算される問題がある．
                    // 循環的な計算になりそうだが・・・まぁ，いいのか？　コンパスをビットフラグにすれば解決できそうではある．
                    
                    int adjacentStep = routeMap_step[turnCnt][newnewx, newnewy];
                    if (adjacentStep <= 0) { continue; }    //0以下Stepマスは進入可能性を提供しない．
                    int newstep = Math.Max(0, adjacentStep - step_toEnter);
                    if(newstep > tmp_max){
                        tmp_max = newstep;
                        tmp_compass = REVERSE_DIR[dir2];
                    }
                }
                //結局以前と同コストで侵入できる場合
                //[new value]       [old value]
                if ( tmp_max    == routeMap_step[turnCnt][newx, newy]) {
                    routeMap_compass[turnCnt][newx, newy] = tmp_compass;
                    continue;
                }

                //侵入コストが重く，マス突入時のステップ余裕値が減っちゃう場合
                routeMap_step[turnCnt][newx, newy] = tmp_max;
                if (tmp_max >= 0) { routeMap_compass[turnCnt][newx, newy] = tmp_compass; } 
                else { routeMap_compass[turnCnt][newx, newy] = COMPASS._; }
                routeReload_eneEnter_aux(turnCnt, newx, newy);
            } 
        }


        public string getRouteMapString() { return getRouteMapString(0); }

        public string getRouteMapString(int turnCnt) {
            string str = "";
            str += "ROUTE_MAP_step t("+turnCnt+") \r\n";
            str += "_";
            for (int t = 1; t < map.getXsize() - 1; t++) str += "_____";
            str += "\r\n";

            for (int y = 1; y < map.getYsize() - 1; y++) {
                str += "|";
                for (int x = 1; x < map.getXsize() - 1; x++) {
                    if (routeMap_step[turnCnt][x, y] < 0) { 
                        str += routeMap_step[turnCnt][x,y].ToString("D1") + routeMap_compass[turnCnt][x,y].ToString() + " |";
                        continue;
                    }
                        str += routeMap_step[turnCnt][x,y].ToString("D2")+ routeMap_compass[turnCnt][x,y].ToString() +" |";                    
                }
                str += "\r\n";
                str += "|";
                for (int t = 1; t < map.getXsize() - 1; t++) str += "____|";
                str += "\r\n";
            }
            return str;
        }

    }

    enum COMPASS {
        _,
        E, 
        N,
        W,
        S,
    }
    
}
