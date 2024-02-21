using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SimpleWars {
    static class SAT_FUNC {





        private static Random rnd = new Random();

        public static Random GET_MY_RANDOM() {
            return rnd;
        }

        public static void LIST_SHUFFLE<T>(List<T> list) {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = rnd.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        
        public static int CALC_TURN_HOLDER(SatQuickMap map) {
            return map.phaseColor;
        }

        public static int REVERSE_COLOR(int teamColor) {
            if (teamColor == Consts.BLUE_TEAM) return Consts.RED_TEAM;
            return Consts.BLUE_TEAM;
        }
        

            

        //キューの長さを切り詰めたキューを返す．元のキューは情報が壊れるので注意．
        //例：　trancateQueue( 1, 3, {11, 12, 13, 14, 15, 16}) => {12, 13, 14}
        public static Queue<int> trancateQueue(int minInd, int length, Queue<int> queue_origin) {
            Queue<int> trancated = new Queue<int>();
            int queueOriginLength = queue_origin.Count;
            for (int i = 0; i < queueOriginLength; i++) {                
                int pop = queue_origin.Dequeue();
                if (minInd <= i && i < minInd + length) {
                    trancated.Enqueue(pop);
                }
            }
            return trancated;
        }

        

        public static bool NO_UNITS_REMAIN(SatQuickMap map) {
             return map.getNumOfAliveColorUnits(Consts.BLUE_TEAM)==0 ||
                        map.getNumOfAliveColorUnits(Consts.RED_TEAM) ==0 ;
        }



        public static int POSI_TO_INT(int x, int y) {
            return x * 100 + y;
        }
        public static int[] INT_TO_POSI(int posiInt) {
            return new int[2] { posiInt / 100, posiInt % 100 };
        }


        public static int BIT_COUNT(int num) {
            if (num < 256)
                return SAT_CNST.BITCOUNT[num];
            return SAT_CNST.BITCOUNT[num%256];
        }

        public static bool isEffective(SatQuickUnit operationUnit, SatQuickUnit targetUnit) {
            if (operationUnit.getSpec().getUnitAtkPower(targetUnit.getTypeOfUnit()) != 0) { return true; }
            return false;
        }
        //ほとんど無力な場合．ダメージ係数が10以下しかない．
        public static bool isAlmostIneffective(SatQuickUnit operationUnit, SatQuickUnit targetUnit) {
            if (operationUnit.getSpec().getUnitAtkPower(targetUnit.getTypeOfUnit()) <= 10) { return true; }
            return false;
        }

        public static void PRINT_ACTIONS(List<Action> acts) {
            WriteLine("PRINT ACTIONS:");
            foreach (Action act in acts) { WriteLine(act.toOneLineString()); }
        }
        public static void PRINT_ACTIONS(Action[] acts) {
            PRINT_ACTIONS(acts.ToList());
        }
        public static void PRINT_ACTIONS(List<int> acts) {
            WriteLine("PRINT ACTIONS:");
            foreach (int act in acts) { WriteLine(SatQuickAction.toOneLineString(act)); }
        }
        public static void PRINT_ACTIONS(int[] acts) {
            PRINT_ACTIONS(acts.ToList());
        }


        public static int MANHATTAN_DIST(int x0, int y0, int x1, int y1) {
            return Math.Abs(x0 - x1) + Math.Abs(y0 - y1);
        }

        public static void INIT_2D_IntARRAY_AS(int[,] array, int k) {
            for (int i = 0; i < array.GetLength(0); i++) {
                for (int j = 0; j < array.GetLength(1); j++) {
                    array[i, j] = k;
                }
            }
        }


        public static void PRINT_UNIT(SatQuickUnit u) {
            WriteLine(u.toString());
        }
        public static void PRINT_MAP(SatQuickMap map) {
            WriteLine(map.toString());
        }
        public static void PRINT_BINARY_MAP_ARRAY(int[,] array) {
            WriteLine("PRINT BINARY MAP ARRAY:");
            for (int yi = 0; yi < array.GetLength(1); yi++) {
                Write("{");
                for (int xj = 0; xj < array.GetLength(0); xj++) {
                    Write(Convert.ToString( array[xj, yi],2).PadLeft(4,'0')+" ");
                }
                WriteLine("}");
            }
            
        }
        public static void PRINT_2D_INT_ARRAY(int[,] array) {
            WriteLine("PRINT 2D ARRAY:");
            for (int yi = 0; yi < array.GetLength(1); yi++) {
                Write("{");
                for (int xj = 0; xj < array.GetLength(0); xj++) {
                    if (array[ xj, yi] >= 0) {
                        Write("  "+array[ xj, yi] + ",");
                    } else {
                        Write(" "+array[ xj, yi] + ",");
                    }
                }
                WriteLine("}");
            }

        }

        /// <summary>
        /// Print Method(Wrappered). I can rewrite here, and I can change output window for my AI codes.
        /// </summary>
        /// <param name="obj"></param>
        public static void WriteLine(Object obj) {
            Console.WriteLine(obj.ToString());
        }
        public static void Write(Object obj) {
            Console.Write(obj.ToString());
        }


        public const string LOG_FILE_NAME_BASIC = @"SatGeneLog";
        public static string LOG_FILE_NAME = @"SatGeneLog";
        //public static DateTime.Now.ToString("yyyyMMddHHmmss");
        private static void WriteLog_Aux(Object obj, bool breakLine) {
            StreamWriter sw = new StreamWriter(LOG_FILE_NAME+".txt",true);
            if (breakLine) { sw.WriteLine(obj); 
            } else {         sw.Write    (obj); }
            sw.Close();
        }
        public static void WriteLog(Object obj) {
            WriteLog_Aux(obj, false);
        }
        public static void WriteLogLine(Object obj) {
            WriteLog_Aux(obj, true);
        }


    }
}