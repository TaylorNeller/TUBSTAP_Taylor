using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {
    // 思考ルーチンのサンプルです．
    // 攻撃可能な全ての自分・敵・攻撃位置の組み合わせに対し，それを評価して，最良のものを選びます．
    class AI_Sample_MaxActEval : Player {

        // 敵ユニットの脅威度を Unit オブジェクトの任意利用int[]に持たせる際のインデックス
        private const int XINDEX_MAXTHREAT = 0;

        // 乱数生成器
        private Random fRand = new Random();
        
        //評価値を画面上に表示するstring
        string[,] evaluateMap;

        // パラメータ
        private static bool DETAILED_LOG = false;                  // 詳細なログを表示するか
        private const bool CALC_THREAT_ONLYTO_MOVEDUNITS = true;  // 敵ユニットの脅威度を計算するのに，味方の行動済みユニットのみを対象にする
        private const int MINIMUM_EFFICIENCY_FOR_ATTACK = -20;    // 攻撃の評価値がこれ以下なら攻撃しない（反撃をくらうなどで負になりうる）
        private const int BASE_VALUE_OF_MYUNIT = 10;              // これ + HP が自ユニットの価値
        private const int BASE_VALUE_OF_ENEMY = 10;               // これ + 脅威度 が敵ユニットの価値


        // AIの表示名（必須のpublic関数）
        public string getName() {
            return "Sample_MaxActionEvalFunc";
        }

        // パラメータ等の情報を返す関数
        public string showParameters() {
            return "DETAILED_LOG = " + DETAILED_LOG.ToString() + "\r\n" +
                   "CALC_THREAT_ONLYTO_MOVEDUNITS = " + CALC_THREAT_ONLYTO_MOVEDUNITS.ToString() + "\r\n" +
                   "MINIMUM_EFFICIENCY_FOR_ATTACK = " + MINIMUM_EFFICIENCY_FOR_ATTACK + "\r\n" +
                   "BASE_VALUE myunits/enemy = " + BASE_VALUE_OF_MYUNIT + " / " + BASE_VALUE_OF_ENEMY;
        }

        // 1ユニットの行動を決定する（必須のpublic関数）
        // v1.06からは引数が4つに増えています
		public Action makeAction(Map map, int teamColor, bool turnStart, bool gameStart) {

            //マップ上に行動後の評価関数を表示するための変数　
            //マップサイズがわからないと生成できないためここで生成
            if (turnStart) {
                evaluateMap = new string[map.getXsize(), map.getYsize()];
                Logger.addLogMessage(map.toString(), teamColor);
            }

            List<Unit> enemies = map.getUnitsList(teamColor, false, false, true);  // 全ての敵リスト

            // 手続き１ ----------- 敵ユニットそれぞれから自分ユニット（行動済みのみ）への最大攻撃力を計算して格納する
            {
                Logger.addLogMessage("\r\nThinking start:\r\n Step 1: Calculating threats of enemies\r\n",teamColor);
                foreach (Unit enemyU in enemies) {
                    int damage = this.estimateMaxAtkDamage(enemyU, map);
                    Logger.addLogMessage("   max damage from " + enemyU.toShortString() + " = " + damage + "\r\n",teamColor);

                    // Unitオブジェクトの任意利用intを初期化して脅威度を格納する
                    enemyU.initX_ints(1);
                    enemyU.setX_int(XINDEX_MAXTHREAT, damage);
                }
            }


            // 手続き２ ----------- 攻撃可能な自ユニットがあれば，評価値最大の手を選ぶ
            {
                Logger.addLogMessage(" Step 2: Attack\r\n",teamColor);
                List<Action> atkActions = AiTools.getAllAttackActions(teamColor, map);  // 全ての攻撃行動リスト

                int maxAtkActValue = MINIMUM_EFFICIENCY_FOR_ATTACK;  // これ以下なら攻撃しない
                Action maxAction = null;

                foreach (Action act in atkActions) {
                    // 攻撃行動評価値を計算する
                    int actValue = evaluateAttackActionValue(act, map,teamColor);

                    if (actValue > maxAtkActValue) {
                        maxAtkActValue = actValue;//最大の評価値に置き換える
                        maxAction = act;
                    }
                }

                if (maxAction != null) {
					Logger.addLogMessage(" Selected = " + maxAction.toOneLineString() + " Value=" + maxAtkActValue + "\r\n", teamColor);

                    //評価値マップの更新処理
                    //なお，攻撃行動にのみ評価値が存在するため　移動行動による評価は表示しない
                    //攻撃を行ったユニット上に　攻撃時の評価値を表示する
                    evaluateMap[maxAction.destinationXpos, maxAction.destinationYpos] = maxAtkActValue.ToString();
                    //表示処理
                    DrawManager.drawStringOnMap(evaluateMap);

                    
                    return maxAction;
                }
            }


            // 手続き３ ----------- 良い攻撃行動がない場合，適当にユニットを選び，移動させる
            // 敵に向かうか，有利な地形に留まるだけ．味方を守るような移動は考慮していない
            {
                Logger.addLogMessage("   No effective attack.\r\n Step 3: Move.\r\n",teamColor);

                List<Unit> myUnits = map.getUnitsList(teamColor, false, true, false);  // 未行動自ユニット
                Unit opUnit = myUnits[fRand.Next(myUnits.Count)]; // ランダムに１つを選択

                Logger.addLogMessage("   Unit " + opUnit.toShortString() + " is randomly selected to move.\r\n",teamColor);

                // その移動できる範囲
                bool[,] movable = RangeController.getReachableCellsMatrix(opUnit, map);

                // それらについて，地形，相性のよいユニットの近さなどを考慮して最良のものを選ぶ
                int maxScore = Int32.MinValue;
                int maxX = -1;
                int maxY = -1;

                string matrixLog = "  ";

                for (int x = 1; x < map.getXsize() - 1; x++) {
                    for (int y = 1; y < map.getYsize() - 1; y++) {
                        if (movable[x, y] == false) {
                            matrixLog += " ---";
                            continue;  // 移動できない場所は飛ばす
                        }

                        int defense = map.getFieldDefensiveEffect(x, y);  // 防御力
                        if (opUnit.getSpec().isAirUnit() == true) defense = 0;

                        int localMaxScore = Int32.MinValue; // そのマスにおけるベスト（ログ用）
                        foreach (Unit enemyU in enemies) {
                            int dist = Math.Abs(x - enemyU.getXpos()) + Math.Abs(y - enemyU.getYpos());  // マンハッタン距離
                            int effect = opUnit.getSpec().getUnitAtkPower(enemyU.getSpec().getUnitType());  // opUnit から enemyU への攻撃力

                            // 良い相手がいれば近づく．いなければ防御を優先するような関数
                            int score = defense * 5 + effect / (dist + 5);

                            if (DETAILED_LOG == true) Logger.addLogMessage("  -> (" + x + "," + y + ") vs " + enemyU.toShortString() + " score=" + score + "\r\n",teamColor);

                            if (score > maxScore) {
                                maxScore = score;
                                maxX = x;
                                maxY = y;
                            }
                            if (score > localMaxScore) localMaxScore = score;
                        }
                        matrixLog += localMaxScore.ToString().PadLeft(4);
                    }
                    matrixLog += "\r\n  ";
                }
                Logger.addLogMessage(matrixLog,teamColor);
                Logger.addLogMessage("   Selected = " + opUnit.toShortString() + " -> (" + maxX + "," + maxY + ")\r\n",teamColor);
                return Action.createMoveOnlyAction(opUnit, maxX, maxY); // 移動のみする行動を作成して返す
            }
        }


        // 行動評価関数．攻撃行動を受け取り、その好ましさを返す
        // 与えるダメージ×敵の価値 － 受ける反撃×自分の価値 を好ましさとする
        public static int evaluateAttackActionValue(Action act, Map map,int teamColor) {
            Unit myUnit = map.getUnit(act.operationUnitId); // 攻撃側ユニット
            Unit enUnit = map.getUnit(act.targetUnitId);       // 防御側ユニット
            if (myUnit == null || enUnit == null) System.Windows.Forms.MessageBox.Show("ActionEvaluator_Sample: evaluateAttackActionValue: ERROR_unit_is_null");

            int[] attackDamages = DamageCalculator.calculateDamages(map, act); // 攻撃，反撃ダメージを計算

            // ダメージ0なら攻撃しない
            if (attackDamages[0] == 0) {
                return Int32.MinValue;
            }
            // 自ユニット価値＝ 10 + ユニットの残りＨＰ
            int myValue = BASE_VALUE_OF_MYUNIT + myUnit.getHP();

            // 敵ユニット価値＝ 10 + 脅威度
            int enValue = BASE_VALUE_OF_ENEMY + enUnit.getX_int(XINDEX_MAXTHREAT);

            // 行動の好ましさ（負になりうる）
            int actValue = (attackDamages[0] * enValue) - (attackDamages[1] * myValue);

            if (DETAILED_LOG == true) {
                Logger.addLogMessage("   total=" + actValue + ", dm=" + attackDamages[0] + " enval=" + enValue + " rvdm=" + attackDamages[1] + " myval=" + myValue + " act=" + act.toOneLineString() + "\r\n",teamColor);
            } else {
                Logger.addLogMessage("   score=" + actValue + " " + myUnit.toShortString() + "  -> " + enUnit.toShortString() + "\r\n",teamColor);
            }

            return actValue;
        }


        // atkUnitの脅威度を計算． 行動済みユニットに与え得る最大ダメージ
        // 未行動のものを含めたり，ユニットの価値（歩兵は低いなど）を入れてもいい
        private int estimateMaxAtkDamage(Unit atkUnit, Map map) {
            int maxDm = 0;
            List<Action> attackActions = RangeController.getAttackActionList(atkUnit, map); // atkUnitの全攻撃行動

            foreach (Action act in attackActions) {
                Unit myUnit = map.getUnit(act.targetUnitId);
                if (CALC_THREAT_ONLYTO_MOVEDUNITS == true && myUnit.isActionFinished() == false) continue; // 未行動ユニットは対象にしない
                int[] dm = DamageCalculator.calculateDamages(atkUnit, myUnit, map); // ダメージ計算（反撃は無視）
                if (dm[0] > maxDm) maxDm = dm[0];
            }
            return maxDm;
        }

    }
}
