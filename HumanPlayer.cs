using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace SimpleWars {
    /// <summary>
    /// 人間プレイヤのマウス操作から Playerインタフェースの makeAction を実装するクラス．
　　/// 一般のユーザは読む・理解する必要はありません．
    /// </summary>
    class HumanPlayer : Player {

        // マウスがクリックされている状態を表す定数．
        public const int MOUSESTATE_PANEL_NOTMOUSEDOWN = 0;   // パネルがクリックされていない状態
        public const int MOUSESTATE_UNIT_CLICKED = 1;         // ユニットが一度、クリックされた状態
        public const int MOUSESTATE_UNIT_MOVE_FINISHED = 2;   // ユニットを移動し終わった状態
        public const int MOUSESTATE_SHOW_CAN_ATTACK_UNIT = 3; // ユニットを動かし終わって攻撃可能ユニットが選択できる状態

        Action fNowAction;
        MainForm fMainForm;
        Map fMap;
        GameManager fGameManager;
        DrawManager fDrawManager;
        private int fTeamColor;
        private bool[,] fSelectRange;
        bool[,] showUnitCanAttackPosBoolAry;// 対応するインデックスのフラグが立っていると選択可能枠を表示する
        public bool fPictureBoxPushedFlag = false;
        public bool fActionFinishFlag = false;
        public bool fActionDecidedFlag = false;
        public bool fTurnEndFlag = false;
        public bool fResignationFlag = false;
        public bool fGameEndFlag = false;
        private int fMouseState;

        public HumanPlayer(MainForm aForm, GameManager gameManager) {
            this.fMainForm = aForm;
            this.fGameManager = gameManager;
            this.fDrawManager = gameManager.fDrawManager;
            this.fNowAction = new Action();
        }

		//ユーザーログ変数　プロパティ
		public int userNumOfCombatLog {
			get { return 0; }
			set {  }
		}

        public HumanPlayer() {
            this.fNowAction = new Action();
        }

        public void setDrawManager(DrawManager drawManager) {
            this.fDrawManager = drawManager;
        }

        public void setGameManager(GameManager gameManager) {
            this.fGameManager = gameManager;
        }

        public void setMainForm(MainForm mainForm) {
            this.fMainForm = mainForm;
        }

        public void setGameEndFlag(bool endFlag) {
            this.fGameEndFlag = endFlag;
        }

        public Action makeAction(Map map, int teamColor, bool turnStart,bool gameStart) {
            this.fMap = map;
            showUnitCanAttackPosBoolAry = new bool[fMap.getXsize(), fMap.getYsize()];
            this.fTeamColor = teamColor;
            fMainForm.pictureBox.MouseDown += new MouseEventHandler(fMainForm.pictureBox_MouseDown);
            fMainForm.pictureBox.MouseMove += new MouseEventHandler(fMainForm.pictureBox_MouseMove);

            fActionFinishFlag = false;
            fMouseState = MOUSESTATE_PANEL_NOTMOUSEDOWN;
            // プレイヤーの入力待ち
            while (true) {
                //Thread.Sleep(100);
                Application.DoEvents();
                if (fTurnEndFlag || fGameEndFlag || fResignationFlag) {// ターンエンド，ゲームエンドフラグが立っていると未行動ユニットのアクションをNoMoveに決定する
                    fNowAction = decideOneUnitActNoMove();
                    fActionFinishFlag = true;
                }
                if (fActionFinishFlag) { // MainFormでパネルがクリックされるかUNDOが押されるとbreakする
                    break;
                }
            }
            fMainForm.pictureBox.MouseDown -= new MouseEventHandler(fMainForm.pictureBox_MouseDown);
            fMainForm.pictureBox.MouseMove -= new MouseEventHandler(fMainForm.pictureBox_MouseMove);
            return fNowAction;
        }

        public string getName() {
            return "HumanPlayer";
        }

        // version 0.104より追加．ゲーム開始時に呼ばれる．何もしなくとも良い．
        public void gameStarted() {
        }

        // version 0.104より追加．ターン開始時に呼ばれる．何もしなくとも良い．
        public void turnStarted() {
        }

        public string showParameters() {
            return "";
        }

        // 1ユニットの行動をNoMoveに決定する
        public Action decideOneUnitActNoMove() {
            Action nowAct = new Action();
            List<Unit> movableUnits = fMap.getUnitsList(fTeamColor, false, true, false);

            if (movableUnits.Count != 0) {
                nowAct.actionType = Action.ACTIONTYPE_MOVEONLY;
                nowAct.destinationXpos = movableUnits[0].getXpos();
                nowAct.destinationYpos = movableUnits[0].getYpos();
                nowAct.fromXpos = movableUnits[0].getXpos();
                nowAct.fromYpos = movableUnits[0].getYpos();
                nowAct.operationUnitId = movableUnits[0].getID();
            }
            return nowAct;
        }

        /*パネル上でマウスムーブが発生したとき呼び出される
         * 
        */
        public void pictureBoxMouseMoved(int mouseMovedXpos, int mouseMovedYpos) {
			if (mouseMovedXpos >= fMap.getXsize() || mouseMovedYpos >= fMap.getYsize()) {
				Logger.showUnitStatus(" ");
				return; 
			}
            Unit[,] mapUnit = fMap.getMapUnit();
            if (mapUnit[mouseMovedXpos, mouseMovedYpos] == null) {
				Logger.showUnitStatus(" ");
				return; 
			}

            String str = mapUnit[mouseMovedXpos, mouseMovedYpos].toString();
            Logger.showUnitStatus(str);
        }

        /*  パネルがクリックされたとき、その位置にユニットが存在すると
         *  そのユニットの移動可能範囲を表示する
        */
        public void pictureBoxPushed(int selectXpos, int selectYpos) {
            //if (isClickedOpponentUnit(selectXpos, selectYpos)) { return; }
            if (selectXpos >= fMap.getXsize() || selectYpos >= fMap.getYsize()) return;
            Unit[,] mapUnit = fMap.getMapUnit();
            Unit opUnit;
            List<Unit> attackAbleUnitList;
            bool isOpUnitCanAttackEnUnit;// opUnitが敵に移動後・前含めて攻撃可能なら立てるフラグ
            //Unit cUnit;

            switch (fMouseState) {
                /******************************************/
                /*ユニットが一度もクリックされていない状態*/
                /******************************************/
                case MOUSESTATE_PANEL_NOTMOUSEDOWN:
                    // 相手ユニットをクリックしても何も起らない
                    if (isClickedOpponentUnit(selectXpos, selectYpos)) {
                        opUnit = mapUnit[fNowAction.fromXpos, fNowAction.fromYpos];
                        //行動ユニットの設定
                        fNowAction.fromXpos = mapUnit[selectXpos, selectYpos].getXpos();
                        fNowAction.fromYpos = mapUnit[selectXpos, selectYpos].getYpos();
                        fNowAction.operationUnitId = mapUnit[selectXpos, selectYpos].getID();
                        fNowAction.teamColor = mapUnit[selectXpos, selectYpos].getTeamColor();
                        //移動レンジ表示
                        fSelectRange = RangeController.getReachableCellsMatrix(mapUnit[selectXpos, selectYpos], fMap);
                        fDrawManager.showUnitMovablePos(fSelectRange, fMap);
                        break;
                    }

                    // もし押された位置にユニットがいれば、またはユニットの行動が終了していなければ
                    if (mapUnit[selectXpos, selectYpos] != null && !mapUnit[selectXpos, selectYpos].isActionFinished()) {
                        opUnit = mapUnit[fNowAction.fromXpos, fNowAction.fromYpos];
                        //行動ユニットの設定
                        fNowAction.fromXpos = mapUnit[selectXpos, selectYpos].getXpos();
                        fNowAction.fromYpos = mapUnit[selectXpos, selectYpos].getYpos();
                        fNowAction.operationUnitId = mapUnit[selectXpos, selectYpos].getID();
                        fNowAction.teamColor = mapUnit[selectXpos, selectYpos].getTeamColor();
                        //移動レンジ表示
                        fSelectRange = RangeController.getReachableCellsMatrix(mapUnit[selectXpos, selectYpos], fMap);
                        fDrawManager.showUnitMovablePos(fSelectRange, fMap);
                        //行動状態の変更
                        fMouseState = MOUSESTATE_UNIT_CLICKED;
                    }
                    break;
                /******************************************/
                /*******ユニットがクリックされた状態*******/
                /******************************************/
                case MOUSESTATE_UNIT_CLICKED:
                    opUnit = mapUnit[fNowAction.fromXpos, fNowAction.fromYpos];
                    isOpUnitCanAttackEnUnit = isExistAttackableEnemyUnit(opUnit);// opUnitが敵に(移動後・前含めて攻撃可能ならフラグをたてる
                    //attackAbleUnitList = RangeController.getAttackAbleEnemiesList(opUnit, fMap);// ユニットが選択されて,動かす前の攻撃可能敵ユニットリスト
                    attackAbleUnitList = getAttackAbleUnits(opUnit);

                    // ユニットを動かさず，攻撃可能敵ユニットが存在しない場合
                    if (selectXpos == fNowAction.fromXpos && selectYpos == fNowAction.fromYpos
                        && isOpUnitCanAttackEnUnit == false) {
                        // ユニットを動かさないので移動先と移動前の位置は同じ
                        fNowAction.destinationXpos = selectXpos;
                        fNowAction.destinationYpos = selectYpos;
                        //行動状態の変更
                        fNowAction.actionType = Action.ACTIONTYPE_MOVEONLY;
                        initSelectRange();
                        fDrawManager.reDrawMap(fMap);

                        fActionFinishFlag = true;
                        return;
                    }

                    // ユニットを動かさず，攻撃可能敵ユニットが存在する場合
                    if (selectXpos == fNowAction.fromXpos && selectYpos == fNowAction.fromYpos
                        && isOpUnitCanAttackEnUnit == true) {
                        // ユニットを動かさないので移動先と移動前の位置は同じ
                        fNowAction.destinationXpos = selectXpos;
                        fNowAction.destinationYpos = selectYpos;

                        attackAbleUnitList = getAttackAbleUnits(opUnit);// 攻撃可能ユニットを抽出
                        initSelectRange();
                        enableUnitsCanSelect(attackAbleUnitList);
                        fDrawManager.showAttackTargetUnits(fMap, attackAbleUnitList);
                        //行動状態変更
                        fMouseState = MOUSESTATE_SHOW_CAN_ATTACK_UNIT;
                        return;
                    }

                    // 操作したユニットが移動後に攻撃不可能（間接タイプ）な場合
                    if (fSelectRange[selectXpos, selectYpos]
                        && !opUnit.getSpec().isDirectAttackType()) {
                            fMap.changeUnitLocation(selectXpos, selectYpos, opUnit);
                        // 移動先の位置を代入
                        fNowAction.destinationXpos = selectXpos;
                        fNowAction.destinationYpos = selectYpos;
                        fNowAction.actionType = Action.ACTIONTYPE_MOVEONLY;
                        initSelectRange();
                        fDrawManager.reDrawMap(fMap, fSelectRange);
                        fActionFinishFlag = true;
                        return;
                    }

                    // ユニットが移動した場所で，攻撃可能ユニットが存在するかを見るために，フラグを更新
                    isOpUnitCanAttackEnUnit = isExistAttackableEnemyUnit(selectXpos, selectYpos, opUnit);

                    // 操作したユニットが移動後に攻撃可能タイプで，攻撃可能ユニットが存在しない場合
                    if (fSelectRange[selectXpos, selectYpos] && opUnit.getSpec().isDirectAttackType()
                        && isOpUnitCanAttackEnUnit == false) {
                        fMap.changeUnitLocation(selectXpos, selectYpos, opUnit);//位置変更
                        // 移動先の位置を代入
                        fNowAction.destinationXpos = selectXpos;
                        fNowAction.destinationYpos = selectYpos;
                        fNowAction.actionType = Action.ACTIONTYPE_MOVEONLY;// moveOnlyに行動決定
                        initSelectRange();
                        fDrawManager.reDrawMap(fMap, fSelectRange);
                        fActionFinishFlag = true;
                        return;
                    }

                    // 操作したユニットが移動後に攻撃可能タイプで，攻撃可能ユニットが存在した場合
                    if (fSelectRange[selectXpos, selectYpos] && opUnit.getSpec().isDirectAttackType()
                        && isOpUnitCanAttackEnUnit == true) {
                        fMap.changeUnitLocation(selectXpos, selectYpos, opUnit);//位置変更
                        // 移動先の位置を代入
                        fNowAction.destinationXpos = selectXpos;
                        fNowAction.destinationYpos = selectYpos;
                        //攻撃可能ユニットの取得と表示
                        attackAbleUnitList = getAttackAbleUnits(opUnit);// 攻撃可能ユニットを抽出.ただし移動後の位置での攻撃可能ユニット
                        initSelectRange();
                        enableUnitsCanSelect(attackAbleUnitList);
                        fDrawManager.showAttackTargetUnits(fMap, attackAbleUnitList);
                        //行動状態変更
                        fMouseState = MOUSESTATE_SHOW_CAN_ATTACK_UNIT;
                        return;
                    }
                    break;

                /******************************************/
                /********攻撃範囲を表示している状態********/
                /******************************************/
                case MOUSESTATE_SHOW_CAN_ATTACK_UNIT:
                    opUnit = fMap.getUnit(fNowAction.operationUnitId);

                    if (mapUnit[selectXpos, selectYpos] == null) break;
                    // 自分をクリックしたら、行動を終了する（AttackMove_Only）
                    if (opUnit.getXpos() == selectXpos && opUnit.getYpos() == selectYpos) {
                        fNowAction.actionType = Action.ACTIONTYPE_MOVEONLY;
                        initBooleanArray();//
                        initSelectRange();
                        fDrawManager.reDrawMap(fMap);
                        fActionFinishFlag = true;
                        return;
                    }

                    // もし攻撃可能ユニットがクリックされていれば
                    if (showUnitCanAttackPosBoolAry[selectXpos, selectYpos]) {
                        int[] anticipationDamages = DamageCalculator.calculateDamages(opUnit,mapUnit[selectXpos,selectYpos],fMap);
						//BattleResultの呼び出し
                        if (Option.IsCombatResultShowed&&BattleResult.setBattleResult(opUnit.getHP(), mapUnit[selectXpos, selectYpos].getHP(), opUnit.getTypeOfUnit(), mapUnit[selectXpos, selectYpos].getTypeOfUnit(), anticipationDamages, fTeamColor) == false) {
                            initSelectRange();

                            initBooleanArray();//
                            fMap.changeUnitLocation(fNowAction.fromXpos, fNowAction.fromYpos, fMap.getUnit(fNowAction.operationUnitId));
                            fDrawManager.initBoolArray();
                            fDrawManager.reDrawMap(fMap);
                            initAction();
                            fMouseState = MOUSESTATE_PANEL_NOTMOUSEDOWN;
                            break;
                        }

                        fNowAction.attackXpos = selectXpos;
                        fNowAction.attackYpos = selectYpos;
                        fNowAction.targetUnitId = mapUnit[selectXpos, selectYpos].getID();
                        fNowAction.actionType = Action.ACTIONTYPE_MOVEANDATTACK;
                        initBooleanArray();//
                        initSelectRange();
                        fDrawManager.reDrawMap(fMap);
                        fActionFinishFlag = true;
                        return;
                    }
                    break;
                default:
                    break;
            }
        }

        // MainFormのパネル上で右クリックが押された
        public void pictureBoxRightMousePushed() {

            switch (fMouseState) {
                // ユニットが一度もクリックされていない状態
                case MOUSESTATE_PANEL_NOTMOUSEDOWN:
                    initSelectRange();
                    fDrawManager.reDrawMap(fMap);
                    initAction();
                    fMouseState = MOUSESTATE_PANEL_NOTMOUSEDOWN;
                    break;
                // ユニットがクリックされた状態(マップ上には移動可能範囲が描かれている)
                case MOUSESTATE_UNIT_CLICKED:
                    initSelectRange();
                    fDrawManager.reDrawMap(fMap);
                    initAction();
                    fMouseState = MOUSESTATE_PANEL_NOTMOUSEDOWN;
                    break;
                // ユニットを動かした位置で隣接した位置に相手ユニットが存在する状態
                case MOUSESTATE_SHOW_CAN_ATTACK_UNIT:
                    initSelectRange();
                    Unit opUnit = fMap.getUnit(fNowAction.operationUnitId);

                    initBooleanArray();//
                    fMap.changeUnitLocation(fNowAction.fromXpos, fNowAction.fromYpos, opUnit);
                    fDrawManager.initBoolArray();
                    fDrawManager.reDrawMap(fMap);
                    initAction();
                    fMouseState = MOUSESTATE_PANEL_NOTMOUSEDOWN;
                    break;
                default:
                    break;
            }
        }

        // ユニット攻撃選択可能フラグを立てる
        private void enableUnitsCanSelect(List<Unit> canSelectAndAttackUnits) {
            int x, y;
            for (int i = 0; i < canSelectAndAttackUnits.Count; i++) {
                x = canSelectAndAttackUnits[i].getXpos();
                y = canSelectAndAttackUnits[i].getYpos();
                showUnitCanAttackPosBoolAry[x, y] = true;
            }
        }

        // 相手のユニットをクリックしていたらtrueを返す
        private Boolean isClickedOpponentUnit(int selectX, int selectY) {
            Unit[,] mapUnit = fMap.getMapUnit();
            if (mapUnit[selectX, selectY] == null) { return false; }

            if (mapUnit[selectX, selectY].getTeamColor() != this.fTeamColor) { return true; }

            return false;
        }

        // opUnitが今いる位置で攻撃可能ならtrueを返す
        private bool isExistAttackableEnemyUnit(Unit opUnit) {
            List<Unit> canAttackableEnemys = getAttackAbleUnits(opUnit);
            if (canAttackableEnemys.Count() == 0) {
                return false;
            }
            return true;
        }

        // x,yに移動後で，攻撃可能敵ユニットがいれば，true を返す
        private bool isExistAttackableEnemyUnit(int x,int y,Unit opUnit) {
            Unit copyU = opUnit.createDeepClone();
            copyU.setXpos(x);
            copyU.setYpos(y);
            
            List<Unit> canAttackableEnemys = getAttackAbleUnits(copyU);
            if (canAttackableEnemys.Count() == 0) {
                return false;
            }
            return true;
        }

        // 隣接攻撃可能なユニットのリストを返す
        // opUnitが現在いる場所で攻撃可能な敵ユニットを返す
        private List<Unit> getAttackAbleUnits(Unit opUnit) {
            List<Unit> attackAbleUnits = new List<Unit>();

            // 近接攻撃ユニットの場合は現在位置の上下左右に存在する敵ユニットを抽出する
            if (opUnit.getSpec().isDirectAttackType()) {
                int posX = opUnit.getXpos();
                int posY = opUnit.getYpos();
                // 右
                if (fMap.getUnit(posX + 1, posY) != null && fMap.getUnit(posX + 1, posY).getTeamColor()
                    != opUnit.getTeamColor() && isEffective(opUnit, fMap.getUnit(posX + 1, posY))) {
                    attackAbleUnits.Add(fMap.getUnit(posX + 1, posY));
                }
                // 左
                if (fMap.getUnit(posX - 1, posY) != null && fMap.getUnit(posX - 1, posY).getTeamColor()
                    != opUnit.getTeamColor() && isEffective(opUnit, fMap.getUnit(posX - 1, posY))) {
                    attackAbleUnits.Add(fMap.getUnit(posX - 1, posY));
                }
                // 下
                if (fMap.getUnit(posX, posY + 1) != null && fMap.getUnit(posX, posY + 1).getTeamColor()
                    != opUnit.getTeamColor() && isEffective(opUnit, fMap.getUnit(posX, posY + 1))) {
                    attackAbleUnits.Add(fMap.getUnit(posX, posY + 1));
                }
                // 上
                if (fMap.getUnit(posX, posY - 1) != null && fMap.getUnit(posX, posY - 1).getTeamColor()
                    != opUnit.getTeamColor() && isEffective(opUnit, fMap.getUnit(posX, posY - 1))) {
                    attackAbleUnits.Add(fMap.getUnit(posX, posY - 1));
                }

            } else {// 間接攻撃ユニットだった場合
                Spec uSpec = opUnit.getSpec();

                bool[,] attackAble = new bool[fMap.getXsize(), fMap.getYsize()];

                for (int x = 1; x < fMap.getXsize() - 1; x++) {
                    for (int y = 1; y < fMap.getYsize() - 1; y++) {
                        int dist = Math.Abs(x - opUnit.getXpos()) + Math.Abs(y - opUnit.getYpos()); //　各(x,y)座標と自分との距離
                        if (uSpec.getUnitMinAttackRange() <= dist && uSpec.getUnitMaxAttackRange() >= dist) {//攻撃可能範囲であれば
                            attackAble[x, y] = true;// 攻撃可能範囲に入ってる位置はtrueにする
                        }
                    }
                }

                // 攻撃範囲に含まれている敵ユニットをリストに追加する
                foreach (Unit enUnit in fMap.getUnitsList(opUnit.getTeamColor(), false, false, true)) {
                    int enXpos = enUnit.getXpos();
                    int enYpos = enUnit.getYpos();

                    if (attackAble[enXpos, enYpos] && isEffective(opUnit,enUnit)){// 攻撃範囲入っている敵ユニットはinRangeUnitsに追加する
                        attackAbleUnits.Add(enUnit);
                    }
                }
            }

            return attackAbleUnits;
        }

        // operationUnitがtargetUnitに攻撃可能かどうか．ダメージ効果0の場合にはfalseが返る
        private bool isEffective(Unit operationUnit, Unit targetUnit) {
            if (operationUnit.getSpec().getUnitAtkPower(targetUnit.getTypeOfUnit()) != 0) { return true; }
            return false;
        } 

        public void initAction() {
            fActionFinishFlag = false;
            this.fNowAction = null;
            fNowAction = new Action();
        }

        // selectRangeの初期化
        public void initSelectRange() {
            // version 0.104 パッチ．停止回避のため．
            if (fSelectRange == null) return;

            int i, j;
            for (i = 0; i < fMap.getXsize(); i++) {
                for (j = 0; j < fMap.getYsize(); j++) {
                    fSelectRange[i, j] = false;
                }
            }
        }

        // BooleanAryの初期化
        private void initBooleanArray() {
            int x, y;

            for (x = 0; x < fMap.getXsize(); x++) {
                for (y = 0; y < fMap.getYsize(); y++) {
                    showUnitCanAttackPosBoolAry[x, y] = false;
                }
            }
        }
        
        public void initMouseState() {
            this.fMouseState = MOUSESTATE_PANEL_NOTMOUSEDOWN;
        }
	}
}
