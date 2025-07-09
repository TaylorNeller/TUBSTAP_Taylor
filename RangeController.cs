using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars 
{
    /// <summary>
    /// 移動可能範囲，攻撃可能ユニットなどを取得できるクラス．
  /// システムも使うが，思考ルーチンでの利用も可能．
  /// AI_template や AI_Sample_MaxActEval もご覧ください
    /// </summary>
    class RangeController {
        // 上下左右を表す定数
        private static int[] DX = new int[4] { +1, 0, -1, 0 };
        private static int[] DY = new int[4] { 0, -1, 0, +1 };

        // opUnitが移動できる範囲を bool の matrix で返す関数
        // 敵ユニットはすり抜けられないが，味方ユニットはすり抜けられる（最終移動先にはできない）
        public static bool[,] getReachableCellsMatrix(Unit opUnit, Map map) {
            Spec unitSpec = opUnit.getSpec();  // 動かすユニットのSpec
            int unitStep = unitSpec.getUnitStep(); // ステップ数
            int unitColor = opUnit.getTeamColor(); // 動かすユニットのチーム

            bool[,] reachable = new bool[map.getXsize(), map.getYsize()];  // 到達可能か．これを返す．boolで初期化される

            int[] posCounts = new int[unitStep + 1];  // 残り移動力 index になる移動可能箇所数．0で初期化される
            int[,] posX = new int[unitStep + 1, 100]; // 残り移動力 index になる移動可能箇所． 100は適当な数，本来 Const化すべき
            int[,] posY = new int[unitStep + 1, 100];

            // 今いる場所は残り移動力 unitStep の移動可能箇所であり，最終的にも移動可能
            posCounts[unitStep] = 1;
            posX[unitStep, 0] = opUnit.getXpos();
            posY[unitStep, 0] = opUnit.getYpos();
            reachable[opUnit.getXpos(), opUnit.getYpos()] = true;

            // 残り移動力 restStep になる移動可能箇所から，その上下左右を見て，移動できるならリストに追加していく
            for (int restStep = unitStep; restStep > 0; restStep--) { // 現在の残り移動コスト
                for (int i = 0; i < posCounts[restStep]; i++) {
                    int x = posX[restStep, i];  // 注目する場所
                    int y = posY[restStep, i];

                    // この (x,y) の上下左右を見る
                    for (int r = 0; r < 4; r++) {
                        int newx = x + DX[r]; // 上下左右
                        int newy = y + DY[r];

                        int newrest = restStep - unitSpec.getMoveCost(map.getFieldType(newx, newy)); // 移動後の残り移動コスト
                        if (newrest < 0) continue;  // 周囲にあたったか，移動力が足りない →進入不可

                        Unit u = map.getUnit(newx, newy);
                        if (u != null) {
                            if (u.getTeamColor() != unitColor) continue;  // 敵ユニットにあたった →進入不可
                        }

                        // すでに移動可能マークがついた場所じゃなければ，移動可能箇所に追加する
                        if (reachable[newx, newy] == false) {
                            posX[newrest, posCounts[newrest]] = newx;
                            posY[newrest, posCounts[newrest]] = newy;
                            posCounts[newrest]++;
                            reachable[newx, newy] = true;
                        }
                    }
                }
            }

            // 味方ユニットがいる場所は，最終的には移動可能な箇所ではない
            foreach (Unit u in map.getUnitsList(unitColor, true, true, false)) {
                reachable[u.getXpos(), u.getYpos()] = false;
            }

            // 自分の場所をどう考えるかは自由だが，移動可能とするなら
            reachable[opUnit.getXpos(), opUnit.getYpos()] = true;

            return reachable;
        }

        // opUnitの攻撃行動のリストを返す
        public static List<Action> getAttackActionList(Unit opUnit, Map map) {
            List<Action> actions = new List<Action>();

            if (opUnit.getSpec().isDirectAttackType() == true) {  // opUnitが近接攻撃タイプだった場合
                bool[,] opUnitsMovableRange = getReachableCellsMatrix(opUnit, map);// opUnitの移動可能範囲配列

                // 敵ユニットが移動可能範囲に隣接しているかどうか見る
                foreach (Unit enUnit in map.getUnitsList(opUnit.getTeamColor(), false, false, true)) {
                    int posX = enUnit.getXpos();// 敵ユニットの位置
                    int posY = enUnit.getYpos();

                    for (int i = 0; i < 4; i++) {// ここで敵ユニットの上下左右をみる
                        int checkX = posX + DX[i];
                        int checkY = posY + DY[i];

                        if (opUnitsMovableRange[checkX, checkY]) {  //checkX, checkY の位置に来て攻撃ができるということ
                            actions.Add(Action.createAttackAction(opUnit, checkX, checkY, enUnit));
                        }
                    }
                }
            } else { // 間接攻撃タイプだった場合
                Spec uSpec = opUnit.getSpec();
                // 敵ユニットが攻撃可能範囲に入っているか否か見る
                foreach (Unit enUnit in map.getUnitsList(opUnit.getTeamColor(), false, false, true)) {
                    int posX = enUnit.getXpos();// 敵ユニットの位置
                    int posY = enUnit.getYpos();
                    int dist = Math.Abs(posX - opUnit.getXpos()) + Math.Abs(posY - opUnit.getYpos()); //敵ユニットと自分との距離
                    if (uSpec.getUnitMinAttackRange() <= dist && dist <= uSpec.getUnitMaxAttackRange()) {
                        actions.Add(Action.createAttackAction(opUnit, opUnit.getXpos(), opUnit.getYpos(), enUnit));// 攻撃可能範囲に含まれるならリストに追加する
                        //actions.Add(new Action(opUnit, enUnit.ge));
                    }
                }
            }
            return actions;
        }

        // opUnitの攻撃可能範囲をbool[,] matrixで返す
        // 攻撃可能な場所にはtrueが代入されている
        public static bool[,] getAttackableCellsMatrix(Unit opUnit, Map map) {
            bool[,] attackAble = new bool[map.getXsize(), map.getYsize()];  // 攻撃可能範囲をmatrix

            if (opUnit.getSpec().isDirectAttackType()) {// opUnitが近接攻撃タイプだった場合
                bool[,] movableCellsMatrix = getReachableCellsMatrix(opUnit, map);//opUnitの移動可能範囲
                bool[,] cells = new bool[map.getXsize(), map.getYsize()];

                for (int x = 1; x < map.getXsize() - 1; x++) {
                    for (int y = 1; y < map.getYsize() - 1; y++) {
                        if (map.getUnit(x, y) != null &&
                            map.getUnit(x, y).getTeamColor() == opUnit.getTeamColor()) continue;

                        // x,yの上下左右の位置を見て，そこにtrueが代入された場所が攻撃可能範囲
                        for (int i = 0; i < 4; i++) {// ここで敵ユニットの上下左右をみる
                            int checkX = x + DX[i];
                            int checkY = y + DY[i];

                            if (movableCellsMatrix[checkX, checkY]) {
                                cells[x, y] = true;// 攻撃可能範囲としてtrueに書き換え
                            }
                        }
                    }
                }

                attackAble = cells;
            } else {// 間接攻撃タイプだった場合

                Spec uSpec = opUnit.getSpec();

                for (int x = 1; x < map.getXsize() - 1; x++) {
                    for (int y = 1; y < map.getYsize() - 1; y++) {
                        int dist = Math.Abs(x - opUnit.getXpos()) + Math.Abs(y - opUnit.getYpos()); // 各(x,y)座標と自分との距離
                        if (uSpec.getUnitMinAttackRange() <= dist && uSpec.getUnitMaxAttackRange() >= dist) {//攻撃可能範囲であれば
                            attackAble[x, y] = true;// 攻撃可能範囲に入ってる位置はtrueにする
                        }
                    }
                }
            }

            return attackAble;
        }

        // operationUnitがtargetUnitに攻撃可能かどうか．ダメージ効果0の場合にはfalseが返る
        private static bool isEffective(Unit operationUnit, Unit targetUnit) {
            if (operationUnit.getSpec().getUnitAtkPower(targetUnit.getTypeOfUnit()) != 0) { return true; }
            return false;
        }
    }
}
