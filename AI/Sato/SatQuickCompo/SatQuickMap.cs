using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {

    /// <summary>
    /// ゲーム木探索中で，着手の実行と巻き戻しを高速に行うことに特化した，派生型のMapクラス．
    /// 
    /// (1)インスタンス生成するときとcreateClone()するときは，普通のMapクラスより多分2，3倍重い．
    /// そのため，基本的にこのQuickMapクラスは，探索中でexecuteAction()とbackAction()の繰り返しで
    /// 使わないと探索が速くならない．
    /// 
    /// (2)元来のMapクラスのフィールドを意図的に隠ぺいしているため，
    /// この高速マップを普通のMapクラスにキャストしてフィールドやメソッドにアクセスすると高確率でバグる．
    /// 詳しくは「C#　継承　隠ぺい」でWeb検索
    /// 
    /// 
    /// </summary>
    partial class SatQuickMap : Map{
        //マップサイズや地形
        private new int fXsize;                         // マップのXサイズを表す変数．外部からマップファイルを読み込んだ時に決定する
        private new int fYsize;                         // マップのYサイズを表す変数．外部から読み込んだ時に決定する
        private new int[,] fMapFieldType;               // 地形の情報を表す       
        
        //マップ上のユニット関連
        private new SatQuickUnit[,] fMapUnit;                   // マップ上のユニット なければnull
        private new int[] fMaxUnitNum = null;     // チームごとの最大ユニット数　現在の仕様上，Nullでも平気
        private new SatQuickUnit[] fUnits;                      // ユニット集合 死んだら nullにして，詰めない
        private new int[] fNumOfAliveUnits = new int[2];      // 生存しているユニットの数

        //ターン経過や引き分け処理用
        private new int fTurnCount;                     // 現在のターン (各プレイヤが全ユニット行動終了ごと１ターン経過）
        private new int fTurnLimit;                     // 引き分け裁定が行われるターン
        private new int fDrawHPThreshold;               // HPの合計の差がこれより小さければ引き分け
        
        //ファイルのロード関連
        public new string fMapFileName;                 // ファイルから読み込んできたフアィイル
		public new bool reverse = false;                //先手後手の反転フラグ．


        /* Variables introduced for "Quick" processing */
        public int phaseColor    { get; set;}
        public int moveCount        { get; set; }  //Initially, 0. The number of unit-actions are executed.  
        public int numUnActedUnits  { get; set; }
        public int[] idHistoryOpUnit    { get; set; }
        public int[] idHistoryTarUnit   { get; set; }
        public int[] actionTypeHistory  { get; set; }
        public SatQuickUnit[][] teamUnits { get; set; }
        public bool applyDisableProcedure = false;
        
        private void showError_NotProvided(string notProvidedFunction){
            SAT_FUNC.WriteLine("Error. Not Equipped<"+notProvidedFunction+"> in SatQuickMap class.");
            int x = 0;
            x = 1/x;    //Stop running by "divided by 0 exception".
        }


        // 空コンストラクタ
        // このクラスのインスタンスを作るときは，
        //staticメソッドの「MapToQuickMap」（分割クラスファイルにて記述）を使うしか方法は提供していない．
        public SatQuickMap() {
        }

        #region ■■情報を取得する関数，ゲッター■■

        public new int getXsize() {
            return this.fXsize;
        }

        public new int getYsize() {
            return this.fYsize;
        }

        // 全てのチームのユニットが格納された配列を取得
        public new SatQuickUnit[] getUnits() {
            return this.fUnits;
        }

        // 引数（Id）で指定されたユニットを取得  version 0.104 より，範囲外の場合 null を返すことに
        public new SatQuickUnit getUnit(int unitId) {
            if (unitId < 0 || unitId >= fUnits.Length) {
                return null;
            } else {
                return this.fUnits[unitId];
            }
        }

        // ある位置にあるユニットを取得
        public new SatQuickUnit getUnit(int x, int y) {
            return this.fMapUnit[x, y];
        }

        // マップ→ユニットのマトリクスを全て取得
        public new SatQuickUnit[,] getMapUnit() {
            return this.fMapUnit;
        }

        // 地形のマトリクスを全て取得
        public new int[,] getFieldTypeArray() {
            return this.fMapFieldType;
        }

        // ある位置の地形を取得
        public new int getFieldType(int x, int y) {
            return this.fMapFieldType[x, y];
        }

        public new int getTurnCount() {
            return this.fTurnCount;
        }

        public new int getTurnLimit() {
            return this.fTurnLimit;
        }

        public new int getDrawHPThreshold() {
            return this.fDrawHPThreshold;
        }

        // 引数チームの残りユニット数を返す
        public new int getNumOfAliveColorUnits(int teamColor) {
            return fNumOfAliveUnits[teamColor];
        }

        //フィールドの防御適正を取得
        public new int getFieldDefensiveEffect(int x, int y) {
            return Consts.sFieldDefense[fMapFieldType[x, y]];
        }
        #endregion

        // 引数で指定されたチームの全ユニットが行動を終了しているか？ HumanPlayerクラスぐらいでしかあんま使わない．
        //他人のUCTメソッドはGetUnitList().Count==0でターンチェンジしてる．
        public new Boolean isAllUnitActionFinished(int teamColor) {
            if (teamColor != phaseColor) {
                SAT_FUNC.WriteLine("WARNING! Check -isAllUnitActionFinished- method, @SatQuickMap Class." + teamColor);
                if (getUnitsList(teamColor, false, true, false).Count == 0) {
                    return true;
                }
                else {
                    return false;
                }
            }
            return (numUnActedUnits == 0);
        }


        public void initMovableArea_AllUnit() {
            for (int i = 0; i < fUnits.Length; i++) {
                if (fUnits[i] == null) { continue; }
                fUnits[i].initMovableMap(0);
                //DEBUG
                //SAT_FUNC.WriteLine(fUnits[i].getRouteMapString());

            }
        }

        public void reloadMovableAreas_AllEnemy(int phaseColor, int fromX, int fromY, int destX, int destY) {
            int eneColor = SAT_FUNC.REVERSE_COLOR(phaseColor);

            if (fromX == destX && fromY == destY) { return; }

            for (int i = 0; i < teamUnits[eneColor].Length; i++) {
                SatQuickUnit eneU = teamUnits[eneColor][i];
                if (eneU == null || eneU.Dead) { continue; }

                //SAT_FUNC.WriteLine(eneU.getRouteMapString());

                //破壊されたユニットとかは，行動のfromXなどが0以下なときがあるのでガード．
                if (fromX > 0) { eneU.routeReload_enemyLeave(fTurnCount, fromX, fromY, destX, destY); }
                if (destX > 0) { eneU.routeReload_enemyEnter(fTurnCount, destX, destY); }
 
            }
        }
        


        /* 参考：GameManagerクラスでのターンチェンジメソッド
         private void changePhase() {
            fMap.enableUnitsAction(fPhase); // 全ての引数チームのユニットの行動を可能にする

            fDrawManager.reDrawMap(fMap);
            fSgfManager.addLogOfOneTurn(fPhase); // １ターンが終了したのでデータ保存

            fMap.incTurnCount();//ターン数をインクリメント
            fPhase = (fPhase + 1) % 2;//ターンチェンジ
        }
        */


        /// <summary>
        /// 対応したincludingのユニットリストを返します．重複が起きた場合は一つにまとめられます．
        /// TODO なんらかの形でさらに高速化？
        /// </summary>
        /// <param name="teamColor"></param>
        /// <param name="includingMyActionFinishedUnits">自軍のアクションが終了したユニットが欲しい場合</param>
        /// <param name="includingMyMovableUnits">自軍の行動が可能なユニットが欲しい場合</param>
        /// <param name="includingOponentUnits">敵ユニットが欲しい場合</param>
        /// <returns>対応したユニットリスト</returns>
        public new List<SatQuickUnit> getUnitsList(int teamColor, bool includingMyActionFinishedUnits, 
                                                   bool includingMyMovableUnits, bool includingOponentUnits) {
            List<SatQuickUnit> unitsList = new List<SatQuickUnit>();

            //TODO 新規のTeamUnits[,]配列を使って高速化！　=> やっぱいいや．なくても別個にTeamUnits配列呼べば代用効くし．
            for (int i = 0; i < fUnits.Length; i++) {
                if (fUnits[i] == null) continue;
                if (fUnits[i].Dead) continue;
                if (includingMyActionFinishedUnits && fUnits[i].isActionFinished() && fUnits[i].getTeamColor() == teamColor) {
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

        public new void executeAction(Action a) {
            showError_NotProvided("executeAction(Action)");
        }

        public void exeAction_quick_MoveAtk(int act, bool reviseEnemyRouteMap_MOV_ATK) {
            exeAction_quick(act, reviseEnemyRouteMap_MOV_ATK, false);
        }
        public void exeAction_quick_TurnChange(int act, bool prepareFriendRouteMap) {
            exeAction_quick(act, false, prepareFriendRouteMap);
        }


        // マップにアクションを適用する関数，木探索などのために利用できる．
        //　（ラッパー版．本体は下に．）
        private void exeAction_quick(int act, bool reviseEnemyRouteMap_MOV_ATK,
                   bool prepareFriendRouteMap_TURNCHANGE
                 ) {
            exeAction_quick_aux(act, prepareFriendRouteMap_TURNCHANGE);
            moveCount++;
            int actType = SatQuickAction.getActType(act);
            if (actType == SatQuickAction.ACTIONTYPE_TURNEND) {
                return;
            }
            if (actType == SatQuickAction.ACTIONTYPE_MOVEONLY) {
                if (reviseEnemyRouteMap_MOV_ATK) {
                    reloadMovableAreas_AllEnemy(
                        phaseColor,
                        SatQuickAction.getFromX(act), SatQuickAction.getFromY(act),
                        SatQuickAction.getDestX(act), SatQuickAction.getDestY(act)
                        );
                }
            }
            if (actType == SatQuickAction.ACTIONTYPE_MOVEANDATTACK) {
                //反撃で死んでるかもしれないので，DestX-Yでなく現在のXposやYposを入れる．
                SatQuickUnit u = fUnits[SatQuickAction.getOpID(act)];
                if (reviseEnemyRouteMap_MOV_ATK) {
                    reloadMovableAreas_AllEnemy(
                        phaseColor,
                        SatQuickAction.getFromX(act), SatQuickAction.getFromY(act),
                        u.getXpos(), u.getYpos());
                }
            }
        }

        // マップにアクションを適用する関数，木探索などのために利用できる．
        // HACK さて．引数にactionType.SURRENDERが来ることは想定してない．どうせ来ないと思うので，高速化のため対処を省略．        
        private void exeAction_quick_aux(int act, bool prepareFriendRouteMap_TURNCHANGE) {
            int actType = SatQuickAction.getActType(act);
            actionTypeHistory[moveCount] = actType;

            if (actType == Action.ACTIONTYPE_TURNEND) {
                SatQuickUnit u = null;
                //全生存ユニの行動終了フラグをfalseに．（行動可能に戻してあげる）
                for (int i = 0; i < teamUnits[phaseColor].Length; i++) {
                    u = teamUnits[phaseColor][i];
                    if (u==null || u.Dead) { continue; }
                    u.recordAttackFinishFlagHist(moveCount);
                    u.setActionFinished(false);
                }
                //ターン数と総Move数の加算．ターン色の入れ替え．
                fTurnCount++;
                phaseColor = SAT_FUNC.REVERSE_COLOR(phaseColor);

                //行動可能ユニット数の加算
                // HACK 行動可能ユニットが残ってるのにターンエンドするとバグるので，必ず行動しきってからターンを変える．
                if (fTurnCount == 1) {  //turn==1だけ特別なのは，最初のターンだけ行動不能な後攻側ユニットの可能性を想定して
                    numUnActedUnits = 0;
                    for (int i = 0; i < teamUnits[phaseColor].Length; i++) {
                        u = teamUnits[phaseColor][i];
                        if (u == null || u.Dead) { continue; }
                        if (!u.isActionFinished()) { numUnActedUnits++; }
                    }
                }else {
                    numUnActedUnits = fNumOfAliveUnits[phaseColor];
                }

                //行動可能エリア配列を次ターン用にコピーなり再計算なりしてあげる．＃既にPhaseColorが反転してる事に注意．
                //※フラグにより処理事態をスキップしたり，スキップしなかったりする．
                foreach (SatQuickUnit eneU in teamUnits[phaseColor]) { //：これスキップできるけど，一応軽いので毎回やっとく．
                    eneU.routeMap_step[fTurnCount] = eneU.routeMap_step[fTurnCount - 1];
                    eneU.routeMap_compass[fTurnCount] = eneU.routeMap_compass[fTurnCount - 1];
                }
                if (prepareFriendRouteMap_TURNCHANGE) {
                    foreach (SatQuickUnit myU in teamUnits[SAT_FUNC.REVERSE_COLOR(phaseColor)]) {
                        myU.initMovableMap(fTurnCount);
                    }
                }
                return;
            }

            //## a.ActionType == MOVE_ONLY or MOVE_AND_ATTACK　##
            //###################################################
            // どちらにせよ移動はする（その場移動であっても）ので移動行動用の処理を共通処理としてこなす．
            numUnActedUnits--;

            int opUnitID = SatQuickAction.getOpID(act);
            SatQuickUnit op_unit = fUnits[opUnitID];

            idHistoryOpUnit[moveCount] = opUnitID;

            //元の位置を記録して，位置をずらす
            // HACK 攻撃行動の場合は，反撃ダメージで死ぬ場合のみ，この処理が無駄になる．一応その時間損失は許容する．
            op_unit.recordXYhist(moveCount);
            op_unit.setPos(SatQuickAction.getDestX(act), SatQuickAction.getDestY(act));
            op_unit.setActionFinished(true);

            //マップ座標上のユニット情報更新
            fMapUnit[SatQuickAction.getFromX(act), SatQuickAction.getFromY(act)] = null;
            fMapUnit[SatQuickAction.getDestX(act), SatQuickAction.getDestY(act)] = op_unit;
            
            
            if (actType == Action.ACTIONTYPE_MOVEONLY) {
                return; //移動のみアクションの場合はここでサヨナラ．
            }

            //## a.ActionType == MOVE_AND_ATTACK ##
            //#####################################
            int tarUnitID = SatQuickAction.getTarID(act);
            SatQuickUnit tar_unit = fUnits[tarUnitID];

            idHistoryTarUnit[moveCount] = tarUnitID;

            //行動前に情報を記録
            op_unit.recordHPhist(moveCount);    //operation unitの位置情報は上の方で既に記録してある．
            tar_unit.recordAllStatus(moveCount);

            //攻撃ユニット　→　防御ユニット　のダメージ処理
            int targetStar = getFieldDefensiveEffect(tar_unit.getXpos(), tar_unit.getYpos());
            int damage_cause = DamageCalculator.calculateDamage(
                            op_unit.getSpec(), op_unit.getHP(), tar_unit.getSpec(), tar_unit.getHP(), targetStar);
            tar_unit.reduceHP(damage_cause);
                        
            //破壊処理（攻撃を受けた側）
            if (tar_unit.getHP() <= 0) {
                int beforeX = tar_unit.getXpos();//7行↓で必要．
                int beforeY = tar_unit.getYpos();

                tar_unit.Dead = true;
                fMapUnit[tar_unit.getXpos(), tar_unit.getYpos()] = null;
                tar_unit.setPos(-1, -1);

                //友軍の移動範囲情報更新（※邪魔な敵ユニットが消えたことで味方の移動範囲が広がる．）
                reloadMovableAreas_AllEnemy(
                    SAT_FUNC.REVERSE_COLOR(phaseColor),//敵色を入れる．
                    beforeX, beforeY, //fromX, fromY
                    -1, -1 //destX, destY
                );
                
                fNumOfAliveUnits[tar_unit.getTeamColor()]--;
                return; //一撃で破壊できたなら，ここでサヨナラ．
            }

            if (tar_unit.IsCannon || op_unit.IsCannon) {
                return; //相手か自分が遠距離攻撃ユニットの場合も，ここでサヨナラ．
            }

            //攻撃ユニット　←　防御ユニット　（つまり反撃）のダメージ処理
            int operateStar = getFieldDefensiveEffect(op_unit.getXpos(), op_unit.getYpos());
            int damage_receive = DamageCalculator.calculateDamage(
                            tar_unit.getSpec(), tar_unit.getHP(), op_unit.getSpec(), op_unit.getHP(), operateStar);
            op_unit.reduceHP(damage_receive);

            //破壊処理（攻撃した側）　自ユニット死による敵ユニの移動可能範囲拡大は，上の階層で処理するのでここでは不要
            if (op_unit.getHP() <= 0) {
                op_unit.Dead = true;
                fMapUnit[op_unit.getXpos(), op_unit.getYpos()] = null;
                op_unit.setPos(-1, -1);
                fNumOfAliveUnits[op_unit.getTeamColor()]--;
            }
        }

        //盤の状態を一手戻すメソッド．Min-Max型探査のときは，これで手を巻き戻すと高速化する．
        public void backTurnChangeAct() {
            backAtkMoveAct(false);
        }


        //盤の状態を一手戻すメソッド．Min-Max型探査のときは，これで手を巻き戻すと高速化する．
        public void backAtkMoveAct(bool /*着手時に敵駒の移動マップ更新をしたか*/ revisedEnemyRouteMap_MOV_ATK) {
            moveCount--;
            int lastActType = actionTypeHistory[moveCount];
            
            if (lastActType == Action.ACTIONTYPE_TURNEND) {
                //手番プレイヤー切り替えて，経過ターン数減らして，片チームのユニット行動フラグ全復元
                phaseColor = SAT_FUNC.REVERSE_COLOR(phaseColor);
                fTurnCount--;
                for (int i = 0; i < teamUnits[phaseColor].Length; i++) {
                    teamUnits[phaseColor][i].restorePastStatus_ActFinishFlag(moveCount);
                }
                numUnActedUnits = 0;    // HACK 未行動ユニ有りのターン終了によりバグるが，まぁ今はこれで良いことにする．
                return; //TurnEndを戻すときはここでサヨナラ．
            }

            //## 移動のみ行動・移動攻撃行動　共通処理 ##
            //##########################################
            numUnActedUnits++;
            SatQuickUnit op_unit = fUnits[idHistoryOpUnit[moveCount]];
            op_unit.setActionFinished(false);

            int fromX = op_unit.getXpos();
            int fromY = op_unit.getYpos();

            //位置の復元
            if (!op_unit.Dead) { 
                fMapUnit[fromX, fromY] = null; 
            }
            op_unit.restorePastStatus_XYpos(moveCount);
            
            int destX = op_unit.getXpos();
            int destY = op_unit.getYpos();
            fMapUnit[destX, destY] = op_unit;
            //全敵の移動エリアマップの巻き戻し（逆操作）
            if (revisedEnemyRouteMap_MOV_ATK) {
                reloadMovableAreas_AllEnemy(phaseColor, fromX, fromY, destX, destY);
            }

            if (lastActType == Action.ACTIONTYPE_MOVEONLY) {
                return; //移動のみ行動の巻き戻しなら，ここでサヨナラ．
            }
            
            //## 攻撃行動　の状態復元　##
            //###########################
            SatQuickUnit tar_unit = fUnits[idHistoryTarUnit[moveCount]];
            
            //被攻撃ユニットの状態再生処理
            tar_unit.restorePastStatus(moveCount);
            if (tar_unit.Dead) {
                tar_unit.Dead = false;
                fMapUnit[tar_unit.getXpos(), tar_unit.getYpos()] = tar_unit;
                fNumOfAliveUnits[tar_unit.getTeamColor()]++;
                //邪魔な位置への敵ユニット復活により，味方ユニットの移動範囲が再び狭まる（かもしれない）．
                reloadMovableAreas_AllEnemy(
                    SAT_FUNC.REVERSE_COLOR(phaseColor), //敵色に変えて，入れる．
                    -1,-1, //fromX, fromY
                    tar_unit.getXpos(), tar_unit.getYpos() //destX, destY
                );
            }
            //攻撃ユニットのHP再生処理
            op_unit.restorePastStatus_HP(moveCount);
            if (op_unit.Dead) {
                op_unit.Dead = false;
                fNumOfAliveUnits[op_unit.getTeamColor()]++;
            }
        }

       
        public new void setTurnCount(int turn) {
            fTurnCount = turn;
        }

        public new void incTurnCount() {
            fTurnCount++;
        }






        public new string toString() {
            string str = "";
            str += "mvCt:" + moveCount + ", trunCt:" + fTurnCount 
                + ", numUnActed"+numUnActedUnits +", pahseC:"+phaseColor+"\r\n";
            str += "_";
            for (int t = 1; t < this.fXsize - 1; t++) str += "_____";
            str += "\r\n";

            for (int y = 1; y < this.fYsize - 1; y++) {
                str += "|";
                for (int x = 1; x < this.fXsize - 1; x++) {
                    if (this.fMapUnit[x, y] == null) {
                        str += "     |";
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
                    if (fMapUnit[x, y].isActionFinished()) {
                        str += ".";
                    } else {
                        str += " ";
                    }
                    str += "|";
                }

                str += "\r\n";
                str += "|";
                for (int t = 1; t < this.fXsize - 1; t++) str += "_____|";
                str += "\r\n";

            }
            return str;
        }
    }

    
}
