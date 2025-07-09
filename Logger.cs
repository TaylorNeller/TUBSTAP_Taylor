using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace SimpleWars {
    // 主に画面上にゲーム時の情報の表示を担う
    class Logger {
        private static TextBox sBlueTextBox;
		private static TextBox sRedTextBox;
        //private static TextBox sTextBox_UnitStatus;
        //private static TextBox sTextBox_GameLog;
        private static MainForm sOwnerForm;
		private static Label sUnitIdLabel;
        
		/// <summary>
		/// セッター
		/// </summary>
		/// <param name="ownerform">表示するフォーム</param>
		/// <param name="tbx_blue">ブルーテキストボックス</param>
		/// <param name="tbx_red">レッドテキストボックス</param>
        public static void setLogBox(MainForm ownerform, TextBox tbx_blue, TextBox tbx_red,Label lbl) {
            sOwnerForm = ownerform;
            sBlueTextBox = tbx_blue;
			sRedTextBox = tbx_red;
			sUnitIdLabel = lbl;
            //sTextBox_UnitStatus = tbx_unitstatus;
            //sTextBox_GameLog = tbx_gamelog;
        }

		/// <summary>
		/// テキストボックスと棋譜にログを追加する
		/// </summary>
		/// <param name="str">表示する文字列</param>
        public static void addLogMessage(String str,int teamColor) {
			if (teamColor == 0) {
				sBlueTextBox.Paste(str);
			} else {
				sRedTextBox.Paste(str);
			}
            SGFManager.storeComment(str);
        }

		/// <summary>
		/// 棋譜にのみログを追加する
		/// </summary>
		/// <param name="str">表示する文字列</param>
        public static void logCommentToSgf(String str) {
            SGFManager.storeComment(str);
        }

		/// <summary>
		/// テキストボックスに文字列を表示する
		/// </summary>
		/// <param name="str">表示する文字列</param>
		public static void log(String str, int teamColor) {
			if (teamColor == 0) {
				sBlueTextBox.Paste(str);
			} else {
				sRedTextBox.Paste(str);
			}

		}

		/// <summary>
		/// 通信対戦用ログ吐き
		/// </summary>
		/// <param name="str">表示する文字列</param>
		public static void netlog(String str, int teamColor) {
			if (Settings.showingNetLog) {
				if (teamColor == 0) {
					sBlueTextBox.Paste(str);
				} else {
					sRedTextBox.Paste(str);
				}
			}
		}

		public static void clearTextBox(){
			sBlueTextBox.Clear();
			sRedTextBox.Clear();
		}

		/// <summary>
		/// ブルーテキストボックスの中身を全てクリアする
		/// </summary>
        public static void clearBlueTextBox() {
            sBlueTextBox.Clear();
        }

		/// <summary>
		/// レッドテキストボックスの中身を全てクリアする
		/// </summary>
		public static void clearRedTextBox() {
			sRedTextBox.Clear();
		}
		
		/// <summary>
		/// テキストボックスの中身を一旦削除してからログを吐く
		/// </summary>
		/// <param name="str">表示する文字列</param>
        public static void clearBoxAndPrintLog(String str) {
            sBlueTextBox.Clear();
            sBlueTextBox.Paste(str);
        }

        /// <summary>
		/// ダイアログにメッセージを表示する
        /// </summary>
		/// <param name="str">表示する文字列</param>
        public static void showDialogMessage(String str) {
            MessageBox.Show(str);
        }

		// ゲーム進行中のログを表示する
		public static void showUnitStatus(String str) {
			sUnitIdLabel.Text = str;
		}

		/*
        // ユニットのステータスを専用テキストボックスに表示する
        public static void showUnitStatus(String str) {
            sTextBox_UnitStatus.Clear();
            sTextBox_UnitStatus.Paste(str);
        }

        // ゲーム進行中のログを表示する
        public static void showGameLog(String str) {
            sTextBox_GameLog.Paste(str);
        }


		 

        // 戦闘が生じたさいのログを表示する
        public static void showBattleLog(Unit attackUnit, Unit targetUnit,int[]damages) {
            string str;
            str = "アタックユニット： " + attackUnit.getName() + "(ID" + attackUnit.getID() +")" + "\r\n";
            str += "ディフェンスユニット： " + targetUnit.getName() + "(ID" + targetUnit.getID() + ")" + "\r\n\r\n";
            str += "☆戦闘ダメージ" + "\r\n";

            str += "防御側ユニット： " + targetUnit.getName() + "(ID" + targetUnit.getID() +") に " + damages[0] + " のダメージ" + "\r\n" +
            "攻撃ユニット： " + attackUnit.getName() + "(ID" + attackUnit.getID() + ") への反撃ダメージは " + damages[1] + "\r\n\r\n";
            str += "☆戦闘結果" + "\r\n";

            str += "攻撃側ユニット：" + Consts.TEAM_NAMES[attackUnit.getTeamColor()] +
            ": " + attackUnit.getName() + "(ID" + attackUnit.getID() + ") HP" + attackUnit.getHP() + "\r\n" +
            "防御側ユニット： " + Consts.TEAM_NAMES[1 - attackUnit.getTeamColor()] +
            " " + targetUnit.getName() + "(ID" + targetUnit.getID() + ") HP" + targetUnit.getHP() + "\r\n\r\n";

            showGameLog(str);
        }
		*/

		// ゲームの結果をテキストボックスに表示する
		public static void showGameResult(int teamColor) {
			sBlueTextBox.Paste(Consts.TEAM_NAMES[teamColor] + "の勝利です．");
		}
		// 自動対戦での結果をテキストボックスに吐かせる．外部にもcsvとして出力可能
        public static void showAutoBattleResult(string fileName, string mapName, string redPlayerName, string bluePlayerName, int battleCnt, int[] winCntOfRed, int[] winCntOfBlue, int drawCnt, int overTurnCnt) {
            String str = "\r\n対戦回数:" + battleCnt + "\r\n";
            str += "Red勝利回数" + winCntOfRed[0] + "\r\n";
            str += "Blue勝利回数" + winCntOfBlue[0] + "\r\n";
            str += "引き分け回数" + drawCnt + "\r\n";
            str += "ターン超過回数" + overTurnCnt + "\r\n";
            str += "ターン超過してRedが勝利" + winCntOfRed[1] + "\r\n";
            str += "ターン超過してBlueが勝利" + winCntOfBlue[1] + "\r\n";
            sBlueTextBox.Paste(str);

            bool writeTitleFlag = false;
            if (!System.IO.File.Exists(fileName)) {
                writeTitleFlag = true;
            }

            // 外に出力したいとき使用する
            using (StreamWriter w = new StreamWriter(fileName,true)) {        
                if (writeTitleFlag) {
                    w.WriteLine("MapName,RedPlayerName,BluePlayerName,BattleCnt,WinCntOfRed,WinCntOfBlue,DrawCnt,OverTrunCnt,JudgeWinCntOfRed,JudgeWinCntOfBlue,NoiseOfHP,NoiseOfPos2");
                }
                w.WriteLine(mapName + "," + redPlayerName + "," + bluePlayerName + "," + battleCnt + "," 
                    + winCntOfRed[0] + "," + winCntOfBlue[0] + "," + drawCnt + "," + overTurnCnt + ","
                    + winCntOfRed[1] + "," + winCntOfBlue[1] + ","
                    + AutoBattleSettings.IsHPRandomlyDecreased.ToString() + "," + AutoBattleSettings.IsPositionRandomlyMoved.ToString());
            }
        }

        //※※　前後半でプレイヤの先後が入れ替わる場合に使われる　※※
        //自動対戦での結果をテキストボックスに吐かせる．外部にもcsvとして出力可能
        public static void showAutoBattleResult(string fileName, string mapName, 
            string redPlayerName, string bluePlayerName, int battleCnt, 
            int[] winCntOfRed, int[] winCntOfBlue, int drawCnt, int overTurnCnt,
            int[] winCntOfRed_latter, int[] winCntOfBlue_latter, int drawCnt_latter, int overTurnCnt_latter
            ) {
            String str = "\r\n総対戦回数:" + battleCnt + "\r\n";
            str += "[前半戦] (" + (battleCnt / 2) + "/"+ battleCnt + " 戦分)\r\n";
            str += "Red勝利回数" + winCntOfRed[0] + "\r\n";
            str += "Blue勝利回数" + winCntOfBlue[0] + "\r\n";
            str += "引き分け回数" + drawCnt + "\r\n";
            str += "ターン超過回数" + overTurnCnt + "\r\n";
            str += "ターン超過してRedが勝利" + winCntOfRed[1] + "\r\n";
            str += "ターン超過してBlueが勝利" + winCntOfBlue[1] + "\r\n";
            str += "[後半戦] (" + ((battleCnt + 1) / 2) + "/" + battleCnt + " 戦分)\r\n";
            str += "Red勝利回数" + winCntOfRed_latter[0] + "\r\n";
            str += "Blue勝利回数" + winCntOfBlue_latter[0] + "\r\n";
            str += "引き分け回数" + drawCnt_latter + "\r\n";
            str += "ターン超過回数" + overTurnCnt_latter + "\r\n";
            str += "ターン超過してRedが勝利" + winCntOfRed_latter[1] + "\r\n";
            str += "ターン超過してBlueが勝利" + winCntOfBlue_latter[1] + "\r\n";
            sBlueTextBox.Paste(str);

            bool writeTitleFlag = false;
            if (!System.IO.File.Exists(fileName)) {
                writeTitleFlag = true;
            }

            // 外に出力したいとき使用する
            using (StreamWriter w = new StreamWriter(fileName, true)) {
                if (writeTitleFlag) {
                    w.WriteLine("MapName,RedPlayerName,BluePlayerName,BattleCnt,"+
                        "WinCntOfRed,WinCntOfBlue,"+
                    "DrawCnt,OverTrunCnt,JudgeWinCntOfRed,JudgeWinCntOfBlue,NoiseOfHP,NoiseOfPos1");
                }
                //先手後手が入れ替わる前，前半戦の記録
                w.WriteLine(mapName + "," + redPlayerName + "," + bluePlayerName + "," + (battleCnt/2) + ","
                    + winCntOfRed[0] + "," + winCntOfBlue[0] + "," 
                    + drawCnt + "," + overTurnCnt + ","
                    + winCntOfRed[1] + "," + winCntOfBlue[1] + ","
                    + AutoBattleSettings.IsHPRandomlyDecreased.ToString() + "," + AutoBattleSettings.IsPositionRandomlyMoved.ToString());
                //先手後手が入れ替わった後半戦の記録
                w.WriteLine(mapName + "," + redPlayerName + "," + bluePlayerName + "," + ((battleCnt + 1) / 2) + ","
                    + winCntOfRed_latter[0] + "," + winCntOfBlue_latter[0] + "," 
                    + drawCnt_latter + "," + overTurnCnt_latter + ","
                    + winCntOfRed_latter[1] + "," + winCntOfBlue_latter[1] + ","
                    + AutoBattleSettings.IsHPRandomlyDecreased.ToString() + "," + AutoBattleSettings.IsPositionRandomlyMoved.ToString());
            }
        }
        public static void saveCombatLog(string fileName, int winTeam, int resingnationFlag, int turnOfGameEnd,
            int attackCnt_0To5_R,int attackCnt_0To5_B, int attackCnt_5To10_R,int attackCnt_5To10_B, int BlueUnit,int RedUnit,
			int BlueF, int BlueA, int BlueP, int BlueU, int BlueR, int BlueI, int RedF, int RedA, int RedP, int RedU, int RedR, int RedI) {

            bool writeTitleFlag=false;
            if (!System.IO.File.Exists(fileName)) {
                writeTitleFlag = true;
            }

            // 外に出力したいとき使用する
            using (StreamWriter w = new StreamWriter(fileName, true)) {
                if (writeTitleFlag) {
                    w.WriteLine("WinTeam,Resing,EndTurn,AttackCntBlue_0_5,AttackCntRed_0_5,attackCntBlue_6_11,attackCntRed_6_11,AllUnitHP_Blue,AllUnitHP_Red," +
                    "F_UnitHP_Blue,A_UnitHP_Blue,P_UnitHP_Blue,U_UnitHP_Blue,R_UnitHP_Blue,I_UnitHP_Blue,"+
                    "F_UnitHP_Red,A_UnitHP_Red,P_UnitHP_Red,U_UnitHP_Red,R_UnitHP_Red,I_UnitHP_Red"
                    );
                }
                w.WriteLine(
                    winTeam + "," + (resingnationFlag>-1).ToString() + "," + turnOfGameEnd + "," + attackCnt_0To5_B + "," + attackCnt_0To5_R + "," + attackCnt_5To10_B + "," + attackCnt_5To10_R + "," + BlueUnit + "," + RedUnit + "," +
                    BlueF + "," + BlueA + "," + BlueP + "," + BlueU + "," + BlueR + "," + BlueI + "," +
                    RedF + "," + RedA + "," + RedP + "," + RedU + "," + RedR + "," + RedI
                    );
            }
        }
		

		// 二次元配列のデバッグ用に使用する
        public static void showArrayContents(int[,] ary) {
            int x, y;
            string str = "";
            for (x = 0; x < ary.GetLength(0); x++) {
                for (y = 0; y < ary.GetLength(1); y++) {
                    str += ary[x, y].ToString().PadLeft(3);
                }
                str += Environment.NewLine;
            }
            MessageBox.Show(str);
        }


    // python version - need to port to c#
    // @staticmethod
    // def add_turn_record(map, move, phase):
    //     # clone map matrix (map.map_field_type)
    //     map_matrix = []
    //     for row in map.map_field_type:
    //         map_matrix.append(row.copy())
    //     # create UnitData objects from units in map
    //     unit_list = []
    //     for unit in map.units:
    //         if unit is not None:
    //             unit_str = unit.spec.get_spec_name()
    //             type = ConstsData.unitNames.index(unit_str)
    //             moved = 1 if unit.is_action_finished() or unit.get_team_color() != phase else 0
    //             unit_list.append(UnitData(unit.x_pos, unit.y_pos, type, unit.team_color, unit.HP, moved))
    //     Logger.states.append((map_matrix, unit_list, str(move)))

        private static List<string> states = new List<string>();
        private static string delineator = ":";

        public static void AddTurnRecord(Map map, Action move, int phase)
        {
            StringBuilder sb = new StringBuilder();
            // sb.Append("(");
            // Clone map matrix (map.map_field_type)
            int fXsize = map.getXsize();
            int fYsize = map.getYsize();
            // int[,] MapMatrix = new int[fXsize, fYsize];
            sb.Append("[");
            for (int i = 0; i < fXsize; i++)
            {
                sb.Append("[");
                for (int j = 0; j < fYsize; j++)
                {
                    sb.Append(map.getFieldType(i, j));  
                    // MapMatrix[i, j] = map.getFieldType(i, j);
                    if (j < fYsize - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append("]");
                if (i < fXsize - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append("]");
            
            var units = map.getUnits();
            List<string> unitStrings = new List<string>();
            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (unit != null)
                {
                    string unitStr = unit.getName();
                    int type = Array.IndexOf(Spec.specNames, unitStr);

                    // Determine if the unit has “moved”: 1 if its action is finished
                    // or if its team color differs from the given phase, 0 otherwise.
                    int moved = (unit.isActionFinished() || !unit.getTeamColor().Equals(phase)) ? 1 : 0;
                    unitStrings.Add("[" + unit.getXpos() + "," + unit.getYpos() + "," + type + "," + unit.getTeamColor() + "," + unit.getHP() + "," + moved + "]");
                }
            }

            sb.Append(delineator);
            sb.Append("[");
            sb.Append(string.Join(",", unitStrings));
            sb.Append("]");
            sb.Append(delineator);

            var moveStr = move.ToString();
            moveStr = moveStr.Replace(":", ",");

            sb.Append("[" + moveStr + "]");
            // sb.Append(")");
            states.Add(sb.ToString());
        }

        private static int[] results = { 0, 0, 0 };
        // added with 'FirstMove'
        public static void showAutoBattleResult(string fileName, string mapName, string redPlayerName, string bluePlayerName, int battleCnt, int[] winCntOfRed, 
                int[] winCntOfBlue, int drawCnt, int overTurnCnt, Action firstMove, string debugStr) {
            bool writeTitleFlag = false;
            if (!System.IO.File.Exists(fileName)) {
                writeTitleFlag = true;
            }

            // 外に出力したいとき使用する
            using (StreamWriter w = new StreamWriter(fileName,true)) {    
                // w.WriteLine("idk");    
                if (writeTitleFlag) {
                    // w.WriteLine("MapName,WinCntOfRed,WinCntOfBlue,DrawCnt,FirstMove");
                    w.WriteLine("map:unitlist:move:result");
                }
                // w.WriteLine(mapName + "," + winCntOfRed[0] + "," + winCntOfBlue[0] + "," + drawCnt + "," + firstMove.ToString());
                int result = 0;
                if (winCntOfRed[0] > winCntOfBlue[0]) {
                    result = 1;
                } else if (winCntOfRed[0] < winCntOfBlue[0]) {
                    result = -1;
                }
                // foreach (var state in states) { // write every state
                //     // w.WriteLine("(" + state + "," + result + ")");
                //     w.WriteLine(state + ":" + result);
                // }
                // write result only
                // w.WriteLine(result);
                results[result+1]++;
                double total = results[0] + results[1] + results[2];
                // w.WriteLine("WLD:"+ results[2] + "," + results[0] + "," + results[1]+":"+results[2]/total+","+results[0]/total+","+results[1]/total);
                // print to 2 decimals
                w.WriteLine("WLD:"+ results[2] + "," + results[0] + "," + results[1]+":                                "+Math.Round(results[2]/total,2)+","+Math.Round(results[0]/total,2)+","+Math.Round(results[1]/total,2));
                states.Clear();
            }
        }

        // added with 'FirstMove'
        public static void showAutoBattleResult(string fileName, string mapName, 
            string redPlayerName, string bluePlayerName, int battleCnt, 
            int[] winCntOfRed, int[] winCntOfBlue, int drawCnt, int overTurnCnt,
            int[] winCntOfRed_latter, int[] winCntOfBlue_latter, int drawCnt_latter, int overTurnCnt_latter, Action firstMove, string debugStr
            ) {
            bool writeTitleFlag = false;
            if (!System.IO.File.Exists(fileName)) {
                writeTitleFlag = true;
            }

            // 外に出力したいとき使用する
            using (StreamWriter w = new StreamWriter(fileName, true)) {
                if (writeTitleFlag) {
                    w.WriteLine("MapName,RedPlayerName,BluePlayerName,BattleCnt,"+
                        "WinCntOfRed,WinCntOfBlue,"+
                    "DrawCnt,OverTrunCnt,FirstMove,JudgeWinCntOfRed,JudgeWinCntOfBlue,NoiseOfHP,NoiseOfPos1");
                }
                // w.WriteLine(debugStr);
                
                //先手後手が入れ替わる前，前半戦の記録
                w.WriteLine(mapName + "," + redPlayerName + "," + bluePlayerName + "," + (battleCnt/2) + ","
                    + winCntOfRed[0] + "," + winCntOfBlue[0] + "," 
                    + drawCnt + "," + overTurnCnt + "," + firstMove.ToString() + ","
                    + winCntOfRed[1] + "," + winCntOfBlue[1] + ","
                    + AutoBattleSettings.IsHPRandomlyDecreased.ToString() + "," + AutoBattleSettings.IsPositionRandomlyMoved.ToString());
                //先手後手が入れ替わった後半戦の記録
                w.WriteLine(mapName + "," + redPlayerName + "," + bluePlayerName + "," + ((battleCnt + 1) / 2) + ","
                    + winCntOfRed_latter[0] + "," + winCntOfBlue_latter[0] + "," 
                    + drawCnt_latter + "," + overTurnCnt_latter + "," + firstMove.ToString() + ","
                    + winCntOfRed_latter[1] + "," + winCntOfBlue_latter[1] + ","
                    + AutoBattleSettings.IsHPRandomlyDecreased.ToString() + "," + AutoBattleSettings.IsPositionRandomlyMoved.ToString());
            }
        }
    }
}
