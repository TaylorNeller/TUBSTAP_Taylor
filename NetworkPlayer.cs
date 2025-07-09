using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace SimpleWars {
    // ネットワーク対戦用．まだ実装していない
    class NetworkPlayer :Player {

		// フィールド
		private TcpClient fSocket;
		private NetworkStream fStream;
		private String fRestString;    // あるコマンドの範囲を超えて受け取った文字列は残しておく
		public Map fMap;
		public int fColor = 0;
		public int fLogcolor = 0;

		private String fOpponentName;

        // AIの名前
        public string getName() {
			if (fOpponentName == null) {
				return "NetworkPlayer";
			} else {
				return fOpponentName;
			}
        }

        // version 0.104より追加．ゲーム開始時に呼ばれる．何もしなくとも良い．
        public void gameStarted() {
        }

        // version 0.104より追加．ターン開始時に呼ばれる．何もしなくとも良い．
        public void turnStarted() {
        }
		
        // 1ユニットの行動を決定する
		public Action makeAction(Map map, int teamColor, bool startTurn, bool gameStart) {
			Action action=new Action();
			return action;
        }

		//初期情報（名前，マップ，チームカラー）のやり取り
		public void waitInitInfo() {
			//名前を聞かれるのを待つ
			Logger.netlog("I am waiting command \"name\"" + "\r\n", fLogcolor);
			waitFor("name");
			respond(Settings.userName);  //返事する

			// 相手を教えてもらう
			fOpponentName = waitFor("opponent");
			respond("");
			Logger.netlog("Opponent name = " + fOpponentName + "\r\n", fLogcolor);

			//マップを聞く
			fMap = new Map();
			fMap.reciveMapFile(waitFor("MAP"));
			respond("");

			//自軍色を聞く
			fColor = Int32.Parse(waitFor("Color"));
			fLogcolor = (fColor + 1) % 2;
			respond("");
			
		}

		//genmoveを待ち答えを返す
		public void sendAction(Action aAct){
			waitFor("Genmove");
			respond(aAct.toSGFString());
			Logger.netlog("Send move" + aAct.toOneLineString()+"\r\n", fLogcolor);
		}

		//相手の手を待つ
		public Action waitUntilOpponentAction() {
			Action aAct;
			String s = waitFor("play");
			respond("");
			aAct = Action.Parse(s);

			return aAct;
		}
	
		public bool connect() {
			fSocket = new TcpClient();
			fRestString = "";
			fOpponentName = null;
			try {
				// ソケット接続
				fSocket.Connect(Settings.ConnectionIP, Settings.ConnectionPort);
				// ソケットストリーム取得
				fStream = fSocket.GetStream();
			} catch {
				// 接続失敗．大抵はサーバがないだけ．
				Logger.netlog("Connection Failure" + "\r\n", fLogcolor);
				return false;
			}

			Logger.netlog("connected to server" + "\r\n", fLogcolor);
			return true;
		}
		
		//ユーザーログ変数　プロパティ
		public int userNumOfCombatLog {
			get { return userNumOfCombatLog; }
			set { }
		}

        // 改行を含んでも良い，パラメータ等の情報を返す関数
        public string showParameters() {
            return "No parameter";
        }

		// = ...\r\n\r\n の形で返事を送る
		private void respond(String message) {
			Byte[] dat = System.Text.Encoding.GetEncoding("UTF-8").GetBytes("= " + message + "\r\n\r\n");
			fStream.Write(dat, 0, dat.GetLength(0));
		}

		// command...\r\n の形のものが来るまで待ち，...の部分を返す．
		// 複数のコマンドがちぎれたりひっついたりするので， \r\n の後ろを次の waitFor のために残す
		// closeされたら抜ける
		private String waitFor(String command) {
			String s = fRestString;
			while (true) {
				Thread.Sleep(50);
				Application.DoEvents();

				//                Logger.log("I received ["+s+"] and waitFor ["+command+"]. Length of s ="+s.Length);

				if (Signal.FormClosed == true) return "";

				if (fSocket.Connected == false) {
					Logger.netlog("Connection closed by server" + "\r\n", fLogcolor);
					return "";
				}

				if (fSocket.Available > 0) {
					Byte[] dat = new Byte[fSocket.Available];
					fStream.Read(dat, 0, dat.GetLength(0));
					s = s + System.Text.Encoding.GetEncoding("UTF-8").GetString(dat);
				}
				if (s.Length >= command.Length + 1) {
					if (s.Substring(0, command.Length).Equals(command)) {
						Logger.netlog("received : " + s + "\r\n", fLogcolor);
						int pos = s.IndexOf("\n");
						if (pos > 0) {
							fRestString = s.Substring(pos + 1); // \n の後ろから全てを残す
							String message = s.Substring(command.Length, pos - command.Length);  // 返すのは，commandの後ろから \n の前まで
							if (message.Length > 0) {
								if (message.Substring(message.Length - 1, 1).Equals("\r") == true) {
									// 最後が \r なら取る
									message = message.Substring(0, message.Length - 1);
								}
							}
							if (message.Length > 0) {  // 引数なしならそのまま，ありならスペースをとって返す
								message = message.Substring(1);
							}
							return message;
						}
					}
				}
			}


		}
    }
}
