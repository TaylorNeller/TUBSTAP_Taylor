using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {

    /// <summary>
    /// 分割定義．いちいち参照不要なメソッド群が主にこちらに集まる．
    /// 派生元クラス（Map）に対する「エラーを返すだけの，隠ぺいメソッド」達が置かれる．
    /// あと，記述が面倒なクローン提供メソッドもここに置く．
    /// </summary>
    partial class SatQuickMap : Map{

        //マップインスタンスを作るときにユニットのIDをシャッフルするかどうか．
        public static readonly bool IDShuffle = true;

        public static SatQuickMap mapToQuickMap(Map map) {
            return mapToQuickMap(map, null);
        }

        public static SatQuickMap mapToQuickMap(Map map, int[] disableIDs) {

            if (map is SatQuickMap) { return (SatQuickMap) map; }
            SatQuickMap sq_map = new SatQuickMap();

            //土地情報コピー
            sq_map.fXsize = map.getXsize();
            sq_map.fYsize = map.getYsize();
            sq_map.fMapFieldType = new int[map.getFieldTypeArray().GetLength(0),
                                           map.getFieldTypeArray().GetLength(1)];
            for (int x = 0; x < sq_map.fMapFieldType.GetLength(0); x++) {
                for (int y = 0; y < sq_map.fMapFieldType.GetLength(1); y++) {
                    sq_map.fMapFieldType[x, y] = map.getFieldType(x, y);
                }
            }

            //ユニットマスク処理（行動不能にしておく．……探索時間の削減のため）
            if (disableIDs != null) {
                sq_map.applyDisableProcedure = true;
                for (int i = 0; i < disableIDs.Length; i++) {
                    map.getUnits()[disableIDs[i]].setActionFinished(true);
                }
            }

            //終局条件コピー
            sq_map.fTurnCount = 0;  //ゼロから始める．//本来なら， map.getTurnCount();
            sq_map.fTurnLimit = map.getTurnLimit() - map.getTurnCount(); //残りターンリミット数．差分．
            sq_map.fDrawHPThreshold = map.getDrawHPThreshold();

            //ユニットコピー
            sq_map.fUnits = new SatQuickUnit[map.getUnits().Length];
            List<SatQuickUnit> sqUnitList_teamRed = new List<SatQuickUnit>();
            List<SatQuickUnit> sqUnitList_teamBlue = new List<SatQuickUnit>();
            sq_map.fMapUnit = new SatQuickUnit[sq_map.fXsize, sq_map.fYsize];
            
            //シャッフル用配列の準備とシャッフル作業．ここに書くと汚いが，しかし分けて書くともっと汚くなるので妥協．
            List<int> redIndexList = new List<int>();
            List<int> blueIndexList = new List<int>();
            for (int i = 0; i < map.getUnits().Length; i++) {
                Unit u = map.getUnits()[i];
                if (u == null) { continue; }
                if (u.getTeamColor() == Consts.RED_TEAM ) { redIndexList.Add( u.getID()); }
                if (u.getTeamColor() == Consts.BLUE_TEAM) { blueIndexList.Add(u.getID()); }
            }
            SAT_FUNC.LIST_SHUFFLE<int>(redIndexList );
            SAT_FUNC.LIST_SHUFFLE<int>(blueIndexList);

            for (int x = 0; x < sq_map.fXsize; x++) {
                for (int y = 0; y < sq_map.fYsize; y++) {
                    if (map.getMapUnit()[x, y] == null) { continue; }
                    SatQuickUnit uTmp = SatQuickUnit.UnitToQuickUnit(map.getMapUnit()[x, y], sq_map);
                    sq_map.fMapUnit[x, y] = uTmp;
                                        
                    if (uTmp.getTeamColor() == Consts.RED_TEAM) {
                        sqUnitList_teamRed.Add( uTmp);
                        if (!IDShuffle) { goto SKIP_ID_SHUFFLE; }
                        uTmp.setID(redIndexList.ElementAt(0));
                        redIndexList.RemoveAt(0);
                    } else {
                        sqUnitList_teamBlue.Add(uTmp);
                        if (!IDShuffle) { goto SKIP_ID_SHUFFLE; }
                        uTmp.setID(blueIndexList.ElementAt(0));
                        blueIndexList.RemoveAt(0);
                    }
                SKIP_ID_SHUFFLE:
                    sq_map.fUnits[uTmp.getID()] = uTmp;
                }
            }
            //一旦リストの整列
            sqUnitList_teamRed.Sort ( CompareByID);
            sqUnitList_teamBlue.Sort (CompareByID);


            //RED_TEAM, BLUE_TEAMの値が変わっても動くようにMath.Maxで対策
            sq_map.teamUnits = new SatQuickUnit[Math.Max(Consts.RED_TEAM, Consts.BLUE_TEAM)+1][];
            sq_map.teamUnits[Consts.RED_TEAM] = sqUnitList_teamRed.ToArray();
            sq_map.teamUnits[Consts.BLUE_TEAM] = sqUnitList_teamBlue.ToArray();

            //ターンチーム色の保持，未行動ユニット数の保持
            sq_map.phaseColor = Consts.RED_TEAM;
            if (map.getTurnCount() % 2 == 1) {
                sq_map.phaseColor = Consts.BLUE_TEAM;//奇数ターンで青
            }
            if (map.reverse) {
                sq_map.phaseColor = SAT_FUNC.REVERSE_COLOR(sq_map.phaseColor);
            }
            //未行動ユニット数と，生き残りユニット数
            int unActed = 0;
            for (int i = 0; i < sq_map.teamUnits[sq_map.phaseColor].Length; i++) {
                if (sq_map.teamUnits[sq_map.phaseColor][i].isActionFinished()) { continue; }
                unActed++;
            }
            sq_map.numUnActedUnits = unActed;
            sq_map.fNumOfAliveUnits = new int[2];
            sq_map.fNumOfAliveUnits[Consts.RED_TEAM ] = map.getNumOfAliveColorUnits(Consts.RED_TEAM );
            sq_map.fNumOfAliveUnits[Consts.BLUE_TEAM] = map.getNumOfAliveColorUnits(Consts.BLUE_TEAM);
 
            //実行された行動の履歴作成
            sq_map.actionTypeHistory = new int[SatQuickUnit.HISTORY_MAX_LENGTH];
            sq_map.idHistoryOpUnit = new int[SatQuickUnit.HISTORY_MAX_LENGTH];
            sq_map.idHistoryTarUnit = new int[SatQuickUnit.HISTORY_MAX_LENGTH];

            return sq_map;
        }

        public void printMap_veryDetailed(){
            SAT_FUNC.WriteLine(toString());
            foreach( SatQuickUnit squ in fUnits.ToList<SatQuickUnit>()){
                if (squ == null){ continue;}
                SAT_FUNC.WriteLine(squ.toString());
            }
            SAT_FUNC.WriteLine("RED");
            foreach( SatQuickUnit squ in teamUnits[0].ToList<SatQuickUnit>()){
                if (squ == null){ continue;}
                SAT_FUNC.WriteLine(squ.toString());
            }
            SAT_FUNC.WriteLine("BLUE");
            foreach (SatQuickUnit squ in teamUnits[1].ToList<SatQuickUnit>()) {
                if (squ == null) { continue; }
                SAT_FUNC.WriteLine(squ.toString());
            }            
        }

        /// <summary>
        /// ID順で比較
        /// </summary>
        private static int CompareByID(SatQuickUnit a, SatQuickUnit b) {
            return a.getID() - b.getID();
        }


        public SatQuickMap(string mapFileName) {
            showError_NotProvided("Constructor(mapfileName)");
        }

		public SatQuickMap(string mapFileName, bool r) {
			showError_NotProvided("Constructor(mapfileName, reverseFlag)");
		}

        
        // ゲーム開始時にマップを読み込む関数
        public new void loadMapFile(string fileName) {
            showError_NotProvided("loadMapFile");
        }
        
        // ユニットの位置を読み込む．マップが生成された際に呼ばれる
        private new void loadUnits(string[] lines) {
            showError_NotProvided("loadUnits");
        }

		//通信用マップデータ送信装置
		public new String sendMapFile(){
            showError_NotProvided("sendMapFile");
            return "";
		}

		//通信用マップデータ受信装置
		public new void reciveMapFile(String fullLine) {
            showError_NotProvided("receiveMapFile");
		}

        // actを内部的に処理します。先送りはできますが巻き戻しはできません。
        public new void actControl(Action act) {
                showError_NotProvided("actControl");
        }

        // ユニット生成用メソッド
        private new void addUnit(int x, int y, String name, int team, int HP, int actionFinished) {
                showError_NotProvided("addUnit");
        }
        
        // 全UnitのactionFinishフラグをfalseに戻す
        // 未行動の状態にする．棋譜の再現ように使う
        public new void resetActionFinish() {
            showError_NotProvided("resetActionFinish");
        }       

        // 破壊されたユニットを削除する
        public new void deleteUnit(Unit deadUnit) {
            showError_NotProvided("deleteUnit");
        }

        // version 0.104より追加．ユニットを追加する．nullの場所にしか追加できない．失敗したら false を返す．
        public new bool addUnit(Unit unit) {
            showError_NotProvided("addUnit");
            return false;
        }

        /*
        public new Unit getUnit(int unitId) {
            showError_NotProvided("getUnit(id)");
            return null;
        }

        public new Unit[] getUnits() {
            showError_NotProvided("getUnits");
            return null;
        }

        public new Unit getUnit(int x, int y) {
            showError_NotProvided("getUnit(x,y)");
            return null;
        }

        public new Unit[,] getMapUnit() {
            showError_NotProvided("getMapUnit");
            return null;
        }
         * */

        // マップ上のユニットの位置を変更する
        public new void changeUnitLocation(int x, int y, Unit selectedUnit) {
                        showError_NotProvided("changeUnitLocation");
        }

        // 引数チームの全てのユニットの行動を終了させる
        public new void finishAllUnitsAction(int teamColor) {
            showError_NotProvided("finishAllUnitsAction");
        }

        // 引数のチームのユニットを全て行動可能にする
        public new void enableUnitsAction(int teamColor) {
            showError_NotProvided("enableUnitsAction");

        }

        // クローン作成メソッド
        public new Map createDeepClone() {
            showError_NotProvided("createDeepClone()");
            return null;
        }

        // クローン作成メソッド
        public new Map createDeepClone(Map copied) {
            showError_NotProvided("createDeepClone(Map)");
            return copied;
        }

    }

    
}
