using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimpleWars {
    /// <summary>
    ///  1ユニットを表すクラス
    ///  自分のスペックもオブジェクトとして，保持している
    ///  HPやマップ上の位置情報などをゲッターから取得できる
    /// </summary>
    class Unit {
        private int fXpos;               // マップ上のポジション
        private int fYpos;               // マップ上のポジション
        private int fHP;                 // ヒットポイント（0以下になると，ユニットはマップ上から消滅する）
        private int fTeamColor;         // ユニットの所属するチームカラー
        private int fId;                 // ユニットを判別するために現在使用中
        private bool fActionFinished;    // 行動終了フラグ
        private Spec fSpec;              // スペック(移動力・攻撃力など固定の値）

        private int[] X_ints;            // 思考ルーチン等が使うための自由なint配列

        // 標準のコンストラクタ
        public Unit(int x, int y, int id, int team, int HP, int actionFinished, Spec spec) {
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
        public Unit() { }

        #region ■■ゲッター■■

        public int getHP() {
            return this.fHP;
        }

        // HPが0なら破壊されたとみなし，trueを返す
        public bool isDead() {
            if (this.fHP == 0) {
                return true;
            } else {
                return false;
            }
        }

        public int getXpos() {
            return this.fXpos;
        }

        public int getYpos() {
            return this.fYpos;
        }

        public int getTypeOfUnit() {
            return this.fSpec.getUnitType();
        }

        public int getID() {
            return this.fId;
        }

        public int getTeamColor() {
            return this.fTeamColor;
        }

        public string getName() {
            return this.fSpec.getSpecName();
        }

        public string getMark() {
            return this.fSpec.getSpecMark();
        }

        public Spec getSpec() {
            return this.fSpec;
        }

        public int[] getX_ints() {
            return this.X_ints;
        }

        public int getX_int(int idx) {
            return this.X_ints[idx];
        }

        public bool isActionFinished() {
            return this.fActionFinished;
        }
        #endregion

        #region ■■セッター■■

        public void setHP(int hp) {
            this.fHP = hp;
        }

        public void setXpos(int x) {
            this.fXpos = x;
        }

        public void setYpos(int y) {
            this.fYpos = y;
        }

        public void setPos(int x, int y) {
            this.fXpos = x;
            this.fYpos = y;
        }

        public void setX_int(int idx, int value) {
            this.X_ints[idx] = value;
        }

        public void setX_ints(int[] argints) {
            this.X_ints = argints;
        }

        public void initX_ints(int length) {
            this.X_ints = new int[length];
        }

        public void setActionFinished(bool actionFinishFlag) {
            this.fActionFinished = actionFinishFlag;
        }
        #endregion

        // 引数に指定された値だけ，HPを上昇させる
        public void raiseHP(int value) {
            this.fHP += value;
        }

        // 引数に指定された値だけ，HPを減少させる．HPが0以下になる場合は0にセットする
        public void reduceHP(int value) {
            if (value > this.fHP) {
                this.fHP = 0;
            } else {
                this.fHP -= value;
            }
        }

        // ユニットの情報を文字列にして返す（画面表示用に使用する）
        public string toString() {
            string str;
			str = "ID:" + this.fId + "  ";

			str += "HP:" + this.fHP + "  ";
			str += "Pos:" + "(" + this.fXpos + " ," + this.fYpos + ")" + "  ";
			str += "Type:" + this.fSpec.getSpecName() + "  ";
            if (this.fTeamColor == Consts.RED_TEAM) { str += "Team:" + "RED"; }
            if (this.fTeamColor == Consts.BLUE_TEAM) { str += "Team:" + "BLUE"; }
			 

            return str;
        }

        // ユニットごく簡易な情報を短い文字列で （ログ用）
        public string toShortString() {
            return fSpec.getSpecMark() + fHP + "(" + fXpos + "," + fYpos + ")";
        }

        // クローン作成メソッド（＃specはdeepCloneする必要はない）
        public Unit createDeepClone() {
            Unit copied = new Unit();
            copied.fXpos = this.fXpos;
            copied.fYpos = this.fYpos;
            copied.fId = this.fId;
            copied.fTeamColor = this.fTeamColor;
            copied.fActionFinished = this.fActionFinished;
            copied.fHP = this.fHP;
            copied.fSpec = this.fSpec;
            if (this.X_ints != null) {
                copied.X_ints = new int[this.X_ints.Length];
                for (int i = 0; i < this.X_ints.Length; i++) copied.X_ints[i] = this.X_ints[i];
            }

            return copied;
        }
    }
}
