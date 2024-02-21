using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars
{
    /// <summary>
    /// ※高速改造版※
    /// 
    /// １ユニットの行動を保存するための重要なクラス．
    /// メンバ変数は全てpublic で，ゲッタセッタはありません．
    /// コンストラクタではなく，Action.createXXX というstatic関数で生成することを勧めます．
    /// </summary>
    class SatQuickAction : Action {

        //！注意！　各メンバ変数に16以上の数を割り当てると盛大にバグる！


        // int化する事によって高速化している．見づらいが以下のようにByteに情報が折りたたまれている．

        // int(32bit = 8hex)     [hex] [hex] - [hex] [hex] - [hex] [hex] - [hex] [hex]
        //                       Color&AtkType- fromY fromX - destY destX - tarID opID
        //
        //※例1　青(1)　MOVE-AND-ATTACK(02)  from{ 5, 6 } => dest{ 3, 3 }. targetUnitID(9) operationUnitID(8)
        //        このような32bitのIntに変換  [00]-[06 (=0110Bin)]-[05]-[06]-[03]-[03]-[09]-[08]
        //         冒頭の[00]は常に空．次の[06]は，4(マジックナンバー) * 1(BLUE-TEAM) + 2(MOVE-AND-ATTACK)．



        private static readonly int[] pow16 = 
            new int[] { 1, 16, 256, 4096, 65536, 1048576, 16777216, 268435456 };
        public const int IND_FOR_OP_ID  = 0  ,    IND_FOR_TAR_ID = 1;
        public const int IND_FOR_DEST_X = 2  ,    IND_FOR_DEST_Y = 3;
        public const int IND_FOR_FROM_X = 4  ,    IND_FOR_FROM_Y = 5;
        public const int IND_FOR_ATK_TYPE = 6;
        public const int DIVIDER_FOR_TEAMCOLOR = 16777216 * 4;

        public static int getActType(int act) {
            return act / pow16[IND_FOR_ATK_TYPE] % 4;
        }
        public static int getTeamColor(int act) {
            return act / DIVIDER_FOR_TEAMCOLOR;
        }

        public static int getOpID(int act) {
            return (act % 16);
        }
        public static int getTarID(int act) {
            return ((act / pow16[IND_FOR_TAR_ID]) % 16 );
        }
        public static int getDestX(int act) {
            return ((act / pow16[IND_FOR_DEST_X]) % 16);
        }
        public static int getDestY(int act) {
            return ((act / pow16[IND_FOR_DEST_Y]) % 16);
        }
        public static int getFromX(int act) {
            return ((act / pow16[IND_FOR_FROM_X]) % 16);
        }
        public static int getFromY(int act) {
            return ((act / pow16[IND_FOR_FROM_Y]) % 16);
        }




        // 行動のタイプ 
        public const int ACTIONTYPE_SURRENDER = 0;     // 降伏
        public const int ACTIONTYPE_MOVEONLY = 1;      // 移動するだけ
        public const int ACTIONTYPE_MOVEANDATTACK = 2; // 攻撃を伴う行動
        public const int ACTIONTYPE_TURNEND = 3;       // ターン終了する

        // 以下５つは基本ユーザが指定するもの
        public new int actionType;      // 行動タイプ
        public new int destinationXpos; // 移動先
        public new int destinationYpos;
        public new int operationUnitId; // 行動ユニット
        public new int targetUnitId;    // 攻撃対象ユニット

        // 以下５つは自動で入るもの
        public new int teamColor;  // 行動ユニットの陣営
        public new int attackXpos; // 攻撃対象ユニットの位置
        public new int attackYpos;
        public new int fromXpos;   // 行動ユニットの移動前位置
        public new int fromYpos;

        // 拡張変数，ご自由にお使いください => この拡張版（？）クラスでは使えません．
        public new int X_attackDamage = 0;  // 与えたダメージ
        public new int X_counterDamage = 0; // 反撃ダメージ
        public new double X_actionEvaluationValue = 0; // 行動評価値


       

        // 空コンストラクタ
        public SatQuickAction() {            
        }


        /****************************************/
        /***アクションを生成するための関数*******/
        /****************************************/

        // 移動のみのアクションを生成して返す static 関数
        public static int createMoveOnlyAction(SatQuickUnit opUnit, int destXpos, int destYpos) {
            int act = 0;
            act += ACTIONTYPE_MOVEONLY * pow16[IND_FOR_ATK_TYPE];
            act += opUnit.getTeamColor() * (DIVIDER_FOR_TEAMCOLOR);
            act += opUnit.getID();
            //act += pow16[IND_FOR_TAR_ID] * 15;
            act += destXpos * pow16[IND_FOR_DEST_X];
            act += destYpos * pow16[IND_FOR_DEST_Y];
            act += opUnit.getXpos() * pow16[IND_FOR_FROM_X];
            act += opUnit.getYpos() * pow16[IND_FOR_FROM_Y];
            return act;
        }

        // 攻撃をともなうアクションを生成して返す static 関数
        public static int createAttackAction(
                                        SatQuickUnit opUnit, int destXpos, int destYpos, SatQuickUnit targetUnit) {
            int act = 0;
            act += pow16[IND_FOR_ATK_TYPE] * ACTIONTYPE_MOVEANDATTACK;
            act += opUnit.getTeamColor() * DIVIDER_FOR_TEAMCOLOR;
            act += opUnit.getID();
            act += pow16[IND_FOR_TAR_ID] * targetUnit.getID();
            act += pow16[IND_FOR_DEST_X] * destXpos;
            act += pow16[IND_FOR_DEST_Y] * destYpos;
            act += pow16[IND_FOR_FROM_X] * opUnit.getXpos();
            act += pow16[IND_FOR_FROM_Y] * opUnit.getYpos();
            return act;
        }

        // アクションタイプTURNEND（オプション）
        // 空のActionオブジェクトを生成して直接返すと，ターンエンドとみなす
        public static int createTurnEndAction() {
            int act = pow16[IND_FOR_ATK_TYPE] * ACTIONTYPE_TURNEND;
            return act;
        }

        /// <summary>
        /// 投了を生成
        /// </summary>
        /// <returns>投了を通知するアクション</returns>
        public static int Resignation() {
            int act = pow16[IND_FOR_ATK_TYPE] * ACTIONTYPE_SURRENDER;
            return act;
        }


        //攻撃のIDを元のMAP上で正しいものにして返す
        public static int fixIDs(int act, Map originalMap, SatQuickMap qMap) {
            int acttype = getActType(act);
            if (acttype == ACTIONTYPE_SURRENDER || acttype == ACTIONTYPE_TURNEND) { return act; }
            
            //移動元UnitのIDを，高速化前のオリジナルマップにおけるIDに修正
            int trueOpID = originalMap.getMapUnit()[getFromX(act), getFromY(act)].getID();
            act = fixOpID(act, trueOpID);
            if (acttype == ACTIONTYPE_MOVEONLY) { return act; }

            //攻撃先UnitのIDをオリジナルマップでのIDに修正
            int tarUniX = qMap.getUnit(getTarID(act)).getXpos();
            int tarUniY = qMap.getUnit(getTarID(act)).getYpos();
            int trueTarID = originalMap.getMapUnit()[tarUniX, tarUniY].getID();
            act = fixTarID(act, trueTarID);
            return act;
        }

        private static int fixTarID(int act, int newTarID) {
            act -= pow16[IND_FOR_TAR_ID] * getTarID(act);
            act += pow16[IND_FOR_TAR_ID] * newTarID;
            return act;
        }
        private static int fixOpID(int act, int newOpID) {
            act -= pow16[IND_FOR_OP_ID] * getOpID(act);
            act += pow16[IND_FOR_OP_ID] * newOpID;
            return act;
        }


        /****************************************/
        /********棋譜表示，読み込み関連**********/
        /****************************************/

        // 少し読みやすい１行の文字列．ログ用に使う
        public static string toOneLineString(int act) {
            string str = "ID:" + getOpID(act) + " (" + getFromX(act) + "," + getFromY(act) + ")" + 
                " -> (" + getDestX(act) + "," + getDestY(act) + ")" + 
                " target=" + getTarID(act) + " (" + "?" + "," + "?" + ")";
            if (getActType(act) == ACTIONTYPE_MOVEONLY) { str += "mv"; }
            return str;
        }

        // SGFManagerが行動履歴を保存する際に使用する
        public string toSGFString() {
            // string str;
            return "ERROR: this class doesn't provide SFGString() method.";
        }

        /// <summary>
        /// アクションの内訳をstringで記述
        /// </summary>
        /// <returns></returns>
        public static string toString(int act) {
            string str = "from->" + "(" + getFromX(act) + "," + getFromY(act) + ") " +
                         "to->" + "(" + getDestX(act) + "," + getDestY(act) + ") " +
                         "attack-> [" + getTarID(act) + "](" + "?" + "," + "?" + ") " +"\r\n";
            return str;
        }

        // 棋譜から1ユニットの行動履歴を文字列で受けとりActionを生成して返す
        public static SatQuickAction Parse(string one) {
            return null;    //Not provide.
        }



        public static Action toAction(int act) {
            return toAction(act, 0, 0);
        }
        public static Action toAction(int act, int atkXpos, int atkYpos){
            Action copied = new Action(
                //this.actionType, this.teamColor, 
                getActType(act), getTeamColor(act),
                //this.operationUnitId, this.targetUnitId, 
                getOpID(act), getTarID(act),              
                //this.fromXpos, this.fromYpos,
                getFromX(act), getFromY(act),
                //this.destinationXpos, this.destinationYpos, 
                getDestX(act), getDestY(act),
                //this.attackXpos, this.attackYpos
                atkXpos, atkYpos                
                );
            return copied;
        }
    }
}
