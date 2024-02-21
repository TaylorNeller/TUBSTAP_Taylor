using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace SimpleWars {
    /// <summary>
    /// 1つのマップ（board）を表現するクラス
    /// マップ上のユニットも保持しており，
    /// 全自軍ユニットや，行動可能な自軍ユニット，相手チームユニット
    /// 等を取得する関数が利用可能
    /// </summary>
    class Map {
        private int fXsize;                         // マップのXサイズを表す変数．外部からマップファイルを読み込んだ時に決定する
        private int fYsize;                         // マップのYサイズを表す変数．外部から読み込んだ時に決定する
        private int[,] fMapFieldType;               // 地形の情報を表す       
        private Unit[,] fMapUnit;                   // マップ上のユニット なければnull

        private int[] fMaxUnitNum = new int[2];     // チームごとの最大ユニット数
        private Unit[] fUnits;                      // ユニット集合 死んだら nullにして，詰めない
        private int[] fNumOfAliveUnits = new int[2];// 生存しているユニットの数

        private int fTurnCount;                     // 現在のターン (各プレイヤが全ユニット行動終了ごと１ターン経過）
        private int fTurnLimit;                     // 引き分け裁定が行われるターン
        private int fDrawHPThreshold;               // HPの合計の差がこれより小さければ引き分け
        
        public string fMapFileName;                 // ファイルから読み込んできたフアィイル
		public bool reverse = false;


        // コンストラクタ
        // マップファイルを引数にとる
        public Map(string mapFileName) {
            this.fMapFileName = mapFileName;
			this.reverse = false;
            loadMapFile(mapFileName);
        }

		public Map(string mapFileName, bool r) {
			this.fMapFileName = mapFileName;
			this.reverse =r;
			loadMapFile(mapFileName);
		}

        // 空コンストラクタ
        public Map() {
        }

        #region ■■情報を取得する関数，ゲッター■■

        public int getXsize() {
            return this.fXsize;
        }

        public int getYsize() {
            return this.fYsize;
        }

        // 全てのチームのユニットが格納された配列を取得
        public Unit[] getUnits() {
            return this.fUnits;
        }

        // 引数（Id）で指定されたユニットを取得  version 0.104 より，範囲外の場合 null を返すことに
        public Unit getUnit(int unitId) {
            if (unitId < 0 || unitId >= fUnits.Length) {
                return null;
            } else {
                return this.fUnits[unitId];
            }
        }

        // ある位置にあるユニットを取得
        public Unit getUnit(int x, int y) {
            return this.fMapUnit[x, y];
        }

        // マップ→ユニットのマトリクスを全て取得
        public Unit[,] getMapUnit() {
            return this.fMapUnit;
        }

        // 地形のマトリクスを全て取得
        public int[,] getFieldTypeArray() {
            return this.fMapFieldType;
        }

        // ある位置の地形を取得
        public int getFieldType(int x, int y) {
            return this.fMapFieldType[x, y];
        }

        public int getTurnCount() {
            return this.fTurnCount;
        }

        public int getTurnLimit() {
            return this.fTurnLimit;
        }

        public int getDrawHPThreshold() {
            return this.fDrawHPThreshold;
        }

        // 引数チームの残りユニット数を返す
        public int getNumOfAliveColorUnits(int teamColor) {
            return fNumOfAliveUnits[teamColor];
        }

        //フィールドの防御適正を取得
        public int getFieldDefensiveEffect(int x, int y) {
            return Consts.sFieldDefense[fMapFieldType[x, y]];
        }

        // 引数で指定されたチームの全ユニットが行動を終了しているか？
        public Boolean isAllUnitActionFinished(int teamColor) {
            if (getUnitsList(teamColor, false, true, false).Count == 0) {
                return true;
            } else {
                return false;
            }
        }
        #endregion

        /// <summary>
        /// 対応したincludingのユニットリストを返します．重複が起きた場合は一つにまとめられます．
        /// </summary>
        /// <param name="teamColor"></param>
        /// <param name="includingMyActionFinishedUnits">自軍のアクションが終了したユニットが欲しい場合</param>
        /// <param name="includingMyMovableUnits">自軍の行動が可能なユニットが欲しい場合</param>
        /// <param name="includingOponentUnits">敵ユニットが欲しい場合</param>
        /// <returns>対応したユニットリスト</returns>
        public List<Unit> getUnitsList(int teamColor, bool includingMyActionFinishedUnits, bool includingMyMovableUnits, bool includingOponentUnits) {
            List<Unit> unitsList = new List<Unit>();

            for (int i = 0; i < fUnits.Length; i++) {
                if (fUnits[i] == null) continue;
                if (includingMyActionFinishedUnits &&  fUnits[i].isActionFinished() && fUnits[i].getTeamColor() == teamColor) {
                    unitsList.Add(fUnits[i]);// 行動が終了したユニット
                    continue;
                }
                if (includingMyMovableUnits        && !fUnits[i].isActionFinished() && fUnits[i].getTeamColor() == teamColor) {
                    unitsList.Add(fUnits[i]);// 行動が終了していないユニット
                    continue;
                }
                if (includingOponentUnits && fUnits[i].getTeamColor() != teamColor) {
                    unitsList.Add(fUnits[i]);// 敵チームのユニットリスト
                    continue;
                }
            }

            return unitsList;
        }

        // マップにアクションを適用する関数，木探索などのために利用できる．
        public void executeAction(Action a){
            
            Unit opUnit = getUnit(a.operationUnitId);// 操作したユニット
            switch (a.actionType) {
                case Action.ACTIONTYPE_MOVEONLY:
                    // ユニットの位置を変更する
                    changeUnitLocation(a.destinationXpos, a.destinationYpos, opUnit);
                    opUnit.setActionFinished(true);// 行動終了フラグをたてる
                    break;
                case Action.ACTIONTYPE_MOVEANDATTACK:
                    Unit targetUnit = getUnit(a.targetUnitId);// 攻撃対象ユニット

                    // まず，ユニットの位置を変更する
                    changeUnitLocation(a.destinationXpos, a.destinationYpos, opUnit);
                    int[] damages = DamageCalculator.calculateDamages(this, a);// 攻撃・反撃ダメージ

                    targetUnit.reduceHP(damages[0]);// 攻撃対象ユニットのHPを減少させる
                    if (targetUnit.getHP() == 0) {
                        deleteUnit(targetUnit);// ＨＰが0以下になる場合は，ユニットを削除する
                    }

                    opUnit.reduceHP(damages[1]);// 反撃処理
                    if (opUnit.getHP() == 0) {
                        deleteUnit(opUnit);// 反撃で破壊されたら削除する
                        return;
                    }

                    opUnit.setActionFinished(true);// 行動終了フラグをたてる
                    break;
                case Action.ACTIONTYPE_TURNEND:

                    List<Unit> movableUnits = getUnitsList(a.teamColor, false, true, false);//未行動ユニットリスト

                    foreach (Unit u in movableUnits) {
                        u.setActionFinished(true);// 行動終了フラグをたてる
                    }

                    break;
                default:
                    break;
            }
        }

        #region ▲▲sgfからマップ情報を読み込む関数（ユーザは通常利用しない）▲▲
        
        // ゲーム開始時にマップを読み込む関数
        public void loadMapFile(string fileName) {
            int i;
            // まず一旦全部読む
            String fullLine = "";
            using (StreamReader sr = new StreamReader(fileName)) {
                String line;
                while ((line = sr.ReadLine()) != null) {
                    fullLine += line;
                }
            }

            // セミコロンで分解
            string[] lines = fullLine.Split(';');
            string one = "",sizeX = "", sizeY = "", unitnumR = "", unitnumB = "", turnLimit = "", hpTs = "";
            for(i = 0;i < lines.Length;i++){
                one = SGFManager.searchOne(lines[i], "SIZEX");
                if (!one.Equals(""))  sizeX = one; 

                one = SGFManager.searchOne(lines[i], "SIZEY");
                if (!one.Equals(""))  sizeY = one; 

                one = SGFManager.searchOne(lines[i], "UNITNUMRED");
                if (!one.Equals(""))  unitnumR = one; 

                one = SGFManager.searchOne(lines[i], "UNITNUMBLUE");
                if (!one.Equals(""))  unitnumB = one; 

                one = SGFManager.searchOne(lines[i], "TURNLIMIT");
                if (!one.Equals(""))  turnLimit = one; 

                one = SGFManager.searchOne(lines[i], "HPTHRESHOLD");
                if (!one.Equals(""))  hpTs = one; 
            }

            // マップサイズと，ユニット数の読み込み
            fXsize = Int32.Parse(sizeX);
            fYsize = Int32.Parse(sizeY);
            fMaxUnitNum[0] = Int32.Parse(unitnumR);
            fMaxUnitNum[1] = Int32.Parse(unitnumB);
            fDrawHPThreshold = Int32.Parse(hpTs);
            fTurnLimit = Int32.Parse(turnLimit);

            // 初期化処理
            fMapFieldType = new int[fXsize, fYsize];
            fMapUnit = new Unit[fXsize, fYsize];
            fUnits = new Unit[fMaxUnitNum[0] + fMaxUnitNum[1]];

            //マップ配置読み込み MS=MapSet
            List<string[]> stList = new List<string[]>();

            for (i = 0; i < lines.Length; i++) {
                string msLine = SGFManager.searchOne(lines[i], "MAP");
                if (msLine.Equals("")) continue;

                string[] ones = msLine.Split(',');

                if (ones.Length > fXsize) { Logger.showDialogMessage("マップファイルのＸサイズが長さ超過．"); }

                stList.Add(ones); //リストに追加する
            }

            if (stList.Count > fYsize) { Logger.showDialogMessage("マップファイルのYサイズが長さ超過．"); }

            // 配列フィールドタイプの対応する位置に地形タイプを格納する
            for (int y = 0; y < stList.Count; y++) {
                string[] ones = stList[y];
                for (int x = 0; x < ones.Length; x++) {
                    fMapFieldType[x, y] = Int32.Parse(ones[x]);
                }
            }

            loadUnits(lines);// ユニットの位置を初期化する
        }
        
        // ユニットの位置を読み込む．マップが生成された際に呼ばれる
        private void loadUnits(string[] lines) {
            //マップ配置を初期化
            fMapUnit = new Unit[fXsize, fYsize];
            fUnits = new Unit[fMaxUnitNum[0] + fMaxUnitNum[1]];

            //ユニット数初期化
            fNumOfAliveUnits[0] = 0;
            fNumOfAliveUnits[1] = 0;

            //ユニット配置読み込み US=UnitSet
            for (int i = 0; i < lines.Length; i++) {
                string usLine = SGFManager.searchOne(lines[i], "UNIT");
                if (usLine.Equals("")) continue;

                string[] ones = usLine.Split(',');
                int x = Int32.Parse(ones[0]);
                int y = Int32.Parse(ones[1]);
                int team = Int32.Parse(ones[3]);
                int HP = Int32.Parse(ones[4]);
                int actionFinished = Int32.Parse(ones[5]);
				if (reverse) {
					team = (team + 1) % 2;
				}

                addUnit(x, y, ones[2], team, HP, actionFinished);
            }
        }

		//通信用マップデータ送信装置
		public String sendMapFile(){
			// まず一旦全部読む
			String fullLine = "";
			using (StreamReader sr = new StreamReader(fMapFileName)) {
				String line;
				while ((line = sr.ReadLine()) != null) {
					fullLine += line;
				}
			}

			return fullLine;
		}

		//通信用マップデータ受信装置
		public void reciveMapFile(String fullLine) {
			int i;
			// セミコロンで分解
			string[] lines = fullLine.Split(';');
			string one = "", sizeX = "", sizeY = "", unitnumR = "", unitnumB = "", turnLimit = "", hpTs = "";
			for (i = 0; i < lines.Length; i++) {
				one = SGFManager.searchOne(lines[i], "SIZEX");
				if (!one.Equals("")) sizeX = one;

				one = SGFManager.searchOne(lines[i], "SIZEY");
				if (!one.Equals("")) sizeY = one;

				one = SGFManager.searchOne(lines[i], "UNITNUMRED");
				if (!one.Equals("")) unitnumR = one;

				one = SGFManager.searchOne(lines[i], "UNITNUMBLUE");
				if (!one.Equals("")) unitnumB = one;

				one = SGFManager.searchOne(lines[i], "TURNLIMIT");
				if (!one.Equals("")) turnLimit = one;

				one = SGFManager.searchOne(lines[i], "HPTHRESHOLD");
				if (!one.Equals("")) hpTs = one;
			}

			// マップサイズと，ユニット数の読み込み
			fXsize = Int32.Parse(sizeX);
			fYsize = Int32.Parse(sizeY);
			fMaxUnitNum[0] = Int32.Parse(unitnumR);
			fMaxUnitNum[1] = Int32.Parse(unitnumB);
			fDrawHPThreshold = Int32.Parse(hpTs);
			fTurnLimit = Int32.Parse(turnLimit);

			// 初期化処理
			fMapFieldType = new int[fXsize, fYsize];
			fMapUnit = new Unit[fXsize, fYsize];
			fUnits = new Unit[fMaxUnitNum[0] + fMaxUnitNum[1]];

			//マップ配置読み込み MS=MapSet
			List<string[]> stList = new List<string[]>();

			for (i = 0; i < lines.Length; i++) {
				string msLine = SGFManager.searchOne(lines[i], "MAP");
				if (msLine.Equals("")) continue;

				string[] ones = msLine.Split(',');

				if (ones.Length > fXsize) { Logger.showDialogMessage("マップファイルのＸサイズが長さ超過．"); }

				stList.Add(ones); //リストに追加する
			}

			if (stList.Count > fYsize) { Logger.showDialogMessage("マップファイルのYサイズが長さ超過．"); }

			// 配列フィールドタイプの対応する位置に地形タイプを格納する
			for (int y = 0; y < stList.Count; y++) {
				string[] ones = stList[y];
				for (int x = 0; x < ones.Length; x++) {
					fMapFieldType[x, y] = Int32.Parse(ones[x]);
				}
			}

			loadUnits(lines);// ユニットの位置を初期化する
		}
        #endregion 

        #region ▲▲棋譜再生・UNDO用関数．SGFManagerから参照される（ユーザは通常利用しない）▲▲

        // actを内部的に処理します。先送りはできますが巻き戻しはできません。
        public void actControl(Action act) {
            switch (act.actionType) {
                case Action.ACTIONTYPE_MOVEONLY:
                    changeUnitLocation(act.destinationXpos, act.destinationYpos, fUnits[act.operationUnitId]);
                    fUnits[act.operationUnitId].setActionFinished(true);
                    break;
                case Action.ACTIONTYPE_MOVEANDATTACK:
                    changeUnitLocation(act.destinationXpos, act.destinationYpos, fUnits[act.operationUnitId]);
                    fUnits[act.operationUnitId].reduceHP(act.X_counterDamage);
                    fUnits[act.targetUnitId].reduceHP(act.X_attackDamage);
                    fUnits[act.operationUnitId].setActionFinished(true);

                    if (fUnits[act.operationUnitId].getHP() <= 0) {
                        deleteUnit(fUnits[act.operationUnitId]);
                    }
                    if (fUnits[act.targetUnitId].getHP() <= 0) {
                        deleteUnit(fUnits[act.targetUnitId]);
                    }
                    break;
                default:
                    break;
            }
        }

        // ユニット生成用メソッド
        private void addUnit(int x, int y, String name, int team, int HP, int actionFinished) {
            int maxUnitsCnt = 0; // 最大ユニット数で，各ユニットのidと対応させる
            for (int i = 0; i < fNumOfAliveUnits.Length; i++) {
                maxUnitsCnt += fNumOfAliveUnits[i];
            }

            fMapUnit[x, y] = new Unit(x, y, maxUnitsCnt, team, HP, actionFinished, Spec.getSpec(name));
            fUnits[maxUnitsCnt] = fMapUnit[x, y];

            //numOfAliveUnits++;
            if (team == Consts.RED_TEAM) fNumOfAliveUnits[0]++;
            if (team == Consts.BLUE_TEAM) fNumOfAliveUnits[1]++;
        }

        
        // 全UnitのactionFinishフラグをfalseに戻す
        // 未行動の状態にする．棋譜の再現ように使う
        public void resetActionFinish() {
            enableUnitsAction(Consts.BLUE_TEAM);
            enableUnitsAction(Consts.RED_TEAM);
        }
        #endregion

        #region ▲▲内容を更新するセッター等（ユーザは通常利用しない）▲▲

        public void setTurnCount(int turn) {
            fTurnCount = turn;
        }

        public void incTurnCount() {
            fTurnCount++;
        }

        // 破壊されたユニットを削除する
        public void deleteUnit(Unit deadUnit) {
            if (deadUnit.getTeamColor() == Consts.RED_TEAM) fNumOfAliveUnits[0]--;
            if (deadUnit.getTeamColor() == Consts.BLUE_TEAM) fNumOfAliveUnits[1]--;
            //numOfAliveUnits--;

            fMapUnit[deadUnit.getXpos(), deadUnit.getYpos()] = null;
            fUnits[deadUnit.getID()] = null;
        }

        // version 0.104より追加．ユニットを追加する．nullの場所にしか追加できない．失敗したら false を返す．
        public bool addUnit(Unit unit) {
            if (unit == null) return false;

            // hpが0以下なら追加できない．
            if (unit.getHP() <= 0) return false;

            // マップ上で， null の場所にしか追加できない．
            if (fMapUnit[unit.getXpos(), unit.getYpos()] != null) return false;

            // fUnits上で，null の場所にしか追加できない．
            if (fUnits[unit.getID()] != null) return false;

            // 追加処理．
            fMapUnit[unit.getXpos(), unit.getYpos()] = unit;
            fUnits[unit.getID()] = unit;
            fNumOfAliveUnits[unit.getTeamColor()]++;

            return true;
        }


        // マップ上のユニットの位置を変更する
        public void changeUnitLocation(int x, int y, Unit selectedUnit) {
            int tempXpos = selectedUnit.getXpos();// 移動前の位置
            int tempYpos = selectedUnit.getYpos();
            if (fMapUnit[x, y] != null && fMapUnit[x, y].getID() != selectedUnit.getID()) {
                MessageBox.Show("Map:バグ．ユニットが既に存在する位置に移動してます．");
            }
            if (tempXpos == x && tempYpos == y) return;  //位置が変更されてない場合は何もしない
            this.fMapUnit[x, y] = selectedUnit;
            this.fMapUnit[tempXpos, tempYpos] = null;// 移動前の位置を変更する
            selectedUnit.setPos(x, y);
        }

        // 引数チームの全てのユニットの行動を終了させる
        public void finishAllUnitsAction(int teamColor) {
            int i;
            for (i = 0; i < fUnits.Length; i++) {
                if (fUnits[i] == null) continue;
                if (fUnits[i].getTeamColor() == teamColor) fUnits[i].setActionFinished(true);
            }
        }

        // 引数のチームのユニットを全て行動可能にする
        public void enableUnitsAction(int teamColor) {
            int i;
            for (i = 0; i < fUnits.Length; i++) {
                if (fUnits[i] == null) continue;
                if (fUnits[i].getTeamColor() == teamColor) fUnits[i].setActionFinished(false);
            }
        }
        #endregion



        // クローン作成メソッド
        public Map createDeepClone() {
            Map copied = new Map();
            return createDeepClone(copied);
        }

        // クローン作成メソッド
        public Map createDeepClone(Map copied) {
            // フィールドタイプのコピー作成
            copied.fXsize = this.fXsize;
            copied.fYsize = this.fYsize;
            copied.fMapFieldType = new int[this.fXsize, this.fYsize];

            for (int x = 0; x < fXsize; x++) {
                for (int y = 0; y < fYsize; y++) {
                    copied.fMapFieldType[x, y] = this.fMapFieldType[x, y];
                }
            }

            // ユニットのコピー作成
            copied.fUnits = new Unit[this.fUnits.Length];
            copied.fMapUnit = new Unit[fXsize, fYsize];

            for (int i = 0; i < this.fUnits.Length; i++) {
                if (fUnits[i] == null) continue;
                copied.fUnits[i] = this.fUnits[i].createDeepClone();

                int x = copied.fUnits[i].getXpos();
                int y = copied.fUnits[i].getYpos();
                copied.fMapUnit[x, y] = copied.fUnits[i];

            }

            // ユニット数のコピー
            for (int team=0; team<=1; team++) { 
                copied.fMaxUnitNum[team] = this.fMaxUnitNum[team];
                copied.fNumOfAliveUnits[team] = this.fNumOfAliveUnits[team];
            }
            // ターン数等のコピー
            copied.fTurnCount = this.fTurnCount;
            copied.fTurnLimit = this.fTurnLimit;
            copied.fDrawHPThreshold = this.fDrawHPThreshold;

            return copied;
        }

        public string toString() {
            string str;
            str = "_";
            for (int t = 1; t < this.fXsize - 1; t++) str += "_____";
            str += "\r\n";

            for (int y = 1; y < this.fYsize - 1; y++) {
                str += "|";
                for (int x = 1; x < this.fXsize - 1; x++) {
                    if (this.fMapUnit[x, y] == null) {
                        str += "    |";
                        continue;
                    }
                    if (fMapUnit[x, y].getTeamColor() == 0) {
                        str += "R";
                    }
                    else {
                        str += "B";
                    }
                    str += fMapUnit[x, y].getMark().ToString();
                    str += fMapUnit[x, y].getHP().ToString("D2");
                    str += "|";
                }

                str += "\r\n";
                str += "|";
                for (int t = 1; t < this.fXsize - 1; t++) str += "____|";
                str += "\r\n";

            }
            return str;
        }
    }
}
