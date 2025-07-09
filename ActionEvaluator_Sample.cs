using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {
    //行動を評価するクラス．現在のところ攻撃行動のみを，戦果から評価する関数が実装されている．
    class ActionEvaluator_Sample {
        /*
        // 行動評価関数（今は使っていないが後ほど書き換えて使う予定）
        public static double evaluateActionValue(Action act, Map map) {
            double actValue = 0;
            Unit[] units = map.getUnits();
            Unit operationUnit = units[act.operationUnitId];
            Unit targetUnit = units[act.targetUnitId];

            // 移動せずに攻撃した場合
            if (act.actionType == Action.ACTIONTYPE_ATTACKONLY) {


            }
            // 行動に移動を伴う場合
            if (act.actionType == Action.ACTIONTYPE_MOVEATTACK || act.actionType == Action.ACTIONTYPE_ATTACKONLY) {
                // double atkActValue = evaluateAttackActionValue(act, map);
                //double moveActValue = evaluateMoveActionValue(act, map);
                //actValue = atkActValue - moveActValue; // 攻撃行動評価値＋移動先位置の危険度

            } //行動に移動を伴わない場合
            else if (act.actionType == Action.ACTIONTYPE_NOMOVE || act.actionType == Action.ACTIONTYPE_MOVEONLY) {

            }

            return actValue;
        }
         * */

        // 行動評価関数
        // 攻撃行動を受け取り、その好ましさを返す
        public static int evaluateAttackActionValue(Action act, Map map) {
            Unit operationUnit = map.getUnit(act.operationUnitId); // 攻撃側ユニット
            Unit targetUnit = map.getUnit(act.targetUnitId);       // 防御側ユニット
            if (operationUnit == null || targetUnit == null) System.Windows.Forms.MessageBox.Show("ActionEvaluator_Sample: evaluateAttackActionValue: ERROR_unit_is_null");

            int[] attackDamages = DamageCalculator.calculateDamages(map, act); // 攻撃，反撃ダメージを計算

            // 自ユニット価値＝ 10 + ユニットの残りＨＰ
            int operationUnitValue = 10 + operationUnit.getHP();
            // 敵ユニット価値＝10 ＋　予想される最大ダメージ
            int targetUnitValue = 10 + targetUnit.getX_int(AI_Sample_MaxActEval.XINDEX_MAXTHREAT);

            // 与えるダメージ×敵の価値　－　受ける反撃×自分の価値　を計算　（負になりうる注意）
            int actValue = (attackDamages[0] * targetUnitValue) - (attackDamages[1] * operationUnitValue);

            Logger.addLogMessage("total="+actValue+ ", dam="+attackDamages[0] + " enval="+ targetUnitValue + " rvdam=" + attackDamages[1] + " myval=" + operationUnitValue  + " act="+act.toOneLineString()+ "\r\n");

            return actValue;
        }


        // atkUnit が行動済みユニットに与え得る最大ダメージを計算
        public static int estimateMaxAtkDamage(Unit atkUnit, Map map) {
            int maxDm = 0;
            List<Action> attackActions = RangeController.getAttackActionList(atkUnit, map); // atkUnitの全攻撃行動
            foreach (Action act in attackActions) {
                Unit myUnit = map.getUnit(act.targetUnitId);
                if (isEffective(atkUnit, myUnit) && (myUnit.isActionFinished() == true)) {  // 行動済みのみを対象にする
                    int[] dm = DamageCalculator.calculateDamages(atkUnit, myUnit, map); // ダメージ計算（反撃は無視）
                    if (dm[0] > maxDm) maxDm = dm[0];
                }
            }
            return maxDm;
        }

        // operationUnitがtargetUnitに攻撃可能かどうか．ダメージ効果0の場合にはfalseが返る
        private static bool isEffective(Unit operationUnit, Unit targetUnit) {
            if (operationUnit.getSpec().getUnitAtkPower(targetUnit.getTypeOfUnit()) != 0) { return true; }
            return false;
        }
    }
}
