using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars
{
    /// <summary>
    /// １ユニットの行動を保存するための重要なクラス．
    /// メンバ変数は全てpublic で，ゲッタセッタはありません．
    /// コンストラクタではなく，Action.createXXX というstatic関数で生成することを勧めます．
    /// </summary>
    class Action {
        // 行動のタイプ 
        public const int ACTIONTYPE_SURRENDER = 0;     // 降伏
        public const int ACTIONTYPE_MOVEONLY = 1;      // 移動するだけ
        public const int ACTIONTYPE_MOVEANDATTACK = 2; // 攻撃を伴う行動
        public const int ACTIONTYPE_TURNEND = 3;       // ターン終了する

        // 以下５つは基本ユーザが指定するもの
        public int actionType;      // 行動タイプ
        public int destinationXpos; // 移動先
        public int destinationYpos;
        public int operationUnitId; // 行動ユニット
        public int targetUnitId;    // 攻撃対象ユニット

        // 以下５つは自動で入るもの
        public int teamColor;  // 行動ユニットの陣営
        public int attackXpos; // 攻撃対象ユニットの位置
        public int attackYpos;
        public int fromXpos;   // 行動ユニットの移動前位置
        public int fromYpos;

        // 拡張変数，ご自由にお使いください
        public int X_attackDamage = 0;  // 与えたダメージ
        public int X_counterDamage = 0; // 反撃ダメージ
        public double X_actionEvaluationValue = 0; // 行動評価値


        // 標準の引数を全て受け取るコンストラクタ （非標準）
        public Action(int actionType, int teamColor, int operationUnitId, int targetUnitId, int fromXpos, int fromYpos,
            int destXpos, int destYpos, int attackXpos, int attackYpos) {
            this.actionType = actionType;
            this.teamColor = teamColor;
            this.operationUnitId = operationUnitId;
            this.targetUnitId = targetUnitId;
            this.destinationXpos = destXpos;
            this.destinationYpos = destYpos;
            this.fromXpos = fromXpos;
            this.fromYpos = fromYpos;
            this.attackXpos = attackXpos;
            this.attackYpos = attackYpos;
        }

        // 空コンストラクタ
        public Action() {
            actionType = -1;
            destinationXpos = -1;
            destinationYpos = -1;
            operationUnitId = -1;
            targetUnitId = -1;
        }


        /****************************************/
        /***アクションを生成するための関数*******/
        /****************************************/

        // 移動のみのアクションを生成して返す static 関数
        public static Action createMoveOnlyAction(Unit opUnit, int destXpos, int destYpos) {
            Action a = new Action();
            a.operationUnitId = opUnit.getID();
            a.teamColor = opUnit.getTeamColor();
            a.destinationXpos = destXpos;
            a.destinationYpos = destYpos;
            a.fromXpos = opUnit.getXpos();
            a.fromYpos = opUnit.getYpos();
            a.actionType = ACTIONTYPE_MOVEONLY;
            return a;
        }

        // 攻撃をともなうアクションを生成して返す static 関数
        public static Action createAttackAction(Unit opUnit, int destXpos, int destYpos, Unit targetUnit) {
            Action a = new Action();
            a.operationUnitId = opUnit.getID();
            a.teamColor = opUnit.getTeamColor();
            a.targetUnitId = targetUnit.getID();
            a.destinationXpos = destXpos;
            a.destinationYpos = destYpos;
            a.fromXpos = opUnit.getXpos();
            a.fromYpos = opUnit.getYpos();
            a.actionType = ACTIONTYPE_MOVEANDATTACK;
            a.attackXpos = targetUnit.getXpos();
            a.attackYpos = targetUnit.getYpos();
            return a;
        }

        // アクションタイプTURNEND（オプション）
        // 空のActionオブジェクトを生成して直接返すと，ターンエンドとみなす
        public static Action createTurnEndAction() {
            Action a = new Action();
            a.actionType = ACTIONTYPE_TURNEND;
            return a;
        }

        /// <summary>
        /// 投了を生成
        /// </summary>
        /// <returns>投了を通知するアクション</returns>
        public static Action Resignation() {
            Action a = new Action();
            a.actionType = ACTIONTYPE_SURRENDER;
            return a;
        }



        /****************************************/
        /********棋譜表示，読み込み関連**********/
        /****************************************/

        // 少し読みやすい１行の文字列．ログ用に使う
        public string toOneLineString() {
            string str = "ID:" + operationUnitId + " (" + fromXpos + "," + fromYpos + ")" + 
                " -> (" + destinationXpos + "," + this.destinationYpos + ")" + 
                " target=" + this.targetUnitId + " (" + this.attackXpos + "," + this.attackYpos + ")";
            return str;
        }

        // SGFManagerが行動履歴を保存する際に使用する
        public string toSGFString() {
            // string str;
            string str = this.operationUnitId + "," 
                + this.destinationXpos + "," 
                + this.destinationYpos + ","
                + this.fromXpos + "," 
                + this.fromYpos + "," 
                + this.targetUnitId + "," 
                + this.attackXpos + ","
                + this.attackYpos + "," 
                + this.actionType + "," 
                + this.X_attackDamage + "," 
                + this.X_counterDamage;
            return str;
        }

        public string ToString() {
            return this.fromXpos + ":" + this.fromYpos + ":" + this.destinationXpos + ":" + this.destinationYpos + ":" + this.attackXpos + ":" + this.attackYpos;
        }

        /// <summary>
        /// アクションの内訳をstringで記述
        /// </summary>
        /// <returns></returns>
        public string toString() {
            string str = "from->" + "(" + this.fromXpos + "," + this.fromYpos + ") " +
                         "to->" + "(" + this.destinationXpos + "," + this.destinationYpos + ") " +
                         "attack->" + "(" + this.attackXpos + "," + this.attackYpos + ") " +"\r\n";
            return str;
        }

        // 棋譜から1ユニットの行動履歴を文字列で受けとりActionを生成して返す
        public static Action Parse(string one) {
            string[] ones = one.Split(',');
            Action a = new Action();
            a.operationUnitId = Int32.Parse(ones[0]);
            a.destinationXpos = Int32.Parse(ones[1]);
            a.destinationYpos = Int32.Parse(ones[2]);
            a.fromXpos = Int32.Parse(ones[3]);
            a.fromYpos = Int32.Parse(ones[4]);
            a.targetUnitId = Int32.Parse(ones[5]);
            a.attackXpos = Int32.Parse(ones[6]);
            a.attackYpos = Int32.Parse(ones[7]);
            a.actionType = Int32.Parse(ones[8]);
            a.X_attackDamage = Int32.Parse(ones[9]);
            a.X_counterDamage = Int32.Parse(ones[10]);
            return a;
        }

        // ディープクローン生成(厳密にはDeepではない）
        public Action createDeepClone() {
            Action copied = new Action(this.actionType, this.teamColor, this.operationUnitId, this.targetUnitId,
                this.fromXpos, this.fromYpos, this.destinationXpos, this.destinationYpos, this.attackXpos, this.attackYpos);
            copied.X_actionEvaluationValue = this.X_actionEvaluationValue;
            copied.X_attackDamage = this.X_attackDamage;
            copied.X_counterDamage = this.X_counterDamage;
            return copied;
        }
    }
}
