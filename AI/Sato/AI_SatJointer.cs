using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {



    // Sato AI. 
    // Min-Max Tree search. Depth-2(or 3, in case there are few units in the map). 
    class AI_Jointer : Player{


        Player pA = new AI_DLMC();
        Player pB = new AI_UCT_PW();
        Player pC = new AI_UCT_OA();

        Player p1 = null;
        Player p2 = null;

        public AI_Jointer() {
            p1 = pA;
            p2 = pB;
        }


        // AIの表示名（必須のpublic関数）
        public string getName() {
            return "J(1)_1T"+p1.getName()+"->2T~"+p2.getName();
        }

        // パラメータ等の情報を返す関数
        public string showParameters() {
            return "J(1)_1T" + p1.showParameters() + "->2T~" + p2.showParameters();
        }


        // 1ユニットの行動を決定する（必須のpublic関数）
        // v1.06からは引数が4つに増えています
        public Action makeAction(Map map, int teamColor, bool turnStart, bool gameStart) {
            //Action a; //= Action.createTurnEndAction() ;
            if (map.getTurnCount() <= 1) {
                //Console.WriteLine("Trn" + map.getTurnCount());
                return p1.makeAction(map, teamColor, turnStart, gameStart);
            } else {
                //Console.WriteLine(" B Trn" + map.getTurnCount());
                return p2.makeAction(map, teamColor, turnStart, gameStart);
            }

        }

    }

}
