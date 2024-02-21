using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {
    // AI作成のためのテンプレートです．
    // かならず実装しなければいけない関数が4つあります．
    // 作成したAIを実際に利用するためには，PlayerList クラスも更新してください．
    class AI_template : Player {       // ← ★ 必ず書き変えてください

        // AIの表示名を返す（必須）
        // 改行文字 \r, \n，半角大カッコ [, ]，システム文字は使わないでください．
        public string getName() {
            return "AI_XXXX";         // ← ★ 必ず書き変えてください
        }
        
        // パラメータ等の情報を返す（必須だが，空でも良い）
        // 改行は含んでも構いません．半角大カッコ [, ], システム文字は使わないでください．
        public string showParameters() {
            return "";

            // 例えば PARAM1, PARAM2 というパラメータがあって，棋譜等に残したい場合
            // return "PARAM1 = " + PARAM1 + "\r\n" + "PARAM2 = " + PARAM2;
        }

        // 1ユニットの行動を決定する（必須）
        // なお，ここでもらった map オブジェクトはコピーされたものなので，どのように利用・変更しても問題ありません．
		public Action makeAction(Map map, int teamColor, bool isTheBeggingOfTurn, bool isTheFirstTurnOfGame) {
            // --------- 情報の取得法 ---------- //

            // あなたが赤軍・青軍どちらなのかは teamColor で指定され，
            // 他の全ての情報は map オブジェクトに格納されています．     ::: mapクラス参照

            // 行動可能な自分の全ユニットを取得する
            // List<Unit> myUnits = map.getUnitsList(teamColor, false, true, false);

            // 自分の全ユニットを取得する
            // List<Unit> myUnits = map.getUnitsList(teamColor, true, true, false);

            // 全敵ユニットを取得する
            // List<Unit> enemyUnits = map.getUnitsList(teamColor, false, false, true);

            // マップ上にあるユニットを得る
            // Unit u = map.getUnit(x, y);

            // マップの地形を得る 全部・１マス
            // int[,] fields = map.getFieldTypeArray();
            // int field = map.getFieldType(x, y);

            // ユニット u の位置やHPを得る       ::: Unit クラス参照
            // int hp = u.getHP();
            // int x = u.getXpos();
            // int y = u.getYpos();
            // bool finished = u.isActionFinished();  // 行動済みか

            // ユニット u の性能を得る           ::: Spec クラス参照
            // Spec sp = u.getSpec();
            // int movePower = sp.getUnitStep(); // 移動力
            // int atkRange = sp.getUnitMaxAttackRange(); // 攻撃距離

            // ユニット u の移動できる範囲を得る
            // bool[,] isReachable = RangeController.getReachableCellsMatrix(u, map);

            // ユニット u の全ての攻撃行動を得る．Actionの中身に，攻撃相手や攻撃位置などが含まれている
            // List<Action> attackActions = RangeController.getAttackActionList(u, map)

            // 攻撃結果を予測する             ::: DamageCalculator クラス参照
            // int[] damages = DamageCalculator.calculateDamages(map, action);   // 行動から
            // int[] damages = DamageCalculator.calculateDamages(myUnit, enemyUnit, map, x, y) // ユニットと攻撃位置から

            // 先読みなどのために，マップをまるまるコピーする
            // Map copied = map.createDeepClone();

            // 先読みなどのために，マップに行動を適用する
            // map.executeAction(act);

            // 画面および棋譜に，思考ルーチンの挙動確認のためのログを表示する
            // Logger.addLogMessage("hoge value = " + value);




            // --------- 行動の返し方 --------- //

            // 何もしない場合
            return Action.createTurnEndAction();
            
            // 投了する場合
            //return Action.Resignation();

            // ユニット myU を 位置 (x,y) に移動させる場合
            //return Action.createMoveOnlyAction(U, x, y);

            // ユニット myU を 位置 (x,y) から敵ユニット enemyU に攻撃させる場合
            //return Action.createAttackAction(myU, x, y, enemyU);
        }



    }
}
