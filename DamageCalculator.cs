using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {
    /// <summary>
    /// ユニット同士のダメージを計算する関数をもつクラス
    /// 攻撃ダメージと，反撃ダメージを配列で返す
    /// 引数のバリエーションが豊富で，いろんな引数の組合わせから
    /// ダメージ計算が可能
    /// </summary>
    class DamageCalculator {

        // Mapとアクションから攻撃ダメージを計算する
        public static int[] calculateDamages(Map map, Action action) {
            if (action.actionType == Action.ACTIONTYPE_MOVEONLY) {
                Logger.showDialogMessage("DamageCalculator: calculateDamages(1): アクション攻撃タイプではないのに，ダメージが計算されようとしています．");
                return new int[] { 0, 0 };
            } else {
                Unit atkUnit = map.getUnit(action.operationUnitId);
                Unit targetUnit = map.getUnit(action.targetUnitId);
                return calculateDamages(atkUnit, targetUnit, map, action.destinationXpos, action.destinationYpos);
            }
        }

        /// <summary> 攻撃ダメージ，反撃ダメージを計算する．Unit, mapが引数のバージョン．
        /// ※攻撃Unitの現在の位置で計算することに注意 （非標準関数）</summary>
        public static int[] calculateDamages(Unit atkUnit, Unit targetUnit, Map map) {
            return calculateDamages(atkUnit, targetUnit, map, atkUnit.getXpos(), atkUnit.getYpos());
        }

        // 攻撃ダメージ，反撃ダメージを計算する．Unit, map, 攻撃位置が引数のバージョン （標準）
        public static int[] calculateDamages(Unit atkUnit, Unit targetUnit, Map map, int atkXpos, int atkYpos) {
            int atkStars = map.getFieldDefensiveEffect(atkXpos, atkYpos);
            int targetStars = map.getFieldDefensiveEffect(targetUnit.getXpos(), targetUnit.getYpos());

            return calculateDamages(atkUnit, targetUnit, atkStars, targetStars);
        }

        // 攻撃ダメージ，反撃ダメージを計算する．Unit, ☆の数が引数のバージョン
        public static int[] calculateDamages(Unit atkUnit, Unit targetUnit, int atkStars, int targetStars) {
            return calculateDamages(atkUnit.getSpec(), atkUnit.getHP(), targetUnit.getSpec(), targetUnit.getHP(), atkStars, targetStars);
        }

        // 攻撃ダメージ，反撃ダメージを計算する．Spec, HP, ☆の数が引数のバージョン
        public static int[] calculateDamages(Spec atkSpec, int atkHP, Spec targetSpec, int targetHP, int atkStars, int targetStars) {
            int[] damages = new int[2] { 0, 0 };
            damages[0] = calculateDamage(atkSpec, atkHP, targetSpec, targetHP, targetStars);

            // 反撃ダメージの計算．ただし，
            // - 遠距離攻撃には反撃がない．
            // - 遠距離攻撃ユニットは反撃できない．
            // - 破壊されれば反撃はない．
            if (targetHP - damages[0] > 0 && atkSpec.isDirectAttackType() == true && targetSpec.isDirectAttackType() == true) {
                damages[1] = calculateDamage(targetSpec, targetHP - damages[0], atkSpec, atkHP, atkStars);
            }
            return damages;
        }

        // 攻撃ダメージのみを計算する．
        public static int calculateDamage(Spec atkSpec, int atkHP, Spec targetSpec, int targetHP, int targetStars) {
            // 攻撃対象ユニットが航空ユニットならば、地形の防御効果を考慮せず0にする
            if (targetSpec.isAirUnit() == true) targetStars = 0;

            // 相性
            int atkPower = atkSpec.getUnitAtkPower(targetSpec.getUnitType());

            // ダメージ計算式
            int rawDamage = ((atkPower * atkHP) + 70) / (100 + (targetStars * targetHP));

            if (rawDamage > targetHP) {
                rawDamage = targetHP;
            }
            return rawDamage;
        }
    }
}
