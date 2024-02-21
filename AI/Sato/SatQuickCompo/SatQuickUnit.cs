using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {
    /// <summary>
    /// 盤面高速処理のためのUnit派生クラス．
    /// QuickUnitという名前だが，情報量が削減されて軽量化されてるわけでなく，むしろフィールドがいろいろ増えてる．
    /// （HPや位置情報などをターン毎に記録しておくことによって，１ターン巻き戻し機能などに対応している）
    /// 
    /// !! WARNING: これも“Unit”クラスにキャストしない事．その理由の詳細は「C#　継承　隠ぺい」をネット検索．
    /// </summary>
    partial class SatQuickUnit : Unit{

        //ユニットのHP履歴や位置履歴の最大保持長さ．片チームのユニット数 * 読みターン深さ だけあれば十分
        public const int HISTORY_MAX_LENGTH = 32;

        private new int fXpos;               // マップ上のポジション
        private new int fYpos;               // マップ上のポジション
        private new int fHP;                 // ヒットポイント（0以下になると，ユニットはマップ上から消滅する）
        private new int fTeamColor; 　       // ユニットの所属するチームカラー
        private new int fId;                 // ユニットを判別するために現在使用中
        private new bool fActionFinished;    // 行動終了フラグ
        private new Spec fSpec;              // スペック(移動力・攻撃力など固定の値）

        private new int[] X_ints;            // 思考ルーチン等が使うための自由なint配列

        /* Variables introduced for "Quick" processing */
        private SatQuickMap map = null;  //The pointer to the map to access several Info.
        public bool Dead { get; set; }
        public int[] HpHistory {get; set;}
        public int[] XposHistory {get; set;}
        public int[] YposHistory {get; set;}
        public bool[] ActFinishedHistory {get; set;}

        public bool IsFlyer { get; set;}    //True if this unit is a fighter or attacker
        public bool IsInfantry { get; set;} //True if this unit is an infantry. 
        public bool IsCannon { get; set; } //True if this unit is an infantry. 

        public int MyFlexibleBuffer = 0;   //Keep 0 as possible. Use in any way you like.

        //To store information about movable areas
        public COMPASS[][,] routeMap_compass = new COMPASS[5][,];
        public int[][,] routeMap_step = new int[5][,];


        /// <summary>
        /// 例えば，moveCountが5のMap（5ターン目，ではなく，ユニット行動が5回実行された後，という意味）
        /// におけるユニットのHPやX，Y座標を記録しておく．
        ///
        /// これによって例えばmoveCntが6である局面から状態を1手分戻したいときに，
        /// LastHP[5]やLastXpos[5]を参照して盤の状態を元に戻せる．
        /// </summary>
        /// <param name="moveCnt"></param>
        public void recordAllStatus(int moveCnt){
            HpHistory[moveCnt] = fHP;
            XposHistory[moveCnt] = fXpos;
            YposHistory[moveCnt] = fYpos;
            //"行動済みフラグ"保存は一見無駄に見えるかもだが，「ゲーム開始時すでに行動済みなユニット」に関する処理で必要．
            ActFinishedHistory[moveCnt] = fActionFinished;        
        }

        public void recordHPhist(int moveCnt) {
            HpHistory[moveCnt] = fHP;
        }
        public void recordXYhist(int moveCnt) {
            XposHistory[moveCnt] = fXpos;
            YposHistory[moveCnt] = fYpos;
        }
        public void recordAttackFinishFlagHist(int moveCnt) {
            ActFinishedHistory[moveCnt] = fActionFinished;
        }


        /// <summary>
        /// 「MoveCnt回目の行動」終了時点でのMapにおけるユニットHPや位置を復元
        ///  ！このメソッド使用時は，ちゃんとMapのNumOfAliveUnitやMapUnit[,]の状態まで復元するよう注意！
        /// </summary>
        /// <param name="moveCnt"></param>
        public void restorePastStatus(int moveCnt){
            fHP = HpHistory[moveCnt];
            fXpos = XposHistory[moveCnt];
            fYpos = YposHistory[moveCnt];
            fActionFinished = ActFinishedHistory[moveCnt];            
        }

        public void restorePastStatus_HP(int moveCnt) {
            fHP = HpHistory[moveCnt];
        }
        public void restorePastStatus_XYpos(int moveCnt) {
            fXpos = XposHistory[moveCnt];
            fYpos = YposHistory[moveCnt];
        }

        public void restorePastStatus_ActFinishFlag(int moveCnt) {
            fActionFinished = ActFinishedHistory[moveCnt];
        }

        public static SatQuickUnit UnitToQuickUnit(Unit unit, SatQuickMap qMap) {
            SatQuickUnit quick_unit = new SatQuickUnit(
                unit.getXpos(),         unit.getYpos(), unit.getID(), 
                unit.getTeamColor(),    unit.getHP(),   (unit.isActionFinished())? 1 : 0,
                unit.getSpec());
            if (quick_unit.fHP == 0) { quick_unit.Dead = true; }
            else { quick_unit.Dead = false; }

            quick_unit.map = qMap;
            quick_unit.HpHistory    = new int[HISTORY_MAX_LENGTH];
            quick_unit.XposHistory  = new int[HISTORY_MAX_LENGTH];
            quick_unit.YposHistory  = new int[HISTORY_MAX_LENGTH];
            quick_unit.ActFinishedHistory = new bool[HISTORY_MAX_LENGTH]; 

            quick_unit.HpHistory[0]     = quick_unit.fHP;
            quick_unit.XposHistory[0]   = quick_unit.fXpos;
            quick_unit.YposHistory[0]   = quick_unit.fYpos;
            quick_unit.ActFinishedHistory[0] = quick_unit.fActionFinished; 

            string mark = quick_unit.getMark();
            quick_unit.IsFlyer = (mark.Equals("F") || mark.Equals("A"));
            quick_unit.IsInfantry = mark.Equals("I");
            quick_unit.IsCannon = mark.Equals("U");

            return quick_unit;
        }

        // 標準のコンストラクタ
        public  SatQuickUnit(int x, int y, int id, int team, int HP, int actionFinished, Spec spec) {
            this.fXpos = x;
            this.fYpos = y;
            this.fId = id;
            this.fHP = HP;
            this.fTeamColor = team;
            this.fSpec = spec;
            if (actionFinished == 0) {
                this.fActionFinished = false;
            } else {
                this.fActionFinished = true;
            }
        }

        // 空コンストラクタ
        public SatQuickUnit() { }



        #region ■■ゲッター■■

        public new  int getHP() {
            return this.fHP;
        }

        // HPが0なら破壊されたとみなし，trueを返す
        //※このクラスでは提供しない！　その代りDeadフラグを使う．
        public new  bool isDead() {
            showError_NotProvided("IsDead()");
            return false;
        }

        public new  int getXpos() {
            return this.fXpos;
        }

        public new  int getYpos() {
            return this.fYpos;
        }

        public new  int getTypeOfUnit() {
            return this.fSpec.getUnitType();
        }

        public new  int getID() {
            return this.fId;
        }

        public new  int getTeamColor() {
            return this.fTeamColor;
        }

        public new  string getName() {
            return this.fSpec.getSpecName();
        }

        public new  string getMark() {
            return this.fSpec.getSpecMark();
        }

        public new  Spec getSpec() {
            return this.fSpec;
        }

        public new  int[] getX_ints() {
            return this.X_ints;
        }

        public new  int getX_int(int idx) {
            return this.X_ints[idx];
        }

        public new  bool isActionFinished() {
            return this.fActionFinished;
        }
        #endregion

        #region ■■セッター■■

        //あんまり，やたらと使わない事．バグりやすくなる．
        public new void setID(int id) {
            this.fId = id;  
        }
        public new  void setHP(int hp) {
            this.fHP = hp;
        }

        public new  void setXpos(int x) {
            this.fXpos = x;
        }

        public new  void setYpos(int y) {
            this.fYpos = y;
        }

        public new  void setPos(int x, int y) {
            this.fXpos = x;
            this.fYpos = y;
        }

        public new  void setX_int(int idx, int value) {
            this.X_ints[idx] = value;
        }

        public new  void setX_ints(int[] argints) {
            this.X_ints = argints;
        }

        public new  void initX_ints(int length) {
            this.X_ints = new int[length];
        }

        public new  void setActionFinished(bool actionFinishFlag) {
            this.fActionFinished = actionFinishFlag;
        }

        public void setMapPointer(SatQuickMap qMap) {
            this.map = qMap;
        }
        #endregion

       

        // 引数に指定された値だけ，HPを上昇させる
        public new  void raiseHP(int value) {
            this.fHP += value;
        }

        // 引数に指定された値だけ，HPを減少させる．HPが0以下になる場合は0にセットする
        public new  void reduceHP(int value) {
            if (value >= this.fHP) {
                this.fHP = 0;
            } else {
                this.fHP -= value;
            }
        }

        // ユニットの情報を文字列にして返す（画面表示用に使用する）
        public new  string toString() {
            string str;
			str = "ID:" + this.fId + "  ";

			str += "HP:" + this.fHP + "  ";
			str += "Pos:" + "(" + this.fXpos + " ," + this.fYpos + ")" + "  ";
			str += "Type:" + this.fSpec.getSpecName() + "  ";
            if (this.fTeamColor == Consts.RED_TEAM) { str += "Team:" + "RED"; }
            if (this.fTeamColor == Consts.BLUE_TEAM) { str += "Team:" + "BLUE"; }
            if (fActionFinished) { str += " E"; }
			 

            return str;
        }

        // ユニットごく簡易な情報を短い文字列で （ログ用）
        public new  string toShortString() {
            return fSpec.getSpecMark() + fHP + "(" + fXpos + "," + fYpos + ")";
        }

        // クローン作成メソッド（＃このQuickUnitクラスは，クローン作成機能を提供しない）
        //MapでCreateClone用に呼ばれるか，HumanPlayerで攻撃判定用に呼ばれるかする．
        public new  Unit createDeepClone() {
            showError_NotProvided("createDeepClone");
            return null;
        }

        //このクラスが提供しない（派生元クラスが持ってた）機能を，「それエラーですよ」とコンソールに吐き出すメソッド．
        private void showError_NotProvided(string notProvidedFunction){
            SAT_FUNC.WriteLine("Error. Not Equipped<"+notProvidedFunction+"> in SatQuickUnit class.");
            int x = 0;
            x = 1/x;    //Stop running by "divided by 0 exception".
        }
    }
    
}
