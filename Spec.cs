using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimpleWars {
    /// <summary>
    /// ユニット毎の移動力や，攻撃タイプ等の情報を扱うクラス
    /// Unitが参照用にオブジェクトを持つ
    /// </summary>
    class Spec {
        public const int SPECTYPENUM = 6; // ユニットのタイプ数（スペック数）

        //ユニットが持つパラメータ群
        private int fTypeIndex;
        private int fStep;
        private int fMaxRange;
        private int fMinRange;
        private bool fDirectAttack;

        //コンストラクタ（内部でしか使わない）
        public Spec(int typeIndex, int unitStep, int minRange, int maxRange, bool canDirectAttack) {
            this.fTypeIndex = typeIndex;
            this.fStep = unitStep;
            this.fMinRange = minRange;
            this.fMaxRange = maxRange;
            this.fDirectAttack = canDirectAttack;
        }

        //それぞれのユニットタイプのステータスを設定
        //引数（タイプインデックス，移動力，最小攻撃範囲，最大攻撃範囲，直接攻撃可能フラグ）
        static Spec specF_fighter = new Spec(0, 9, 0, 1, true);
        static Spec specA_attacker = new Spec(1, 7, 0, 1, true);
        static Spec specT_panzer = new Spec(2, 6, 0, 1, true);
        static Spec specU_cannon = new Spec(3, 5, 2, 3, false);
        static Spec specR_antiAir = new Spec(4, 6, 0, 1, true);
        static Spec specI_infantry = new Spec(5, 3, 0, 1, true);

        //ステータス格納場所 下記のtypeIndexに対応
        public static Spec[] specs = new Spec[SPECTYPENUM] { specF_fighter, specA_attacker, specT_panzer, specU_cannon, specR_antiAir, specI_infantry };

        //各ユニットの名前（文字列）typeIndexに対応
        public static string[] specNames = new String[SPECTYPENUM] { "fighter", "attacker", "panzer", "cannon", "antiair", "infantry" };

        //各ユニットの名前短い版
        public static string[] specMarks = new String[SPECTYPENUM] { "F", "A", "P", "U", "R", "I" };

        //航空ユニットか（地形効果が影響がないか）
        public static readonly bool[] sAirUnit = new bool[] { true, true, false, false, false, false };

        //ユニット毎のダメージ相性 高いほどダメージが通りやすい F A P U R Iの順
        public static readonly int[,] atkPowerArray = new int[,] {	
             // against   F   A   P   U   R   I
					    {55, 65,  0,  0,  0,  0}, // of F
						{ 0,  0,105,105, 85,115}, // of A
						{ 0,  0, 55, 70, 75, 75}, // of P
						{ 0,  0, 60, 75, 65, 90}, // of U
						{70, 70, 15, 50, 45,105}, // of R
						{ 0,  0,  5, 10,  3, 55}, // of I	 
        };

        // ユニット毎の移動コスト 低いほど移動できる  NOENTRY, 平原，海，森，山，道路の順
        public static readonly int[,] moveCost = new int[,] {
              // to       禁 平 海 森  山 道 城
                        { 99, 1,  1, 1,  1, 1, 1}, //F
						{ 99, 1,  1, 1,  1, 1, 1}, //A
                        { 99, 1, 99, 2, 99, 1, 1}, //P
						{ 99, 1, 99, 2, 99, 1, 1}, //U
                        { 99, 1, 99, 2, 99, 1, 1}, //R
		                { 99, 1, 99, 1,  2, 1, 1}, //I
        };

        #region ■■ゲッター■■

        // 攻撃パワーを取得する
        public int getUnitAtkPower(int targetUnitType) {
            return atkPowerArray[fTypeIndex, targetUnitType];
        }

        // フィールドごとの移動コストを取得する
        public int getMoveCost(int mapFieldType) {
            return moveCost[fTypeIndex, mapFieldType];
        }

        // ユニットの移動力を取得
        public int getUnitStep() {
            return fStep;
        }

        // ユニットのタイプインデックスを返す
        public int getUnitType() {
            return fTypeIndex;
        }

        // ユニットの攻撃範囲の最小値
        public int getUnitMinAttackRange() {
            return fMinRange;
        }

        // ユニットの攻撃範囲の最大値
        public int getUnitMaxAttackRange() {
            return fMaxRange;
        }

        // ユニットの攻撃タイプをリターン，近接ならTrue,遠距離ならFalse
        public bool isDirectAttackType() {
            return fDirectAttack;
        }

        // 地形効果が得られないタイプか
        public bool isAirUnit() {
            return sAirUnit[fTypeIndex];
        }

        // ユニットのタイプインデックスから名前(String)を返す関数
        // 主に表示用に使用する
        public string getSpecName() {
            return specNames[fTypeIndex];
        }

        // ユニットのタイプインデックスから名前(String)を返す関数
        // 主に表示用に使用する
        public string getSpecMark() {
            return specMarks[fTypeIndex];
        }
        #endregion

        // 名前（文字列）から，そのユニットのスペックを返す  
        public static Spec getSpec(String unitName) {

            for (int i = 0; i < specNames.Length; i++) {
                if (specNames[i].Equals(unitName)) {
                    return specs[i];// 名前（文字列）が一致したユニットのスペック
                }
            }

            Logger.showDialogMessage("SPEC: getSpec: ユニットの名前が不正確です");
            return null;
        }
    }
}
