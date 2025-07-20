using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace SimpleWars {
    // ゲームを進行するクラス，自動対戦モード用の統計量も取る．
    // 基本的に一般ユーザは読む・利用する必要はない．
    class GameManager {
        public Map fMap;
        public DrawManager fDrawManager;
        private MainForm fForm;
        private HumanPlayer fHumanPlayer;
        private SGFManager fSgfManager;
        private Player[] fPlayers = new Player[2];  // プレイヤーインタフェース
		private VSServer fVSServer;

        private int fPhase;                         // 現在ターンのチーム
        private int fResignationFlag = -1;          //投了用フラグ
		private int fNetColor = -1;
        private bool fAutoBattleFlag = false;       // 自動対戦モードのためのフラグ
		public bool fNetworkBattleFlag = false;	//通信対戦ようフラグ
        private bool fGameEndFlag = false;          //対戦終了用フラグ
        private string fMapFileName;                // 外部ファイルから読み込んだマップファイル
        private string fAutoBattleResultFileName;   //オートバトル時の結果の出力ファイル名
        private string fAutoBattleCombatLogFileName;//オートバトル時の詳細な勝敗ログデータの出力ファイル名
        
        #region ■自動対戦モード用の統計量フィールド
//        private int battleCnt = 100;// 自動対戦モードの対戦回数（要入力）
        private int battleCntOfNow = 0;// 現在の対戦回数
        private int drawGameCnt = 0;// 自動対戦モードでの引き分けゲーム数
        private int overMaxTurnCnt = 0;// 自動対戦モードでターン制限を超過した回数
        private int[] winCntOfRed = new int[2];// 自動対戦モードのRed勝利回数・ターン制限を超過した回数も含む
        private int[] winCntOfBlue = new int[2];// 自動対戦モードのBlue勝利回数・ターン制限を超過した回数も含む
        
        //総対戦回数の前後半で先手後手を入れ替える場合，後半の結果を代わりに代入するフィールド
        //(A)先手後手入れ替えがある場合　→　通常のdrawGameCntやwinCntOfRed等には「前半の結果」のみが格納されます．
        //(B)先手後手入れ替えがない場合　→　通常のdrawGameCntやwinCntOfRedだけに全部の戦闘結果が格納されます．
        private int drawGameCnt_latterHalf = 0;
        private int overMaxTurnCnt_latterHalf = 0; 
        private int[] winCntOfRed_latterHalf = new int[2];
        private int[] winCntOfBlue_latterHalf = new int[2];

        //戦績を保持する統計量をインクリメントするラッパーメソッド．4つ分．
        //(A)後半戦で先手後手入れ替わってる時　→　後半専用の統計用フィールドに値を加算する
        //(B)前半戦または，先手後手入れ替わってない後半戦の時　→　通常の統計量フィールドに値を加算する
        private void incrementDrawCnt(){
            if (fMap.reverse) { drawGameCnt_latterHalf++; }
            else { drawGameCnt++; }
        }
        private void incrementOverMaxTurnCnt() {
            if (fMap.reverse) { overMaxTurnCnt_latterHalf++; }
            else { overMaxTurnCnt++; }
        }
        private void incrementWinCntOfRed(int index) {
            if (fMap.reverse) { winCntOfRed_latterHalf[index]++; }
            else { winCntOfRed[index]++; }
        }
        private void incrementWinCntOfBlue(int index) {
            if (fMap.reverse) { winCntOfBlue_latterHalf[index]++; }
            else { winCntOfBlue[index]++; }
        }

      #endregion

        #region ■自動対戦モード用の統計量フィールド2
        private int winTeam = 0;
        private int attackCnt_0to5_Red = 0;
        private int attackCnt_0to5_Blue = 0;
        private int attackCnt_6to11_Red = 0;
        private int attackCnt_6to11_Blue = 0;
        private int remainingBlueNumOfAll = 0;
        private int remainingRedNumOfAll = 0;
        private int remainingBlueNumOfP = 0;
        private int remainingBlueNumOfI = 0;
        private int remainingBlueNumOfU = 0;
        private int remainingBlueNumOfF = 0;
        private int remainingBlueNumOfA = 0;
        private int remainingBlueNumOfR = 0;
        private int remainingRedNumOfP = 0;
        private int remainingRedNumOfI = 0;
        private int remainingRedNumOfU = 0;
        private int remainingRedNumOfF = 0;
        private int remainingRedNumOfA = 0;
        private int remainingRedNumOfR = 0;
        private Action firstMove = null;
        private string debugString = "<none>";
        #endregion

        // コンストラクタ
        public GameManager(MainForm form, string mapFileName) {
            this.fMapFileName = mapFileName;
            this.fMap = new Map(this.fMapFileName);
            fPhase = Consts.RED_TEAM;
            this.fForm = form;
        }

        #region ■■セッター■■

        public void setSGFManager(SGFManager sgfManager) {
            this.fSgfManager = sgfManager;
        }

        public void setMap(Map map) {
            this.fMap = map;
        }

        public void setHumanPlayer(HumanPlayer humanPlayer) {
            this.fHumanPlayer = humanPlayer;
        }

        public void setDrawManager(DrawManager drawManager) {
            this.fDrawManager = drawManager;
            drawManager.setMap(this.fMap);
        }

        public void setMapFileName(string mapfileName) {
            this.fMapFileName = mapfileName;
        }

        public void setAutoBattleResultFileName(string saveFileName) {
            this.fAutoBattleResultFileName = saveFileName;
        }

        public void setAutoBattleCombatLogFileName(string saveFileName) {
            this.fAutoBattleCombatLogFileName = saveFileName;
        }

        public void setGameEndFlag(bool ended) {
            fGameEndFlag = true;
        }


        #endregion

        public int getTurnCount() {
            return fMap.getTurnCount();
        }

        // ゲーム開始前の初期化処理をここで済ませる．初期化が終わればゲーム開始
        public void initGame() {
            fPlayers[0] = PlayerList.getPlayer(fForm.getPlayer(Consts.RED_TEAM));
            fPlayers[1] = PlayerList.getPlayer(fForm.getPlayer(Consts.BLUE_TEAM));

            if (fAutoBattleFlag == true) {// 自動対戦フラグが立っている場合
                this.fMap = new Map(fMapFileName);
                makeRandomNoise();// 対戦マップにノイズを加える
                fMap.setTurnCount(0);
            }
            firstMove = null;
            debugString = "<none>";
            
            fPhase = fMap.getTurnCount() % 2;
            fSgfManager.setInitializeMap(fMap);// マップの初期状態をSGFManagerに保存しておく
            fSgfManager.setPlayerName(fPlayers[0].getName(), fPlayers[1].getName());// Playerの名前を保存しておく
            fDrawManager.reDrawMap(fMap);

			// if (fForm.getPlayer(Consts.RED_TEAM) == PlayerList.NETWORK_PLAYER || fForm.getPlayer(Consts.BLUE_TEAM) == PlayerList.NETWORK_PLAYER) {
			// 	executeNetGame_Client();
			// } else {
			executeGame();
			// }
        }
        
        // 自動高速対戦を可能にする関数
        public void enableAutoBattle() {
            fAutoBattleFlag = true;
        }

        //自動対戦用にマップの初期化する
        private void newGame() {
			//先手後手入れ替えをする場合でかつ
			//対戦回数が規定の半数を超えている場合は色の入れ替えを行う
			//尚，青が先手に変わる
			if (fAutoBattleFlag && battleCntOfNow >= AutoBattleSettings.NumberOfGamesPerMap / 2 && AutoBattleSettings.IsPosChange) {
				this.fMap = new Map(fMapFileName, true /* プレイヤの先手後手反転フラグ */);
				fPhase = Consts.BLUE_TEAM;
			} else {
				this.fMap = new Map(fMapFileName);
				fPhase = Consts.RED_TEAM;
			}
            makeRandomNoise();// 対戦マップにノイズを加える
            fMap.setTurnCount(0);

            fSgfManager.setInitializeMap(fMap);// マップの初期状態をSGFManagerに保存しておく
            fSgfManager.setPlayerName(fPlayers[0].getName(), fPlayers[1].getName());// Playerの名前を保存しておく
            fDrawManager.reDrawMap(fMap);
            fResignationFlag = -1;

            //統計フィールドの初期化
            attackCnt_0to5_Red = 0;
            attackCnt_0to5_Blue = 0;
            attackCnt_6to11_Red = 0;
            attackCnt_6to11_Blue = 0;
            remainingBlueNumOfP = 0;
            remainingBlueNumOfI = 0;
            remainingBlueNumOfU = 0;
            remainingBlueNumOfF = 0;
            remainingBlueNumOfA = 0;
            remainingBlueNumOfR = 0;
            remainingRedNumOfP = 0;
            remainingRedNumOfI = 0;
            remainingRedNumOfU = 0;
            remainingRedNumOfF = 0;
            remainingRedNumOfA = 0;
            remainingRedNumOfR = 0;
            remainingRedNumOfAll = 0;
            remainingBlueNumOfAll = 0;
        }

        // ゲームを実行する
        public void executeGame()
        {

            //自動対戦モードの場合
            if (fAutoBattleFlag)
            {
                //指定の対戦回数までゲーム実行
                while (battleCntOfNow < AutoBattleSettings.NumberOfGamesPerMap)
                {
                    inquireAI(fPlayers[fPhase], fPhase);//１ターン内の行動を決定させる

                    changePhase();//ターンを変更する version 0.104 より場所を修正．

                    if (isAllUnitDead() == true || fMap.getTurnCount() == fMap.getTurnLimit() || fResignationFlag >= 0)
                    {
                        gameEndPhase();
                        //対戦ログを保存
                        // Logger.saveCombatLog(fAutoBattleCombatLogFileName, winTeam, fResignationFlag, fMap.getTurnCount(), attackCnt_0to5_Red, attackCnt_0to5_Blue, attackCnt_6to11_Red, attackCnt_6to11_Blue,
                        //     remainingBlueNumOfAll, remainingRedNumOfAll,
                        //     remainingBlueNumOfF, remainingBlueNumOfA, remainingBlueNumOfP, remainingBlueNumOfU, remainingBlueNumOfR, remainingBlueNumOfI,
                        //     remainingRedNumOfF, remainingRedNumOfA, remainingRedNumOfP, remainingRedNumOfU, remainingRedNumOfR, remainingRedNumOfI);
                        newGame();
                        Console.WriteLine("NEW GAME");
                    }

                    // 指定の回数の対戦実験が行われている場合
                    if (battleCntOfNow == AutoBattleSettings.NumberOfGamesPerMap / 2)
                    { // CHANGED THIS: NOW IT DOES NOT RUN GAMES FOR THE AIS SWITCHED
                        // if (fMap.reverse) {
                        //     Logger.showAutoBattleResult(fAutoBattleResultFileName, fMapFileName,
                        //         fPlayers[0].getName(), fPlayers[1].getName(), AutoBattleSettings.NumberOfGamesPerMap, 
                        //         winCntOfRed, winCntOfBlue, drawGameCnt, overMaxTurnCnt,
                        //         winCntOfRed_latterHalf, winCntOfBlue_latterHalf, 
                        //         drawGameCnt_latterHalf, overMaxTurnCnt_latterHalf, firstMove, debugString
                        //         );
                        // }
                        // else {
                        //     Logger.showAutoBattleResult(fAutoBattleResultFileName, fMapFileName,
                        //     fPlayers[0].getName(), fPlayers[1].getName(),
                        //     AutoBattleSettings.NumberOfGamesPerMap, winCntOfRed, winCntOfBlue, drawGameCnt, overMaxTurnCnt, firstMove, debugString);
                        // }
                        Logger.showAutoBattleResult(fAutoBattleResultFileName, fMapFileName,
                            fPlayers[0].getName(), fPlayers[1].getName(),
                            AutoBattleSettings.NumberOfGamesPerMap, winCntOfRed, winCntOfBlue, drawGameCnt, overMaxTurnCnt, firstMove, debugString);
                        break;
                    }
                }
            }
            else
            {//HumanPlayer対AI または　AI対AIの通常対戦モードのケース
                while (true)
                {
                    if (fForm.getPlayer(fPhase) == PlayerList.HUMAN_PLAYER)
                    {
                        inquirePlayer(fPhase);//人間プレイヤーの行動の決定を待つ
                        if (fGameEndFlag) return;
                        Logger.showDialogMessage("プレイヤーのターン終了");
                        //ターンを変更する
                        changePhase();
                    }
                    else
                    {//AIプレイヤーのターンのとき
                        inquireAI(fPlayers[fPhase], fPhase);//１ターン内の行動を決定させる
                        if (fGameEndFlag) return;
                        //ターンを変更する
                        changePhase();
                    }

                    //全滅，投了，ターンオーバーを判定して終了
                    if (isAllUnitDead() || fResignationFlag >= 0 || fMap.getTurnCount() == fMap.getTurnLimit())
                    {
                        gameEndPhase();
                        break;
                    }
                }
            }

            // if (winCntOfBlue[0] == 1)
            // {
            //     return -1;
            // }
            // else if (winCntOfRed[0] == 1)
            // {
            //     return 1;
            // }
            // else
            // {
            //     return 0;
            // }
        }

		#region ■■通信対戦関係■■

		// ネット対戦用関数
		// クライアント側の処理
		public void executeNetGame_Client() {
			// 接続処理
			if (fForm.getPlayer(Consts.RED_TEAM) == PlayerList.NETWORK_PLAYER) {
				if (((NetworkPlayer)fPlayers[0]).connect() == false) {
					return;
				}
				fNetColor = 0;
			} else {
				if (((NetworkPlayer)fPlayers[1]).connect() == false) {
					return;
				}
				fNetColor = 1;
			}
			//初期化処理
			//サーバー側からルールとマップを貰う
			((NetworkPlayer)fPlayers[fNetColor]).waitInitInfo();

			//サーバーから貰ったマップを反映する
			fMap = ((NetworkPlayer)fPlayers[fNetColor]).fMap;
			fForm.rsetPictureBox();
			fDrawManager.setMap(fMap);
			fDrawManager.reDrawMap(fMap);
			
			//自信が先手なのか後手なのかで挙動が変わる
			if (((NetworkPlayer)fPlayers[fNetColor]).fColor == Consts.RED_TEAM) {
				//ループ処理 先手の場合
				while (true) {
					//こちらが打つ
					if (fForm.getPlayer((fNetColor + 1) % 2) == PlayerList.HUMAN_PLAYER) {
						inquireNetworkPlayer(Consts.RED_TEAM);
					} else {
						inquireNetworkAI(Consts.RED_TEAM, 2);
					}
					changePhase();
					//全滅，投了，ターンオーバーを判定して終了
					if (isAllUnitDead() || fResignationFlag >= 0 || fMap.getTurnCount() == fMap.getTurnLimit()) {
						gameEndPhase();
						break;
					}
					//相手に聞く
					inquireNetworkAI(Consts.BLUE_TEAM, 1);
					changePhase();
					//全滅，投了，ターンオーバーを判定して終了
					if (isAllUnitDead() || fResignationFlag >= 0 || fMap.getTurnCount() == fMap.getTurnLimit()) {
						gameEndPhase();
						break;
					}
				}
			} else {
				//ループ処理 後手の場合
				while (true) {
					//相手に聞く
					inquireNetworkAI(Consts.RED_TEAM, 1);
					changePhase();
					//全滅，投了，ターンオーバーを判定して終了
					if (isAllUnitDead() || fResignationFlag >= 0 || fMap.getTurnCount() == fMap.getTurnLimit()) {
						gameEndPhase();
						break;
					}
					//こちらが打つ
					if (fForm.getPlayer((fNetColor + 1) % 2) == PlayerList.HUMAN_PLAYER) {
						inquireNetworkPlayer(Consts.BLUE_TEAM);
					} else {
						inquireNetworkAI(Consts.BLUE_TEAM, 2);
					}
					changePhase();
					//全滅，投了，ターンオーバーを判定して終了
					if (isAllUnitDead() || fResignationFlag >= 0 || fMap.getTurnCount() == fMap.getTurnLimit()) {
						gameEndPhase();
						break;
					}
				}
			}
		}

		// ネット対専用関数
		// サーバー側の処理
		// 接続は既に確立されている
		public void executeNetGame_Server() {
			// ゲーム開始前のサーバの処理
			// 名前を尋ねる
			String s1 = fVSServer.askPlayerName(0);
			Logger.addLogMessage("Player1 : " + s1 + "\r\n", fVSServer.fLogcolor);
			String s2 = fVSServer.askPlayerName(1);
			Logger.addLogMessage("Player2 : " + s2 + "\r\n", fVSServer.fLogcolor);

			// 相手の名前を伝える
			fVSServer.sendOpponentName(0, s2);
			fVSServer.sendOpponentName(1, s1);

			// ルール，持ち時間，配珠など，共通の情報を通知する
			fVSServer.sendRules(fMap);

			//タイムオーバーした数を数える
			int[] limitoverCnt = new int[2];

			//ストップフォッチを用意する．
			Stopwatch sw = new Stopwatch();

			while (true) {
				sw.Start();
				//プレイヤーに手を要求
				inquireNetworkAI(fPhase,0);
				sw.Stop();
				//思考時間を表示
				Logger.log("ThinkingTime:" + sw.Elapsed.ToString() + "sec" + "\r\n", fVSServer.fLogcolor);
				//時間オーバーの場合
				if ((sw.ElapsedMilliseconds / 1000) > Settings.LimitTime) {
					Logger.log("TIMEOUT" + "\r\n", fVSServer.fLogcolor);
					limitoverCnt[fPhase]++;
				}

				//ストップウォッチ初期化とターンプレイヤーの交代
				sw.Reset();
				changePhase();
				//全滅，投了，ターンオーバーを判定して終了
				if (isAllUnitDead() || fResignationFlag >= 0 || fMap.getTurnCount() == fMap.getTurnLimit()) {
					Logger.log("RED  Player TIMEOUT CNT:" + limitoverCnt[0].ToString() + "\r\n", fVSServer.fLogcolor);
					Logger.log("BLUE Player TIMEOUT CNT:" + limitoverCnt[1].ToString() + "\r\n", fVSServer.fLogcolor);

					gameEndPhase();
					break;
				}
			}
		}

		//通信対戦AIに1ターンの行動を着手させる
		//setMode=0でサーバ側
		//setMode=1でクライアント受信側
		//setMode=2でクライアント送信側
		//AI専用
		private void inquireNetworkAI(int teamColor,int setMode) {
			Map copyMap;
			bool turnEndFlag = false;
			bool turnStartFlag = false;
			bool gameStartFlag = false;
			int numOfColorUnits = fMap.getUnitsList(fPhase, false, true, false).Count;// 色で指定されたチームの残りユニット数
			// ユニット数だけＡＩに問い合わせる
			for (int i = 0; i < numOfColorUnits; i++) {
				//ゲーム開始時，ターン開始時の判定処理
				if (i == 0) {
					turnStartFlag = true;
				} else {
					turnStartFlag = false;
				}
				if (turnStartFlag && (fMap.getTurnCount() == 1 || fMap.getTurnCount() == 0)) {
					gameStartFlag = true;
				} else {
					gameStartFlag = false;
				}

				if (isAllUnitDead()) { // いずれかのチームが全滅している場合
					//gameEndPhase();//終了フェイズへ
					fDrawManager.reDrawMap(fMap);
					return;
				}

				copyMap = fMap.createDeepClone();

				Action act = null;
				Unit[] units = fMap.getUnits();

				if (turnEndFlag == false) {
					// 送受信部分
					// AIにアクションを生成させる（1ユニットのアクションを生成させる）
					if (setMode == 0) { //サーバーモード
						act=fVSServer.askAction(teamColor, act);
						//受け取り処理の場合，アクションにカラーが存在しないので追加する
						if (act.actionType == Action.ACTIONTYPE_TURNEND) {
							turnEndFlag = true;
						} else {
							act.teamColor = units[act.operationUnitId].getTeamColor();
						}
					}
					if (setMode == 1) { //受信モード
						act = ((NetworkPlayer)fPlayers[fNetColor]).waitUntilOpponentAction();
						//受け取り処理の場合，アクションにカラーが存在しないので追加する
						if (act.actionType == Action.ACTIONTYPE_TURNEND) {
							turnEndFlag = true;
						} else {
							act.teamColor = units[act.operationUnitId].getTeamColor();
						}
					}
					if (setMode == 2) { //送信モード
						act = fPlayers[(fNetColor+1)%2].makeAction(copyMap, teamColor, turnStartFlag, gameStartFlag);
						((NetworkPlayer)fPlayers[fNetColor]).sendAction(act);
					}

					if (!ActionChecker.isTheActionLegalMove(act, fMap)) {// その行動が合法手であるか？
						Logger.showDialogMessage("その手は合法でないです．");
					}

					if (act.actionType == Action.ACTIONTYPE_TURNEND) {
						turnEndFlag = true;
					}

					if (act.actionType == Action.ACTIONTYPE_SURRENDER) {
						turnEndFlag = true;
						fResignationFlag = teamColor;
					}
				}

				if (turnEndFlag == true) {
					Unit u = fMap.getUnitsList(fPhase, false, true, false)[0];
					act = Action.createMoveOnlyAction(u, u.getXpos(), u.getYpos());
				}

				SGFManager.recordComment();// SGFManagerで，棋譜にコメントを保存する

				Application.DoEvents();

				if (fAutoBattleFlag == false) Thread.Sleep(Settings.TOTALWAIT);//自動対戦モードでなければスリープ

				//マップにアクションを適用する
				fMap.changeUnitLocation(act.destinationXpos, act.destinationYpos, fMap.getUnit(act.operationUnitId));
				fMap.getUnit(act.operationUnitId).setActionFinished(true);
				fDrawManager.reDrawMap(fMap);
				// アクションが攻撃タイプならば戦闘する
				if (act.targetUnitId != -1) battlePhase(act);
				fDrawManager.reDrawMap(fMap);

				fSgfManager.addUnitAction(act); // sgfManagerにアクションのログを保存する             
			}
		}

		//人間プレイヤーの行動を決定させる
		//通信対戦用
		private void inquireNetworkPlayer(int teamColor) {

			fHumanPlayer.fTurnEndFlag = false;// ターンエンドフラグを初期化
			fHumanPlayer.fResignationFlag = false;
			while (true) {
				if (isAllUnitDead()) { //ゲームエンドしているか？
					gameEndPhase();// ゲームエンドフェイズへ
					fDrawManager.reDrawMap(fMap);
					return;
				} // ゲーム終了条件を満たしていたら終了フェイズへ

				if (fMap.isAllUnitActionFinished(fPhase)) break; // 全ユニット行動終了しているか？
				Action act = fHumanPlayer.makeAction(fMap, fPhase, true, true); //プレイヤーの行動を入力待ち
				if (fGameEndFlag) return;
				if (fHumanPlayer.fResignationFlag) {
					fResignationFlag = teamColor;
					return;
				}
				fHumanPlayer.initAction(); // １ユニットの行動が決定したのでいったん初期化
				Unit[] units = fMap.getUnits();

				((NetworkPlayer)fPlayers[fNetColor]).sendAction(act);

				battlePhase(act);

				SGFManager.recordComment();

				if (units[act.operationUnitId] != null) units[act.operationUnitId].setActionFinished(true);
				fSgfManager.addUnitAction(act); // sfgManagerにアクションのログを保存する

				fDrawManager.reDrawMap(fMap);
			}
		}
		#endregion

		//何れかのチームのユニットが全滅しているかどうか
        private bool isAllUnitDead() {
            if (fMap.getNumOfAliveColorUnits(Consts.RED_TEAM) == 0) return true;
            if (fMap.getNumOfAliveColorUnits(Consts.BLUE_TEAM) == 0) return true;

            return false;
        }

        // ターンチェンジ
        private void changePhase() {
            fMap.enableUnitsAction(fPhase); // 全ての引数チームのユニットの行動を可能にする

            fDrawManager.reDrawMap(fMap);
            fSgfManager.addLogOfOneTurn(fPhase); // １ターンが終了したのでデータ保存

            fMap.incTurnCount();//ターン数をインクリメント
            fPhase = (fPhase+1)%2;//ターンチェンジ
        }

        //AIプレイヤーに1ターンの行動を着手させる
        private void inquireAI(Player AI_Player, int teamColor) {
            Map copyMap;
            bool turnEndFlag = false;
            bool turnStartFlag = false;
			bool gameStartFlag = false;
            int numOfColorUnits = fMap.getUnitsList(fPhase, false, true, false).Count;// 色で指定されたチームの残りユニット数
            // ユニット数だけＡＩに問い合わせる
            for (int i = 0; i < numOfColorUnits; i++) {
				//ゲーム開始時，ターン開始時の判定処理
				if (i == 0) {
					turnStartFlag = true;
				} else {
					turnStartFlag = false;
				}
				if (turnStartFlag && (fMap.getTurnCount() == 1 || fMap.getTurnCount() == 0)) {
					gameStartFlag = true;
				} else {
					gameStartFlag = false;
				}

                if (isAllUnitDead()) { // いずれかのチームが全滅している場合
                    //gameEndPhase();//終了フェイズへ
                    fDrawManager.reDrawMap(fMap);
                    return;
                }

                copyMap = fMap.createDeepClone();

                Action act = null;

                if (turnEndFlag == false) {
                    // AIにアクションを生成させる（1ユニットのアクションを生成させる）
                    act = fPlayers[fPhase].makeAction(copyMap, fPhase, turnStartFlag, gameStartFlag);
                    if (!ActionChecker.isTheActionLegalMove(act, fMap)) {// その行動が合法手であるか？
                        Logger.showDialogMessage("その手は合法でないです．");
                    }
                    if (act.actionType == Action.ACTIONTYPE_TURNEND) {
                        turnEndFlag = true;
                    }

                    if (act.actionType == Action.ACTIONTYPE_SURRENDER) {
                        turnEndFlag = true;
                        fResignationFlag = teamColor;
                    }
                }
                if (turnEndFlag == true) {
                    Unit u = fMap.getUnitsList(fPhase, false, true, false)[0];
                    act = Action.createMoveOnlyAction(u, u.getXpos(), u.getYpos());
                }

                // debugString = debugString+".";
                if (firstMove == null) {
                    firstMove = act;
                }
                Logger.AddTurnRecord(copyMap, act, fPhase);

                SGFManager.recordComment();// SGFManagerで，棋譜にコメントを保存する

                Application.DoEvents();

                if (fAutoBattleFlag == false) Thread.Sleep(Settings.TOTALWAIT);//自動対戦モードでなければスリープ

                //マップにアクションを適用する
                fMap.changeUnitLocation(act.destinationXpos, act.destinationYpos, fMap.getUnit(act.operationUnitId));
                fMap.getUnit(act.operationUnitId).setActionFinished(true);
				fDrawManager.reDrawMap(fMap);
                // アクションが攻撃タイプならば戦闘する
                if (act.targetUnitId != -1) battlePhase(act);
                fDrawManager.reDrawMap(fMap);

                fSgfManager.addUnitAction(act); // sgfManagerにアクションのログを保存する             
            }
        }

        //人間プレイヤーの行動を決定させる
        private void inquirePlayer(int teamColor) {

            fHumanPlayer.fTurnEndFlag = false;// ターンエンドフラグを初期化
			fHumanPlayer.fResignationFlag = false;
            while (true) {
                if (isAllUnitDead()) { //ゲームエンドしているか？
                    gameEndPhase();// ゲームエンドフェイズへ
                    fDrawManager.reDrawMap(fMap);
                    return;
                } // ゲーム終了条件を満たしていたら終了フェイズへ

                if (fMap.isAllUnitActionFinished(fPhase)) break; // 全ユニット行動終了しているか？
                Action act = fHumanPlayer.makeAction(fMap, fPhase, true,true); //プレイヤーの行動を入力待ち
                if (fGameEndFlag) return;
				if (fHumanPlayer.fResignationFlag){
					fResignationFlag = teamColor;
					return;
				}
                fHumanPlayer.initAction(); // １ユニットの行動が決定したのでいったん初期化
                Unit[] units = fMap.getUnits();

                battlePhase(act);

                SGFManager.recordComment();

                if (units[act.operationUnitId] != null) units[act.operationUnitId].setActionFinished(true);
                fSgfManager.addUnitAction(act); // sfgManagerにアクションのログを保存する

                fDrawManager.reDrawMap(fMap);
            }
        }

        // ゲームが終了してから行う処理
        private void gameEndPhase() {
            

            #region ■残りユニット数の割合を収集■
            Unit[] unit = fMap.getUnits();
            List<Unit> redTeamUnits = fMap.getUnitsList(Consts.RED_TEAM, true, true, false);//レッドチームユニット
            List<Unit> blueTeamUnits = fMap.getUnitsList(Consts.BLUE_TEAM, true, true, false);// ブルーチームユニット

            for (int i = 0; i < redTeamUnits.Count; i++) remainingRedNumOfAll += redTeamUnits[i].getHP();
            for (int i = 0; i < blueTeamUnits.Count; i++) remainingBlueNumOfAll += blueTeamUnits[i].getHP();

            for (int i = 0; i < unit.Count(); i++) {
                if (unit[i] == null) continue;
                if (unit[i].getTeamColor()==0) {
                    switch (unit[i].getTypeOfUnit()) {
                        case 0:
                            remainingRedNumOfF += unit[i].getHP();
                            break;
                        case 1:
                            remainingRedNumOfA += unit[i].getHP();
                            break;
                        case 2:
                            remainingRedNumOfP += unit[i].getHP();
                            break;
                        case 3:
                            remainingRedNumOfU += unit[i].getHP();
                            break;
                        case 4:
                            remainingRedNumOfR += unit[i].getHP();
                            break;
                        case 5:
                            remainingRedNumOfI += unit[i].getHP();
                            break;
                        default:
                            break;
                    }
                }
                else {
                    switch (unit[i].getTypeOfUnit()) {
                        case 0:
                            remainingBlueNumOfF += unit[i].getHP();
                            break;
                        case 1:
                            remainingBlueNumOfA += unit[i].getHP();
                            break;
                        case 2:
                            remainingBlueNumOfP += unit[i].getHP();
                            break;
                        case 3:
                            remainingBlueNumOfU += unit[i].getHP();
                            break;
                        case 4:
                            remainingBlueNumOfR += unit[i].getHP();
                            break;
                        case 5:
                            remainingBlueNumOfI += unit[i].getHP();
                            break;
                        default:
                            break;
                    }
                }
            }
            #endregion 

            //全滅による終了
            if (fMap.getNumOfAliveColorUnits(Consts.RED_TEAM) == 0) {
                Logger.log(Consts.TEAM_NAMES[Consts.RED_TEAM] + "が全滅しました．" + "\r\n", 0);
                Logger.showGameResult(Consts.BLUE_TEAM);
                if (fAutoBattleFlag) incrementWinCntOfBlue(0); //自動対戦モードの場合、勝利数をインクリメント
                if (fAutoBattleFlag) battleCntOfNow += 1; //自動対戦モードの場合は対戦回数をインクリメントする
                winTeam = Consts.BLUE_TEAM;
                return;
            }
            else if (fMap.getNumOfAliveColorUnits(Consts.BLUE_TEAM) == 0) {
                Logger.log(Consts.TEAM_NAMES[Consts.BLUE_TEAM] + "が全滅しました．" + "\r\n", 0);
                Logger.showGameResult(Consts.RED_TEAM);
                if (fAutoBattleFlag) incrementWinCntOfRed(0); //自動対戦モードの場合、勝利数をインクリメント
                if (fAutoBattleFlag) battleCntOfNow += 1; //自動対戦モードの場合は対戦回数をインクリメントする
                winTeam = Consts.RED_TEAM;
                return;
            }
            //投了による終了
            if (fResignationFlag == Consts.RED_TEAM) {
				Logger.log(Consts.TEAM_NAMES[Consts.RED_TEAM] + "が投了しました．" + "\r\n", 0);
                Logger.showGameResult(Consts.BLUE_TEAM);
                if (fAutoBattleFlag) incrementWinCntOfBlue(0); //自動対戦モードの場合、勝利数をインクリメント
                if (fAutoBattleFlag) battleCntOfNow += 1; //自動対戦モードの場合は対戦回数をインクリメントする
                winTeam = Consts.BLUE_TEAM;
                return;
            }
            if (fResignationFlag == Consts.BLUE_TEAM) {
				Logger.log(Consts.TEAM_NAMES[Consts.BLUE_TEAM] + "が投了しました．" + "\r\n", 0);
                Logger.showGameResult(Consts.RED_TEAM);
                if (fAutoBattleFlag) incrementWinCntOfRed(0); //自動対戦モードの場合、勝利数をインクリメント
                if (fAutoBattleFlag) battleCntOfNow += 1; //自動対戦モードの場合は対戦回数をインクリメントする
                winTeam = Consts.RED_TEAM;
                return;
            }

            // 合計ターンが超過してしまっていた場合
            if (fMap.getTurnCount() == fMap.getTurnLimit()) {
                incrementOverMaxTurnCnt(); // ターン制限を超過した回数を記録しておく．
                Logger.log("規定ターン数を超過しました．残りユニット数による勝敗の判定を行います．" + "\r\n", 0);
                int winTeamColor = judgeByRemainHP(); // 残りＨＰから勝利チームを決定する
                winTeam = winTeamColor;
                //drawGameCnt++;
                if (winTeamColor == Consts.RED_TEAM) {
                    Logger.showGameResult(Consts.RED_TEAM);
                    incrementWinCntOfRed(1);// 自動対戦モードでターンが超過して赤が勝利した回数
                    incrementWinCntOfRed(0);//自動対戦モードの場合、勝利数をインクリメント
                } else if (winTeamColor == Consts.BLUE_TEAM) {
                    Logger.showGameResult(Consts.BLUE_TEAM); //自動対戦モードの場合、勝利数をインクリメント
                    incrementWinCntOfBlue(1);//自動対戦モードで，ターンが超過して青が勝利した数
                    incrementWinCntOfBlue(0);//勝利数をインクリメント
                } else if (winTeamColor == -1) { // 合計ＨＰが両チームで等しい場合
                    incrementDrawCnt();
                    //battleCntOfNow -= 1;// 引き分けの場合は再戦する
                }
                if (fAutoBattleFlag) { battleCntOfNow += 1; }//自動対戦モードの場合は対戦回数をインクリメントする
                return;
            }
        }

        // 一定ターン数たっても勝負がつかない場合、残りＨＰの合計で決着をつける
        private int judgeByRemainHP() {
            int winTeamColor = -1; //デフォルトで-1に，差が一定値以下なら引き分けとする
            int i;
            int sumOfRedTeamHP = 0;
            int sumOfBlueTeamHP = 0;
            List<Unit> redTeamUnits = fMap.getUnitsList(Consts.RED_TEAM, true, true, false);//レッドチームユニット
            List<Unit> blueTeamUnits = fMap.getUnitsList(Consts.BLUE_TEAM, true, true, false);// ブルーチームユニット

            for (i = 0; i < redTeamUnits.Count; i++) sumOfRedTeamHP += redTeamUnits[i].getHP();

            for (i = 0; i < blueTeamUnits.Count; i++) sumOfBlueTeamHP += blueTeamUnits[i].getHP();

            if (sumOfRedTeamHP > sumOfBlueTeamHP + fMap.getDrawHPThreshold()) winTeamColor = Consts.RED_TEAM;
            else if (sumOfBlueTeamHP > sumOfRedTeamHP + fMap.getDrawHPThreshold()) winTeamColor = Consts.BLUE_TEAM;

            return winTeamColor;
        }

        // 戦闘フェイズ
        private void battlePhase(Action act) {
            if (act.actionType == Action.ACTIONTYPE_MOVEANDATTACK) {
                Unit[] units = fMap.getUnits();
				if (Option.IsCheatUsed) {
					battleForCheat(units[act.operationUnitId], units[act.targetUnitId], act);
				} else {
					battle(units[act.operationUnitId], units[act.targetUnitId], act);
				}
            }
        }

        // 攻撃対象のユニットを攻撃してダメージを与える
        private void battle(Unit attackUnit, Unit targetUnit, Action act) {

            int[] attackDamages = DamageCalculator.calculateDamages(attackUnit, targetUnit, fMap);// 攻撃ダメージ
            int counterDamage = 0;
            targetUnit.reduceHP(attackDamages[0]);// 相手のHPを減少させる
            act.X_attackDamage = attackDamages[0]; //攻撃ダメージの保存

            if (fMap.getTurnCount() < 6) {
                if (attackUnit.getTeamColor() == 0) attackCnt_0to5_Red++;
                else attackCnt_0to5_Blue++;
            }
            if (fMap.getTurnCount() < 12 && fMap.getTurnCount() > 5) {
                if (attackUnit.getTeamColor() == 0) attackCnt_6to11_Red++;
                else attackCnt_6to11_Blue++;
            }

            // ターゲユニットのHPが0以下になっていた場合
            if (targetUnit.getHP() == 0) {
                counterDamage = 0;
                act.X_counterDamage = counterDamage;// 反撃ダメージの保存
                unitDeleteProcess(targetUnit);
                //敵のHPが0以上でどちらも近接型ユニットである場合のみ反撃が行われる
            } else if (targetUnit.getHP() > 0 && attackUnit.getSpec().isDirectAttackType() && targetUnit.getSpec().isDirectAttackType()) {
                counterDamage = attackDamages[1];
                act.X_counterDamage = counterDamage;// 反撃ダメージの保存
                attackUnit.reduceHP(counterDamage);

                if (attackUnit.getHP() == 0) {// 反撃により自滅した場合
                    unitDeleteProcess(attackUnit);
                }
            } else {
                act.X_counterDamage = 0;
            }

            //Logger.showBattleLog(attackUnit, targetUnit, attackDamages);// 戦闘のログを表示する
        }

		//FWDSとの対戦用関数
		private void battleForCheat(Unit attackUnit, Unit targetUnit, Action act) {
			BattleCheat.SetBattleResult();
			if (attackUnit.getTeamColor() == 0) {
				attackUnit.setHP(BattleCheat.RedUnitHP);
				targetUnit.setHP(BattleCheat.BlueUnitHP);
			} else {
				attackUnit.setHP(BattleCheat.BlueUnitHP);
				targetUnit.setHP(BattleCheat.RedUnitHP);
			}

			// ターゲユニットのHPが0以下になっていた場合 ユニットの消去を行う
			if (targetUnit.getHP() == 0) {
				unitDeleteProcess(targetUnit);
			}
		}

        // ユニットが破壊されるフェイズ
        private void unitDeleteProcess(Unit deadUnit) {
            fMap.deleteUnit(deadUnit);
            string myText = deadUnit.getName() + deadUnit.getID() + " は破壊されました\r\n";
            myText += Environment.NewLine;
            //Logger.showGameLog(myText);
        }

        // 一手元に戻す
        public void undoOneAction() {
            Map undoMap = fSgfManager.undoOneAction(fMap);
            if (undoMap == null) return;
            this.setMap(undoMap);
            fDrawManager.setMap(undoMap);
            fHumanPlayer.initAction();
            fHumanPlayer.initMouseState();
            fDrawManager.reDrawMap(fMap);
        }

        // 1ターン元に戻す
        public void undo() {
            Map undoMap = fSgfManager.undo(fMap);
            if (undoMap == null) return;
            this.setMap(undoMap);
            fDrawManager.setMap(undoMap);
            fHumanPlayer.initAction();
            fHumanPlayer.initMouseState();
            fDrawManager.reDrawMap(fMap);
        }

        // マップ初期状態のユニット位置と残りHPをランダムに変動させる
        // 位置：上下左右１マス
        // HP ：＋－ 1
        private void makeRandomNoise() {
            Unit[] units = fMap.getUnits();
            Random r = new Random();
            int rn;

            for (int i = 0; i < units.Length; i++) {
                if (units[i] == null) continue;

                // version 0.104より．位置ずらしをなしにできる．
                if (AutoBattleSettings.IsPositionRandomlyMoved == true) {
                    rn = r.Next(4);
                    int xPos = units[i].getXpos();
                    int yPos = units[i].getYpos();

                    switch (rn) {
                        case 0:// 左にずらす
                            if (isUnitCanMove(xPos - 1, yPos, units[i])) {
                                fMap.changeUnitLocation(xPos - 1, yPos, units[i]);
                            }
                            break;
                        case 1:// 右にずらす
                            if (isUnitCanMove(xPos + 1, yPos, units[i])) {
                                fMap.changeUnitLocation(xPos + 1, yPos, units[i]);
                            }
                            break;
                        case 2:// 上にずらす
                            if (isUnitCanMove(xPos, yPos - 1, units[i])) {
                                fMap.changeUnitLocation(xPos, yPos - 1, units[i]);
                            }
                            break;
                        case 3:// 下にずらす
                            if (isUnitCanMove(xPos, yPos + 1, units[i])) {
                                fMap.changeUnitLocation(xPos, yPos + 1, units[i]);
                            }
                            break;
                        default:
                            break;
                    }
                }

                // version 0.104より．ＨＰへらしをなしにできる．
                if (AutoBattleSettings.IsHPRandomlyDecreased == true) {
                    rn = r.Next(2);

                    if (rn == 0 && units[i].getHP() <= 9) {
                        units[i].raiseHP(1);// HPを1上昇させる
                    } else if (rn == 1 && units[i].getHP() >= 2) {
                        units[i].reduceHP(1);// HPを1減少させる
                    }
                }
            }

        }

        // その位置にユニットが移動可能か？
        private bool isUnitCanMove(int xPos, int yPos, Unit selectUnit) {

            // その位置に既にユニットが存在する場合
            if (fMap.getUnit(xPos, yPos) != null) return false;

            //ユニットの移動しようとしている場所がそのユニットにとって移動不可であった場合
            if (selectUnit.getSpec().getMoveCost(fMap.getFieldType(xPos, yPos)) == 99) {
                return false;
            }

            return true;
        }

		public void setVSServer(VSServer server) {
			fVSServer = server;
		}
    }
}
