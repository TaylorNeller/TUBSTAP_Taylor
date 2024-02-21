using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace SimpleWars {
    public partial class MainForm : Form {
        private GameManager fGameManager;
        private DrawManager fDrawManager;
        private HumanPlayer fHumanPlayer;
        private SGFManager fSgfManager;
        private Map fReplayMap; //ここのmapはreplay専用なので注意 本来使うmapはGameManagerしか保持しない
        private string fMapFileName;
		private VSServer fVSServer;

        //保存ファイルの命名規則決め用変数
        private static int AutoBattleCnt = 1;
        private static string sNowTime = DateTime.Now.ToString("yyyyMMddHHmmss");
        private string fResultFileName = "AutoBattleResult" + sNowTime + ".csv";
        private string fCombatLogFileName = "AutoBattleCombatLog" + sNowTime + "_1" + ".csv";
        
        // コンストラクタ，初期設定
        public MainForm() {
            InitializeComponent();

            // pictureBox と drawManager の初期化
            pictureBox.Image = new Bitmap(pictureBox.Width, pictureBox.Height);
            fDrawManager = new DrawManager(this, pictureBox);

			
            // Logger の設定
            Logger.setLogBox(this, BlueLog, RedLog,UnitID);
			

            //コマンドライン
            commandlineInterface();

            // コンボボックスを初期化
            string[] stFilePathes = GetFilesMostDeep(@"./map/", "*.tbsmap");
            foreach (string stFilePath in stFilePathes) {
                comboBox_MapFile.Items.Add(Path.GetFileName(stFilePath));
            }
            comboBox_MapFile.Items.Add("AutoBattleフォルダ");
            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);
            
            PlayerList.initializePlayerListComboBox(comboBox_REDTEAM, comboBox_BLUETEAM);
            if (comboBox_MapFile.Items.Count > 0) comboBox_MapFile.SelectedIndex = 0;// マップ初期化
        }

        //ゲッター
        public int getPlayer(int teamColor) {
            if (teamColor == Consts.RED_TEAM) {
                return comboBox_REDTEAM.SelectedIndex;
            } else {
                return comboBox_BLUETEAM.SelectedIndex;
            }
        }
        
        /************************************
                マウス入力関係の関数
         ************************************/

        // ピクチャーボックス（マップ）上でマウスが押下された際に起こるイベント
        public void pictureBox_MouseDown(object sender, MouseEventArgs e) {

            switch (e.Button) {
                case MouseButtons.Right:// 右クリックされたとき
                    fHumanPlayer.pictureBoxRightMousePushed();
                    break;
                case MouseButtons.Left:// 左クリックされたとき
                    fHumanPlayer.pictureBoxPushed(e.X / DrawManager.BMP_SIZE + 1, e.Y / DrawManager.BMP_SIZE + 1);
                    break;
            }
        }

        // PictureBox上（マップ）でマウスが動いた際のイベント
        public void pictureBox_MouseMove(object sender, MouseEventArgs e) {
            // プレイヤに対してマウスがPictureBox上で動作したことを通知する
            fHumanPlayer.pictureBoxMouseMoved(e.X / DrawManager.BMP_SIZE + 1, e.Y / DrawManager.BMP_SIZE + 1);
        }

        // Exitボタンを押下するとWindowが閉じる
        private void eXITToolStripMenuItem_Click(object sender, EventArgs e) {
            Environment.Exit(0);
        }

        /*************************************
             システム関連 
         ************************************/
 
        // マップ選択用のコンボボックスの変更 マップが変更されたら表示する
        private void comboBox_MapFile_SelectedIndexChanged(object sender, EventArgs e) {
            if (comboBox_MapFile.Text == "AutoBattleフォルダ") {
                fMapFileName = "AutoBattles";
                return;
            }

            fMapFileName = @"./map/" + comboBox_MapFile.Text;

            if (fMapFileName.Equals("") == false && fDrawManager != null) {
                pictureBox.Image = new Bitmap(pictureBox.Width, pictureBox.Height);
                fDrawManager = new DrawManager(this, pictureBox);
                fDrawManager.reDrawMap(new Map(fMapFileName));
                SGFManager.showMapInfo(fMapFileName);// マップの著者名やコメントを表示する
            }
        }

        //指定ディレクトリ内の指定された拡張子を取得する
        public string[] GetFilesMostDeep(string stRootPath, string stPattern) {
            System.Collections.Specialized.StringCollection hStringCollection = (
                new System.Collections.Specialized.StringCollection()
            );

            // このディレクトリ内のすべてのファイルを検索する
            foreach (string stFilePath in System.IO.Directory.GetFiles(stRootPath, stPattern)) {
                hStringCollection.Add(stFilePath);
            }

            // StringCollection を 1 次元の String 配列にして返す
            string[] stReturns = new string[hStringCollection.Count];
            hStringCollection.CopyTo(stReturns, 0);

            return stReturns;
        }

        // ゲーム開始・終了に合わせて，GUIの各パーツのEnableを変更するための関数
        // 今のところgameStartToolStripからの使用される
        // EnableFlag=trueでゲーム中，falseでゲーム終了
        private void setEnabledGUI(bool enableFlag) {
            comboBox_REDTEAM.Enabled = !enableFlag;
            comboBox_BLUETEAM.Enabled = !enableFlag;
            comboBox_MapFile.Enabled = !enableFlag;
            loadToolStripMenuItem.Enabled = !enableFlag;
            loadreplayToolStripMenuItem.Enabled = !enableFlag;
            GameStartToolStripMenuItem.Enabled = !enableFlag;
            autoBattleToolStripMenuItem.Enabled = !enableFlag;
            bt_TurnEnd.Enabled = enableFlag;
            oneTurnToolStripMenuItem.Enabled = enableFlag;
            oneActionToolStripMenuItem.Enabled = enableFlag;
            gameEndToolStripMenuItem.Enabled = enableFlag;
            saveToolStripMenuItem.Enabled = enableFlag;
            bt_replayBackStep.Enabled = false;
            bt_replayNextStep.Enabled = false;
            bt_replayNextTurn.Enabled = false;
            bt_replayBackTurn.Enabled = false;
			startAsToolStripMenuItem.Enabled = !enableFlag;
			resingnetionToolStripMenuItem.Enabled = enableFlag;
        }
        
        private void gameEndToolStripMenuItem_Click(object sender, EventArgs e) {
            fGameManager.setGameEndFlag(true);
            if (fHumanPlayer != null) {
                fHumanPlayer.setGameEndFlag(true);
            }
            fDrawManager.clearMapImage();
            fSgfManager.clearActionLists();
        }

        private void bt_logboxclear_Click(object sender, EventArgs e) {
            BlueLog.Clear();
        }

		private void button1_Click(object sender, EventArgs e) {
			RedLog.Clear();
		}

        private void helpToolStripMenuItem_Click(object sender, EventArgs e) {
            MessageBox.Show("これは，JAIST 池田研で開発している，\n" +
                "ターン制戦略ゲームの学術研究用プログラムです.\n\n" +
                "ゲームのルールや利用法は\n" +
                "http://jaist.ac.jp/labs/is/ikedalab/tbs/ を参照ください．\n\n" +
                "不具合や質問，要望は ikedalab_inquiry@jaist.ac.jp までご連絡ください．");
        }

        // windowをとじる
        void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            Environment.Exit(0);
        }


        #region ■■ゲーム開始や，対戦形式選択，Undo, TurnEnd 等■■

        // 自動対戦を行う関数
        // AI同士でなければ，選択できない．
        private void autoBattleToolStripMenuItem_Click(object sender, EventArgs e) {
            if (PlayerList.getPlayer(comboBox_REDTEAM.SelectedIndex).getName() == "HumanPlayer") {
                MessageBox.Show("RedTeamPlayerはＡＩを選択してください。"); return;
            }

            if (PlayerList.getPlayer(comboBox_BLUETEAM.SelectedIndex).getName() == "HumanPlayer") {
                MessageBox.Show("BlueTeamPlayerはＡＩを選択してください。"); return;
            }
            if (fMapFileName == null) {
                MessageBox.Show("Mapを選択してください。");
                return;
            }

            // version 0.104で集約．読み込むマップ
            string[] stFilePathes;
            if (fMapFileName == "AutoBattles") {
                stFilePathes = GetFilesMostDeep(@"./autobattle/", "*.tbsmap");
                if (stFilePathes.Count() == 0) {
                    MessageBox.Show("autobattleにtbsmapファイルが存在しません。");
                    return;
                }
            } else {
                // autobattleじゃない場合も１個の集合と考えればよい．
                stFilePathes = new string[] { fMapFileName };
            }

            // version 0.104で追加．設定の確認．キャンセルされたら戻す．ＯＫボタンが押されたら，設定が変更されていると思えばよい．
            if (AutoBattleSettings.setAutoBattleSettings() == false) return;

            foreach (string stFilePath in stFilePathes) {
                fGameManager = new GameManager(this, stFilePath);
                fSgfManager = new SGFManager();
                fGameManager.setAutoBattleResultFileName(fResultFileName);
                fGameManager.setAutoBattleCombatLogFileName(fCombatLogFileName);
                fGameManager.setSGFManager(fSgfManager);
                fGameManager.setDrawManager(fDrawManager);
                fGameManager.enableAutoBattle();
                setEnabledGUI(true);
                bt_TurnEnd.Enabled = false;
                resingnetionToolStripMenuItem.Enabled = false;
                gameEndToolStripMenuItem.Enabled = false;
                fGameManager.initGame();
                setEnabledGUI(false);
            }

            AutoBattleCnt++;
            fCombatLogFileName = "AutoBattleCombatLog" + sNowTime + "_"+ AutoBattleCnt + ".csv";
        }

        //起動時に引数を与える事でGUI無しに動作させるための関数
        void commandlineInterface() {
            // 起動時パラメータを受け取り，サーバモードなら起動する
            string[] args = System.Environment.GetCommandLineArgs();   // args[0] は実行パスなので無視
            if (args.Length > 3) {
                // 一応，順番がどうでもいいようにハッシュを使う
                Dictionary<string, string> dict = new Dictionary<string, string>();
                for (int i = 1; i < args.Length; i++) {
                    string[] pair = args[i].Split('=');
                    dict.Add(pair[0], pair[1]);
                }

                //REDTEAMのAI指定．存在しなければSampleAI
                comboBox_REDTEAM.SelectedIndex = 2;
                for (int i = 1; i < PlayerList.sRegisteredPlayerList.Count(); i++) {
                    if (dict["user1"].Equals(PlayerList.getPlayer(i).getName()) == true) {
                        comboBox_REDTEAM.SelectedIndex = i;
                    }
                }

                //BLUETEAMのAI指定．存在しなければSampleAI
                comboBox_BLUETEAM.SelectedIndex = 2;
                for (int i = 1; i < PlayerList.sRegisteredPlayerList.Count(); i++) {
                    if (dict["user2"].Equals(PlayerList.getPlayer(i).getName()) == true) {
                        comboBox_BLUETEAM.SelectedIndex = i;
                    }
                }

                //マップファイルネームの指定 デフォルトでAutoBattles
                fMapFileName = "AutoBattles";
                if (dict.ContainsKey("map")) {
                    fMapFileName = @"./map/" + dict["map"];
                }

                //出力ファイル名指定　デフォルトでAutoBattleResultXXX.csv XXXは日付
                if (dict.ContainsKey("output")) {
                    fResultFileName = dict["output"];
                }

                if (PlayerList.getPlayer(comboBox_REDTEAM.SelectedIndex).getName() == "HumanPlayer") {
                    MessageBox.Show("RedTeamPlayerはＡＩを選択してください。"); return;
                }

                if (PlayerList.getPlayer(comboBox_BLUETEAM.SelectedIndex).getName() == "HumanPlayer") {
                    MessageBox.Show("BlueTeamPlayerはＡＩを選択してください。"); return;
                }
                if (fMapFileName == null) {
                    MessageBox.Show("Mapを選択してください。");
                    return;
                }

                if (fMapFileName == "AutoBattles") {
                    string[] stFilePathes = GetFilesMostDeep(@"./autobattle/", "*.tbsmap");
                    if (stFilePathes.Count() == 0) {
                        MessageBox.Show("autobattleにtbsmapファイルが存在しません。");
                        return;
                    }
                    foreach (string stFilePath in stFilePathes) {
                        fGameManager = new GameManager(this, stFilePath);
                        fSgfManager = new SGFManager();
                        //対戦回数指定　デフォルトで100
                        if (dict.ContainsKey("games")) {
                            AutoBattleSettings.NumberOfGamesPerMap = int.Parse(dict["games"]);
                        }
                        fGameManager.setAutoBattleResultFileName(fResultFileName);
                        fGameManager.setAutoBattleCombatLogFileName(fCombatLogFileName);
                        fGameManager.setSGFManager(fSgfManager);
                        fGameManager.setDrawManager(fDrawManager);
                        fGameManager.enableAutoBattle();
                        setEnabledGUI(true);
                        fGameManager.initGame();
                        setEnabledGUI(false);
                    }
                }
                else {
                    fGameManager = new GameManager(this, fMapFileName);
                    //対戦回数指定　デフォルトで100
                    if (dict.ContainsKey("games")) {
                        AutoBattleSettings.NumberOfGamesPerMap = int.Parse(dict["games"]);
                    }
                    fSgfManager = new SGFManager();
                    fGameManager.setAutoBattleResultFileName(fResultFileName);
                    fGameManager.setAutoBattleCombatLogFileName(fCombatLogFileName);
                    fGameManager.setSGFManager(fSgfManager);
                    fGameManager.setDrawManager(fDrawManager);
                    fGameManager.enableAutoBattle();
                    setEnabledGUI(true);
                    fGameManager.initGame();
                    setEnabledGUI(false);
                }

                Environment.Exit(0);
            }
        }

        // ターンエンドボタン
        private void bt_TurnEnd_Click(object sender, EventArgs e) {
            fHumanPlayer.fTurnEndFlag = true;
        }

		// 投了ボタン
        private void resingnetionToolStripMenuItem_Click(object sender, EventArgs e) {
            if(fHumanPlayer!=null)
            fHumanPlayer.fResignationFlag = true;
        }

        // Undo(1ターン) ゲーム進行中に押すと1ターン前の状態に戻る
        private void oneTurnToolStripMenuItem_Click(object sender, EventArgs e) {
            if (fGameManager.getTurnCount() < 2) {
                Logger.showDialogMessage("これ以上戻すことはできません");
                return;
            }
            fGameManager.undo();
        }

        // 1ユニットの行動をキャンセルする（待った！）
        private void oneActionToolStripMenuItem_Click(object sender, EventArgs e) {
            fGameManager.undoOneAction();
        }
        #endregion 


        #region ■■セーブ・ロード 棋譜再生関連の関数■■

        // saveを押すと呼び出される．Sgf形式で外部に保存される
        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            saveFileDialog.Filter = "*.sgf|*.sgf";
            if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                fSgfManager.setFileName(saveFileDialog.FileName);
                fSgfManager.saveActionListsToFile();
            }
        }

        // マップの状態を外部ファイル（Sgfファイル）から読み込むLoad(saveFile)
        // 途中対局させたいとき使用する
        private void loadToolStripMenuItem_Click(object sender, EventArgs e) {
            openFileDialog.InitialDirectory = Application.StartupPath;
            openFileDialog.Filter = "sgf|*.sgf|tbsmap|*.tbsmap";
            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                fMapFileName = openFileDialog.FileName;
                fGameManager = new GameManager(this, openFileDialog.FileName);
                fSgfManager = new SGFManager();
                pictureBox.Image = new Bitmap(pictureBox.Width, pictureBox.Height);// pictureBox作り直し
                fDrawManager = new DrawManager(this, pictureBox);
                fSgfManager.setTempMap(fMapFileName);
                fGameManager.setSGFManager(fSgfManager);
                fGameManager.setDrawManager(fDrawManager);
                //人間1
                comboBox_REDTEAM.SelectedIndex = PlayerList.HUMAN_PLAYER;
                fHumanPlayer = new HumanPlayer(this, fGameManager);
                fHumanPlayer.setDrawManager(fDrawManager);
                fGameManager.setHumanPlayer(fHumanPlayer);
                //人間2
                comboBox_BLUETEAM.SelectedIndex = PlayerList.HUMAN_PLAYER;

                fSgfManager.setInitializeMap(fGameManager.fMap);
                fSgfManager.setMap(fGameManager.fMap);
                fSgfManager.loadMapStateFromSgf(openFileDialog.FileName);
                fDrawManager.reDrawMap(fGameManager.fMap);
                setEnabledGUI(true);
                fGameManager.initGame();// loadしたマップでゲーム開始
                setEnabledGUI(false);
            }
        }

        // 棋譜再現用Load(replay)の関数
        private void loadreplayToolStripMenuItem_Click(object sender, EventArgs e) {
            openFileDialog.InitialDirectory = Application.StartupPath;
            openFileDialog.Filter = "*.sgf|*.sgf";
            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                fReplayMap = new Map(openFileDialog.FileName);
                fGameManager = new GameManager(this, openFileDialog.FileName);
                fSgfManager = new SGFManager();
                pictureBox.Image = new Bitmap(pictureBox.Width, pictureBox.Height);// pictureBox作り直し
                fDrawManager = new DrawManager(this, pictureBox);
                fGameManager.setSGFManager(fSgfManager);
                fGameManager.setDrawManager(fDrawManager);
                fGameManager.setMap(fReplayMap);
                fSgfManager.setInitializeMap(fReplayMap);// 初期状態マップのセット
                fSgfManager.setMap(fReplayMap);
                fDrawManager.reDrawMap(fReplayMap);
                fSgfManager.loadActionFromFile(openFileDialog.FileName);

                bt_replayBackStep.Enabled = true;
                bt_replayNextStep.Enabled = true;
                bt_replayNextTurn.Enabled = true;
                bt_replayBackTurn.Enabled = true;
            }
        }

        // 再生用の矢印を押した際のイベント．1手進める
        private void bt_replayNextStep_Click(object sender, EventArgs e) {
            fSgfManager.replayNextStep();
            fDrawManager.reDrawMap(fReplayMap);
        }

        // 再生用の矢印を押した際のイベント．1手戻る
        private void bt_replayBackStep_Click(object sender, EventArgs e) {
            fSgfManager.replayBackStep();
            fDrawManager.reDrawMap(fReplayMap);
        }

        // 再生用の矢印をおした際のイベント 1ターン進める
        private void bt_replayNextTurn_Click(object sender, EventArgs e) {
            fSgfManager.replayNextTurn();
            fDrawManager.reDrawMap(fReplayMap);
        }

        // 再生用の矢印をおした際のイベント 1ターン戻る
        private void bt_replayBackTurn_Click(object sender, EventArgs e) {
            fSgfManager.replayBackTurn();
            fDrawManager.reDrawMap(fReplayMap);
        }

#endregion

        private void MainForm_Load(object sender, EventArgs e) {

        }

        private void GameStartToolStripMenuItem_Click(object sender, EventArgs e) {
            if (fMapFileName == null || fMapFileName == "AutoBattleフォルダ") { MessageBox.Show("Mapを選択してください。"); return; }
			if (comboBox_REDTEAM.SelectedIndex == PlayerList.NETWORK_PLAYER && comboBox_BLUETEAM.SelectedIndex == PlayerList.NETWORK_PLAYER) { MessageBox.Show("対戦できません。"); return; }
            fGameManager = new GameManager(this, fMapFileName);
            fSgfManager = new SGFManager();
            fSgfManager.setTempMap(fMapFileName);
            fGameManager.setSGFManager(fSgfManager);
            fGameManager.setDrawManager(fDrawManager);

            if (comboBox_REDTEAM.SelectedIndex == PlayerList.HUMAN_PLAYER || comboBox_BLUETEAM.SelectedIndex == PlayerList.HUMAN_PLAYER) {
                fHumanPlayer = new HumanPlayer(this, fGameManager);
                fHumanPlayer.setDrawManager(fDrawManager);
                fGameManager.setHumanPlayer(fHumanPlayer);
                bt_TurnEnd.Enabled = true;
            }

			if (comboBox_REDTEAM.SelectedIndex == PlayerList.NETWORK_PLAYER || comboBox_BLUETEAM.SelectedIndex == PlayerList.NETWORK_PLAYER) {
				fGameManager.fNetworkBattleFlag = true;
				uNDOToolStripMenuItem.Enabled = false;
			}

            setEnabledGUI(true);// ゲーム開始前にいじられたくないものを非アクティブ化

            if (comboBox_REDTEAM.SelectedIndex != PlayerList.HUMAN_PLAYER && comboBox_BLUETEAM.SelectedIndex != PlayerList.HUMAN_PLAYER) {
                bt_TurnEnd.Enabled = false;
            }

            this.Refresh();
            Application.DoEvents(); // メニューを消すためのおまじない 意味なさそう

            fGameManager.initGame();// ゲームスタート

            setEnabledGUI(false);//ゲームが終了したのでアクティブに変更
			uNDOToolStripMenuItem.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
			fGameManager.fNetworkBattleFlag = false;
        }

		private void label1_Click(object sender, EventArgs e) {

		}

		private void optionToolStripMenuItem_Click(object sender, EventArgs e) {
			if (Option.setOption() == false) return;
		}

		private void settingToolStripMenuItem_Click(object sender, EventArgs e) {
			(new Settings()).ShowDialog();
		}

		private void startAsToolStripMenuItem_Click(object sender, EventArgs e) {
			//色々な初期化処理
			if (fMapFileName == null || fMapFileName == "AutoBattleフォルダ") { MessageBox.Show("Mapを選択してください。"); return; }

			fGameManager = new GameManager(this, fMapFileName);
			fSgfManager = new SGFManager();
			fSgfManager.setTempMap(fMapFileName);
			fGameManager.setSGFManager(fSgfManager);
			fGameManager.setDrawManager(fDrawManager);

			if (comboBox_REDTEAM.SelectedIndex == PlayerList.HUMAN_PLAYER || comboBox_BLUETEAM.SelectedIndex == PlayerList.HUMAN_PLAYER) {
				fHumanPlayer = new HumanPlayer(this, fGameManager);
				fHumanPlayer.setDrawManager(fDrawManager);
				fGameManager.setHumanPlayer(fHumanPlayer);
				bt_TurnEnd.Enabled = true;
			}

			setEnabledGUI(true);// ゲーム開始前にいじられたくないものを非アクティブ化

			if (comboBox_REDTEAM.SelectedIndex != PlayerList.HUMAN_PLAYER && comboBox_BLUETEAM.SelectedIndex != PlayerList.HUMAN_PLAYER) {
				bt_TurnEnd.Enabled = false;
			}
			this.Refresh();
			Application.DoEvents(); // メニューを消すためのおまじない 意味なさそう

			fVSServer = new VSServer();
			fGameManager.setVSServer(fVSServer);
			fVSServer.listenStart();
			Logger.addLogMessage("Waiting for 2 connections"+"\r\n",0);
			// 受付を開始する．その途中にSTOPが押されたら終了する
			while (fVSServer.fStatus != VSServer.STATUS_READY) {
				Thread.Sleep(50);
				Application.DoEvents();
				/*
				if (fPushedKey == Player_User.KEY_STOP) {
					fPushedKey = Player_User.KEY_NONE;
					fVSServer.listenEnd();
					fVSServer.closeConnection();
					Logger.addLogMessage("Waiting stopped",0);
					fVSServer.fStatus = VSServer.STATUS_DISABLED;
					setEnabledGUI(true);
					return;
				}
				 */
				if (fVSServer.fStatus == VSServer.STATUS_DISABLED) {
					Logger.addLogMessage("Error", 0);
					return;
				}
			}
			fVSServer.listenEnd();
			Logger.addLogMessage("2 players joined" + "\r\n", 0);
			fGameManager.executeNetGame_Server();
			setEnabledGUI(false);
			fVSServer.closeConnection();
		}

		//ピクチャーボックスの設定
		//マップ読み込みなおしとかに使う
		public void rsetPictureBox() {
			pictureBox.Image = new Bitmap(pictureBox.Width, pictureBox.Height);
			fDrawManager = new DrawManager(this, pictureBox);
			fGameManager.setDrawManager(fDrawManager);
			if (fHumanPlayer != null) fHumanPlayer.setDrawManager(fDrawManager);
		}





    }
}
