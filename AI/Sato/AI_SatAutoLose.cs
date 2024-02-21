using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {



    // Sato AI. 
    // Min-Max Tree search. Depth-2(or 3, in case there are few units in the map). 
    class AI_AutoLose : Player{


        int counter1 = 0;
        int counter2 = 0;

        // AIの表示名（必須のpublic関数）
        public string getName() {
            return "AutoLoser";
        }

        // パラメータ等の情報を返す関数
        public string showParameters() {
            return "Loser";
        }


        // 1ユニットの行動を決定する（必須のpublic関数）
        // v1.06からは引数が4つに増えています
        public Action makeAction(Map map, int teamColor, bool turnStart, bool gameStart) {
            Action a = Action.createTurnEndAction();
            a.actionType = Action.ACTIONTYPE_SURRENDER;

            int hpsum = 0;
            foreach (Unit u in map.getUnitsList(teamColor, true, true, false)) {
                hpsum += u.getHP();
            }
            //SUCCEEDED
            if (hpsum == 14) {
                counter1++;
            }
            Console.WriteLine("Success Counter " + counter1);

            return a;

        }

    }

}
