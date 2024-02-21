using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;

namespace SimpleWars
{
    /// <summary>
    /// 棋譜の保存やマップの読込などを担当するクラス．
    /// 一般ユーザは読む・理解する必要はありません．
    /// </summary>
    class SGFManager
    {
        private string[] fPlayerNames = new string[2];// プレイヤー名 棋譜に保存するときに必要
        private String fFileName;// 外部から読み込んできた棋譜ファイル名
        private int fLimitTime;// ゲーム終了時まで制限時間  必要ないかもしれない
        private DateTime fDateTime;// 現在の時刻や日付，ゲームが開始されてからの時間をもつ
        private static List<Action> sActionListOfTheTurn = new List<Action>(); //1ターンにおける行動リストを保存するリスト              
        private static List<List<Action>> sActionLists   = new List<List<Action>>();// 行動リストのリスト 別のクラス作った方が良い気がする
        private static List<CommentStruct> sCommentList;// 棋譜に保存するためのコメントリスト
        private static string strBuffer;// コメントを蓄えるバッファ
        private List<int> fTeamColorOfActionList  = new List<int>();// アクションリスト（あるターンのチームカラー）を記録する配列

        private Map fMap;// 棋譜の再生用途のマップ
        private Map fInitializeMap;// 初期状態のマップ,ゲーム開始時にここに保存しておく
        private string fTempMapFile;// 一時的なマップファイル.ゲーム開始時に選択したファイル
        //public Map tempMapForAutobattle;// 自動対戦実験にノイズを加えたマップを保存する

        int stepCnt = 0; // 棋譜を初手から再生する際に使用するカウンタ
		int turnCnt = 0; // 棋譜を初手から再生する際に使用するカウンタ
        private List<Action> allStepLog; // 棋譜を再生する際に初手から

        // コンストラクタ
        public SGFManager(){
            sActionListOfTheTurn = new List<Action>();
            sActionLists         = new List<List<Action>>();
            sCommentList         = new List<CommentStruct>();
            stepCnt = 0;
        }
        
        // プレーヤーの名前をセッティングする(今は使用していない）
        public void setPlayerName(String player1,String player2){
            fPlayerNames[0] = player1;
            fPlayerNames[1] = player2;
        }

        // ゲーム開始時DateTimeをセットする
        public void setDateTime(DateTime dTime) {
            this.fDateTime = dTime;
        }

        public void setLimitTime(int t){
            fLimitTime = t;
        }

        /*****セッター*******/

        // マップの初期状態をセットする
        // GameMangerのinitGameで，呼び出される
        public void setInitializeMap(Map aMap) {
            this.fInitializeMap = aMap.createDeepClone();// DeepClone作成
        }

        public void setMap(Map map){
            this.fMap = map;
        }

        public void setFileName(String fName){
            this.fFileName = fName;        
        }

        public void setTempMap(string mapFile) {
            this.fTempMapFile = mapFile;
        }

        public void clearActionLists() {
            sActionListOfTheTurn = new List<Action>();
            sActionLists = new List<List<Action>>();
            sCommentList = new List<CommentStruct>();
        }

        /***********ゲッター*********/

        public List<Action> getUpdate() {
            return sActionListOfTheTurn;
        }

        public List<List<Action>> getUpdateLog() {
            return sActionLists;
        }

        public Map getInitializeMap() {
            return this.fInitializeMap;
        }

        public string getPlayerName(int index) {
            if (index == 0){            
                return fPlayerNames[0];
            } else if(index == 1){
                return fPlayerNames[1];
            } else {
                Logger.showDialogMessage("playerのインデックスが誤っています．");
                return null;
            }
        }

        public void saveAutobattleFile(){
            string mapFile = toStringMapFile(fInitializeMap);
            /*
            // 時刻と現在のルールセットを保存する
            DateTime dt = DateTime.Now;
            sw.WriteLine("DT[" + dt.ToString() + "];");
            sw.WriteLine("RU[" + Consts.RULE_SET_VERSION + "];");
            // playerの名前を保存する
            sw.WriteLine("PR[" + fPlayerNames[0] + "]" + "PB[" + fPlayerNames[1] + "];");

            */
        }

        // ファイルに現在のマップの状態を保存する
        public void saveActionListsToFile(){
            using (StreamWriter sw = new StreamWriter(fFileName)){
			    using(StreamReader sr= new StreamReader(fTempMapFile, System.Text.Encoding.GetEncoding("shift_jis"))){
					String line;
					while ((line = sr.ReadLine()) != null) {
						sw.WriteLine(line);
					}

                    // 時刻と現在のルールセットを保存する
                    DateTime dt = DateTime.Now;
                    sw.WriteLine("PLAYEDDATE[" + dt.ToString() + "];");
                    sw.WriteLine("PLAYEDRULE[" + Consts.RULE_SET_VERSION + "];");
                    // playerの名前を保存する
                    sw.WriteLine("PLAYERRED[" + fPlayerNames[0] + "]" + "PLAYERBLUE[" + fPlayerNames[1] + "];");

					saveActionListsToFile(sw);
				}
			}
        }
        
        // 行動のログを保存する
        private void saveActionListsToFile(StreamWriter sw) {
            List<Action> liUpdate;
            for (int i = 0; i < sActionLists.Count; i++){
                //Action act = sActionLists[i].Last(); // 
                
                int teamColor = fTeamColorOfActionList[i];// 今のターンの手番（チームカラー）               

                liUpdate = sActionLists[i];// 
                int numOfActions = liUpdate.Count; // i番目リスト（ターン）における行動数

                // そのターンにおける，ターン数,手番(teamColor),行動数を保存する
                sw.WriteLine("TURNINFO[" + i + "," + teamColor +  "," + 
                    numOfActions + "];"); 

                // そのターンでの行動数だけアクションの中身をファイルに保存する
                for (int j = 0; j < liUpdate.Count; j++){
                    sw.WriteLine("ACTION[" + liUpdate[j].toSGFString() + "];");

                    foreach (CommentStruct cSt in sCommentList) {
                        if (cSt.turnCnt == i && cSt.stepCnt == j) {
                            sw.WriteLine("COM[" + cSt.commnet + getAllStepLogCnt(cSt) +"];");
                        }
                    }
                }
            }
            
            // save時のターンにおいて行動リストにアクションが残っていれば
            // それらも保存する
            if (sActionListOfTheTurn.Count != 0) {
                // 
                sw.WriteLine("TURNINFO[" + sActionLists.Count + "," + sActionListOfTheTurn[0].teamColor + "," +
                    sActionListOfTheTurn.Count + "];"); 

                for (int j = 0; j < sActionListOfTheTurn.Count; j++) {
                    sw.WriteLine("ACTION[" + sActionListOfTheTurn[j].toSGFString() + "];");

                    foreach (CommentStruct cSt in sCommentList) {
                        if (cSt.turnCnt == sActionLists.Count && cSt.stepCnt == j) {
                            sw.WriteLine("COM[" + cSt.commnet + getAllStepLogCnt(cSt) + "];");
                        }
                    }
                }
            }
        }

        private int getAllStepLogCnt(CommentStruct cSt){
            int cnt = 0;

            for (int i = 0; i < cSt.turnCnt; i++) {
                cnt += sActionLists[i].Count;// 1ターンにおける行動数
            }

            cnt += cSt.stepCnt;

            return cnt;
        }

        // ファイル(sgf,tbsmap両対応)からマップの状態を読み込む
		// ただし，ロードした途中経過を初期状態としているので行動ログは削除する
		// これによってロードした次点までのUNDOしか使用できない．
        public void loadMapStateFromSgf(string fileName){
            loadActionFromFile(fileName);
			//loadMapState();
            List<Action> liAct = sActionLists.Last();
            goBackNextTurn(sActionLists.Count - 1);// save時のターンの状態まで再生する

            //fMap.setTurnCount(fMap.getTurnCount() - 1);// 1ターン前に戻す

            for (int i = 0; i < liAct.Count; i++) {// 途中対局開始ターンのアクションを適用する
                fMap.actControl(liAct[i]);
            }

            deleteLog();
            deleteUnitAction();
        }

		//LoadMapStateFromSgfの内部関数，replay用の関数を流用 マップの途中経過状態を作る
		//変則的な書き方
		private void loadMapState() {
			turnCnt = sActionLists.Count - 1;
			if (turnCnt < 0) return;
			replayNextTurn();
		}

        // 行動のログをファイルから読み込む
        public void loadActionFromFile(string fileName){

            // まず一旦全部読む
            String fullLine = "";
            using (StreamReader sr = new StreamReader(fileName)){
                String line;
                while ((line = sr.ReadLine()) != null){
                    fullLine += line;
                }
            }

            allStepLog = new List<Action>();
            // セミコロンで分解
            String[] lines = fullLine.Split(';');
            sCommentList = new List<CommentStruct>();

            for (int i = 0; i < lines.Length; i++){
                string[] ones;
                string one = searchOne(lines[i], "TURNINFO");
                if (!one.Equals("")){ 
                    ones = one.Split(',');
                    List<Action> actListOfTurn = searchStep(i, lines, Int32.Parse(ones[1]),Int32.Parse(ones[2]));
                    sActionLists.Add(actListOfTurn);
                }
                
                string oneCommnet = searchOne(lines[i],"MAPCOM");
                if(!oneCommnet.Equals("")) continue;// 

                string oneComment = searchOne(lines[i], "COM");// コメントが書かれている行を探す

                if (!oneComment.Equals("")) {
                    ones = oneComment.Split(',');
                    int stepCnt = Int32.Parse(ones[ones.Length - 1]);// そのコメントが挿入されたさいの，ステップ数

                    for (int j = 0; j < ones.Length - 1; j++) {
                        sCommentList.Add(new CommentStruct(0, stepCnt, ones[j]));// stepCntとコメントを保存する
                    }
                }
            }
        }

        // 棋譜からステップ（ユニットの行動）を読み取る
        private List<Action> searchStep(int sIndex, String[] lines, int teamColor, int numOfActions) {
            deleteUnitAction();
            List<Action> actListOfTurn = new List<Action>();
            int CmtCntInTurn = 0;// ターン内のコメント数

            {
                int k = sIndex + 1;
                string one;
                do {
                    one = searchOne(lines[k], "TURNINFO");
                    string comOne = searchOne(lines[k], "COM");

                    if (comOne.Equals("") == false) {
                        CmtCntInTurn++;
                    }
                    k++;
                } while (one.Equals("") == true && k < lines.Length);
            }

            for (int i = sIndex + 1; i <= numOfActions + CmtCntInTurn + sIndex; i++) {
                String one = searchOne(lines[i], "ACTION");

                if (one.Equals("")) continue;// 空文字ならスキップする

                Action a = Action.Parse(one);
                a.teamColor = teamColor;
                allStepLog.Add(a);
                actListOfTurn.Add(a);
                // fActionListOfTheTurn.Add(a);

                //string[] ones
                //addUnitsActionFromFile(teamColor,ones);
            }
            return actListOfTurn;
        }
        
        // ひとつ前のターンに戻す(将棋とかのゲームで使われる、いわゆる待った！）
        // GamaManagerから呼ばれる
        // 引数のMapは，GameManagerでHumanPlayerが弄っている最中のMap
        public Map undo(Map realMap){

            if (sActionLists.Count <= 1 || sActionLists.Count == 0) {
                Logger.showDialogMessage("これ以上戻すことはできません");
                return null;
            }

			Map undoMap = realMap;
            undoMap.setTurnCount(0);

            // マップ初期状態のユニットをコピーする
			fInitializeMap.createDeepClone(undoMap);

            for (int i = 0; i < sActionLists.Count - 2; i++) {
                sActionListOfTheTurn = sActionLists[i];

                for (int j = 0; j < sActionListOfTheTurn.Count; j++) {
                    undoMap.actControl(sActionListOfTheTurn[j]);
                }
                undoMap.incTurnCount();
                undoMap.resetActionFinish();
            }

            //最初の2Tであれば
            if (sActionLists.Count == 2) {
                deleteLog();
				return undoMap;
            }

            sActionLists.RemoveAt(sActionLists.Count - 2);
            sActionLists.RemoveAt(sActionLists.Count - 1);
            deleteUnitAction();

            return undoMap;
        }

        // １行動（ユニット１体の行動）を終える前の状態に戻す
        // GameManagerから呼ばれる
        // 引数のMapは，GameManagerでHumanPlayerが弄っている最中のMap
        public Map undoOneAction(Map realMap) {
            Map undoMap = realMap;
            List<Action> update;

            if (sActionListOfTheTurn.Count == 0) {
                Logger.showDialogMessage("これ以上戻すことはできません");
                return null;
            }

            // マップ初期状態のユニットをコピーする
			fInitializeMap.createDeepClone(undoMap);

            // ゲーム開始ターンから，現在のターンまで再生する
            for (int i = 0; i < sActionLists.Count; i++) {
                update = sActionLists[i];

                for (int j = 0; j < update.Count; j++) {
                    undoMap.actControl(update[j]);
                }
				undoMap.incTurnCount();
                undoMap.resetActionFinish();
            }

            // 待ったをする直前の状態に再現する
            for (int j = 0; j < sActionListOfTheTurn.Count - 1; j++) {
                undoMap.actControl(sActionListOfTheTurn[j]);
            }

            sActionListOfTheTurn.RemoveAt(sActionListOfTheTurn.Count - 1);// 一手前の状態に戻すので，そのターンの行動リストの最後要素を削除

            return undoMap;
        }

        // replay時に次のステップへ
        public void replayNextStep(){
            if (stepCnt > allStepLog.Count-1) return; 
			fInitializeMap.createDeepClone(fMap);
            showComment(stepCnt);// 棋譜中にコメントがあれば表示する
            stepCnt++;
            goBackNextStep(stepCnt, allStepLog);
        }

        // replay時に前のステップへ
		public void replayBackStep(){
            if (stepCnt < 1)  return; 
			fInitializeMap.createDeepClone(fMap);
            showComment(stepCnt);// 棋譜中にコメントがあれば表示する
            stepCnt--;
            goBackNextStep(stepCnt, allStepLog);
        }

		// 棋譜再生用関数、sIndexまでユニットの行動ログを初期状態から再生する
		public void goBackNextStep(int sIndex, List<Action> actLog) {

			int colorOfControlUnit = actLog[0].teamColor;
			fMap.setTurnCount(0);
            turnCnt = 0;

			for (int i = 0; i < sIndex; i++) {
				if (colorOfControlUnit != actLog[i].teamColor) {
					colorOfControlUnit = actLog[i].teamColor;
					fMap.resetActionFinish();
                    fMap.incTurnCount();
					turnCnt++;
				}

				fMap.actControl(actLog[i]);
			}
		}

        // 棋譜中に保存されたコメントを表示する関数
        private void showComment(int stepCnt) {
            for (int i = 0; i < sCommentList.Count; i++) {
                CommentStruct cSt = sCommentList[i];

                if (cSt.stepCnt == stepCnt) {
                    Logger.log(cSt.commnet + "\r\n",0);// コメントをログボックスに表示する
                }
            }
        }

        // replay時に次のターンへ
        public void replayNextTurn() {
			if (turnCnt > sActionLists.Count - 1)  return;
			if (turnCnt < 0) turnCnt = 0;
			fInitializeMap.createDeepClone(fMap);
			turnCnt++;
			goBackNextTurn(turnCnt);
		}

		// replay時に前のターンへ
		public void replayBackTurn() {
			if (turnCnt < 1) return;
			if (turnCnt > sActionLists.Count - 1) turnCnt = sActionLists.Count - 1;
			fInitializeMap.createDeepClone(fMap);
			turnCnt--;
			goBackNextTurn(turnCnt);
		}

		// 棋譜再生用関数、sIndexTurnのターンまでユニットの行動ログを初期状態から再生する
		public void goBackNextTurn(int sIndexTurn) {
			fMap.setTurnCount(0);
			stepCnt = 0;
			for (int i = 0; i < sIndexTurn; i++) {
				List<Action> ActionListOfTheTurn = sActionLists[i];
				for (int j = 0; j < ActionListOfTheTurn.Count; j++) {
					fMap.actControl(ActionListOfTheTurn[j]);
					stepCnt++;
				}
				fMap.incTurnCount();
				fMap.resetActionFinish();
			}
		}

        // 1ユニットのアクションを入れ込む
        // GameManagerにて，1ユニットの行動が決定すれば
        // その行動を保存する
        public void addUnitAction(Action act){
            sActionListOfTheTurn.Add(act);
        }

        // 1ターンの行動の集合をListにつっこむ
        // GameManagerで，1ターンの行動が決定すれば，
        // 各ユニットの行動リストをさらにリストに格納
        public void addLogOfOneTurn(int teamColor){
            sActionLists.Add(sActionListOfTheTurn);
            sActionListOfTheTurn = new List<Action>(); // updateを再び作り直す
            fTeamColorOfActionList.Add(teamColor);// チームカラーリストにそのターン行動したカラーを登録
        }

        //現在のupdateの破棄
        public void deleteUnitAction(){
            sActionListOfTheTurn = new List<Action>();
        }

        //ログの破棄
        public void deleteLog(){
            sActionLists = new List<List<Action>>();
        }
        
        // 引数のmapクラスをsting型のマップファイルに変換する
        public string toStringMapFile(Map map){

            string str = "";
            // マップのサイズなどを書き込む
            List<Unit> redTeamUnis = map.getUnitsList(Consts.RED_TEAM,true,true,false);
            List<Unit> blueTeamUnits = map.getUnitsList(Consts.BLUE_TEAM,true,true,false);

            str += "SZX[" + map.getXsize() + "]" + "SZY[" + map.getYsize() + "]"
                + "UN0[" + redTeamUnis.Count + "]" + "UN1[" + blueTeamUnits.Count + "]"
                + "TLM[" + map.getTurnLimit() + "]" + "HPTH[" + map.getDrawHPThreshold() + "];" +"\r\n";

            // 地形の情報を書き込む
            for (int y = 0; y < map.getYsize(); y++) {
                str += "MS[";
                for (int x = 0; x < map.getXsize() - 1; x++) {
                    str += map.getFieldType(x, y) + ",";
                }
                str += map.getFieldType(map.getXsize() - 1,y) + "];" + "\r\n";
            }

            // ユニットの情報を書き込む
            Unit[] units = map.getUnits();
            for (int i = 0; i < units.Length; i++) {
                str += "US[" + units[i].getXpos() + "," + units[i].getYpos() + "," + units[i].getName() + ","
                    + units[i].getTeamColor() + "," + units[i].getHP() + "," + units[i].isActionFinished()
                    + "];" + "\r\n";
            }

            // 時刻と現在のルールセットを保存する
            DateTime dt = DateTime.Now;
            str += "DT[" + dt.ToString() + "];" + "\r\n";
            str += "RU[" + Consts.RULE_SET_VERSION + "];" + "\r\n";
            // playerの名前を保存する
            str += "PR[" + fPlayerNames[0] + "]" + "PB[" + fPlayerNames[1] + "];" +"\r\n";

            return str;
        }
        

        // PB[User] などの中身User をタグPB から探す
        public static String searchOne(String src, String tag) {
            int idx = src.IndexOf(tag + "[");
            if (idx == -1) return "";
            int idxto = idx + tag.Length + 1;
            idx = idxto;
            while (true){
                if (src.Length == idxto) return "";
                if (src.Substring(idxto, 1).Equals("]") == true){
                    return src.Substring(idx, idxto - idx);
                }
                idxto++;
            }
        }
        

        // 棋譜中の指定位置にコメントを残す関数
        public static void recordComment(){
            if (strBuffer == null || strBuffer.Equals("")) return;// バッファが空またはnullなら戻る
            CommentStruct cStruct = new CommentStruct(sActionLists.Count,sActionListOfTheTurn.Count,
                strBuffer);
            sCommentList.Add(cStruct);// コメントをリストの中に格納する
            strBuffer = "";
        }

        // ログ（コメント）を追記で保存する
        public static void storeComment(string str){
            str += ",";
            strBuffer += str;
        }

        // マップのの作者名やコメントを表示
        public static void showMapInfo(string mapName) {
            Logger.clearTextBox();

            string author = "";
            string comment = "";
            string one = "";

            // まず一旦全部読む
            String fullLine = "";
            using (StreamReader sr = new StreamReader(mapName, System.Text.Encoding.GetEncoding("shift_jis"))) {
                String line;
                while ((line = sr.ReadLine()) != null) {
                    fullLine += line;
                }
            }

            string[] lines = fullLine.Split(';');

            for (int i = 0; i < lines.Length; i++) {
                one = searchOne(lines[i], "AUTHOR");
                if (!one.Equals("")) author = one;

                one = searchOne(lines[i], "MAPCOM");
                if (!one.Equals("")) comment = one;
            }

            Logger.log("マップ著者名[" + author + "]" + "\r\n",0);
            Logger.log("コメント" + "\r\n[" + comment +"]" + "\r\n",0);
        }
    }

    class CommentStruct {
        public int turnCnt;// それまでのターンの合計
        public int stepCnt;// そのターンにおけるステップ合計カウント
        public string commnet;// 棋譜中に残したいコメント

        public CommentStruct(int tCnt,int sCnt,string comment) {
            this.turnCnt = tCnt;
            this.stepCnt = sCnt;
            this.commnet = comment;
        }
    }
}
